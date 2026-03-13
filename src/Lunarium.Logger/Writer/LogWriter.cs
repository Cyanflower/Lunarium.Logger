// Copyright 2026 Cyanflower
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Lunarium.Logger.GlobalConfig;
using Lunarium.Logger.Parser;

namespace Lunarium.Logger.Writer;

internal abstract class LogWriter : IDisposable
{
    protected readonly BufferWriter _bufferWriter;
    // 状态定义：0 = 使用中 (Active), 1 = 已释放/在池中 (Disposed/InPool)
    // 使用 int 以便进行 Interlocked 操作
    private int _disposedState = 0; 
    private const int DefaultMaxCapacity = 4 * 1024; // 默认4KB阈值

    public LogWriter()
    {
        _bufferWriter = new BufferWriter(256);
    }

    #region --- 池化管理 API ---

    /// <summary>
    /// 获取内部 BufferWriter 的当前容量, 供对象池进行健康度检查
    /// </summary>
    internal virtual int GetBufferCapacity() => _bufferWriter.Capacity;

    /// <summary>
    /// 尝试重置写入器以供重用, 如果对象因状态异常 (如缓冲区过大) 而不适合重用, 则返回 false
    /// </summary>
    /// <param name="maxCapacity">允许的最大缓冲区容量, 超过此容量的对象将被视为不健康, 默认4KB阈值</param>
    /// <returns>如果对象已成功重置并适合归还池中, 则为 true；否则为 false</returns>
    internal virtual bool TryReset(int maxCapacity = DefaultMaxCapacity)
    {
        if (_bufferWriter.Capacity > maxCapacity)
        {
            // 对象被污染, 不应归还
            return false;
        }

        // 对象健康, 清理并准备重用
        _bufferWriter.Reset();

        // 如果派生类有其他状态, 它们应该重写此方法并调用 base.TryReset()
        return true;
    }

    /// <summary>
    /// 当对象从池中取出时调用，将其标记为活跃状态
    /// </summary>
    internal void MarkAsActive()
    {
        // 使用 Volatile 确保所有 CPU 核心立即看到状态变更
        Volatile.Write(ref _disposedState, 0);
    }

    #region --- 对象池归还 ---
    /// <summary>
    /// 将此 Writer 归还到对象池, 非 using 语句时手动调用
    /// </summary>
    public void Return()
    {
        Dispose();
    }

    /// <summary>
    /// 实现 IDisposable, 支持 using 语句自动归还
    /// </summary>
    public void Dispose()
    {
        // 原子检查并设置：如果当前是 0，则设为 1；如果当前已经是 1，说明已归还，直接返回
        if (Interlocked.CompareExchange(ref _disposedState, 1, 0) != 0) 
        {
            return; 
        }

        // 只有抢到“从 0 变 1”权利的线程才能执行归还逻辑
        // 调用子类的归还逻辑
        ReturnToPool();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 此方法主要用于 WriterPool 拒绝回收该对象时，确保 ArrayPool 资源不泄漏。
    /// </summary>
    internal void DisposeAndReturnArrayBuffer()
    {
        try 
        {
            // 增加对环境状态的判断：如果进程正在关闭，归还 ArrayPool 可能不再安全或不再必要
            if (Environment.HasShutdownStarted) return;
            // 直接释放内部缓冲区
            // 注意：_bufferWriter.Dispose() 内部已经处理了幂等性（多次调用安全）
            _bufferWriter?.Dispose();
        }
        catch (Exception ex)
        {
            // 析构函数中绝不能抛出异常，否则会导致进程崩溃
            // 我们通过内部日志记录（如果此时内部日志记录器还能用的话）
            InternalLogger.Error(ex, "Error in LogWriter Finalizer");
        }
    }

    // 抽象方法: 子类负责归还到自己的池
    protected abstract void ReturnToPool();

    #endregion
    #endregion

    #region --- 公共渲染入口 ---

    /// <summary>
    /// 渲染日志条目到内部缓冲区
    /// </summary>
    internal void Render(LogEntry logEntry)
    {
        BeginEntry();
        WriteTimestamp(logEntry.Timestamp);
        WriteLevel(logEntry.LogLevel);
        WriteContext(logEntry.Context);

        // 🎣 钩子：允许子类在渲染消息前插入额外逻辑(如 JSON 的 OriginalMessage)
        BeforeRenderMessage(logEntry);

        WriteRenderedMessage(logEntry.MessageTemplate.MessageTemplateTokens, logEntry.Properties);

        // 🎣 钩子：允许子类在渲染消息后插入额外逻辑(如 JSON 的 PropertyValue)
        AfterRenderMessage(logEntry);

        WriteException(logEntry.Exception);
        EndEntry();

        _bufferWriter.AppendLine();
    }

    /// <summary>
    /// 将已渲染内容直接写入流，零拷贝，无分配。
    /// </summary>
    internal void FlushTo(Stream stream) => _bufferWriter.FlushTo(stream);

    /// <summary>
    /// 将已渲染内容写入 TextWriter（降级路径：UTF-8 解码后写入）。
    /// 小内容（≤4096 chars）使用 stackalloc，不产生堆分配；更大内容退化为 string 分配。
    /// </summary>
    internal void FlushTo(TextWriter output)
    {
        ReadOnlySpan<byte> bytes = _bufferWriter.WrittenSpan;
        if (bytes.IsEmpty) return;
        int charCount = Encoding.UTF8.GetCharCount(bytes);
        if (charCount <= 4096)
        {
            Span<char> chars = stackalloc char[charCount];
            Encoding.UTF8.GetChars(bytes, chars);
            output.Write(chars);
        }
        else
        {
            output.Write(Encoding.UTF8.GetString(bytes));
        }
    }

    /// <summary>
    /// 将已渲染内容以 byte[] 形式返回（ByteChannelTarget 使用）。
    /// </summary>
    internal byte[] GetWrittenBytes() => _bufferWriter.WrittenSpan.ToArray();

    public override string ToString()
    {
        return _bufferWriter.ToString();
    }

    #endregion

    #region --- 抽象方法：子类必须实现 ---

    protected abstract LogWriter WriteTimestamp(DateTimeOffset timestamp);
    protected abstract LogWriter WriteLevel(LogLevel level);
    protected abstract LogWriter WriteContext(string? context);
    protected abstract LogWriter WriteRenderedMessage(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys);
    protected abstract LogWriter WriteException(Exception? exception);

    #endregion

    #region --- 钩子方法：子类可选重写 ---

    protected virtual void BeforeRenderMessage(LogEntry logEntry) { }
    protected virtual void AfterRenderMessage(LogEntry logEntry) { }
    protected virtual LogWriter BeginEntry() { return this; }
    protected virtual LogWriter EndEntry() { return this; }

    #endregion

    #region --- 序列化工具 ---

    /// <summary>
    /// 将对象序列化为 JSON 字符串。序列化失败时返回 null（由调用方决定降级策略）。
    /// 若 GlobalConfigurator 注册了 JsonTypeInfoResolver，AOT 环境下可正常工作；
    /// 否则在 AOT 下序列化将失败并返回 null。
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Serialization failure returns null and callers fall back gracefully. " +
                        "AOT users can register a JsonTypeInfoResolver via GlobalConfigurator.UseJsonTypeInfoResolver().")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Same as IL2026.")]
    protected static string? TrySerializeToJson(object? value)
    {
        try { return JsonSerializer.Serialize(value, JsonSerializationConfig.Options); }
        catch { return null; }
    }

    #endregion

    #region --- 钩子方法：子类可选重写 ---
    // 判断对象是否为C#核心库中常见的集合类型
    protected static bool IsCommonCollectionType(object? obj)
    {
        if (obj == null)
            return false;

        // 排除string（虽然它实现了IEnumerable，但通常不被视为集合）
        if (obj is string)
            return false;

        // 检查是否为数组（使用 is 模式而非反射，AOT 安全）
        if (obj is Array)
            return true;

        // 检查是否实现了常见的集合接口
        return obj is IEnumerable ||
               obj is ICollection ||
               obj is IList ||
               obj is IDictionary;
    }
    #endregion

    /// <summary>
    /// 析构函数：确保 ArrayPool 资源不泄漏
    /// </summary>
    ~LogWriter()
    {
        // 万一用户忘了写 using，对象被 GC 时也要把数组还回去
        // 否则 ArrayPool 会随着时间推移被耗尽
        if (_bufferWriter != null)
        {
            DisposeAndReturnArrayBuffer();
        }
    }
}
