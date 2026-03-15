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

using System.Threading.Channels;
using Lunarium.Logger.Writer;

namespace Lunarium.Logger.Target;

/// <summary>
/// ChannelTarget 的基类，负责 Channel 写入的通用逻辑。
/// 子类只需实现 Transform，决定如何将 LogEntry 转换为目标类型 T。
/// </summary>
public abstract class ChannelTarget<T> : ILogTarget, IDisposable
{
    private readonly ChannelWriter<T> _writer;

    protected ChannelTarget(ChannelWriter<T> writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// 将 LogEntry 转换为目标类型 T，由子类实现。
    /// </summary>
    protected abstract T Transform(LogEntry entry);

    public void Emit(LogEntry entry)
    {
        var item = Transform(entry);
        _writer.TryWrite(item);
    }

    public void Dispose() => _writer.TryComplete();
}

/// <summary>
/// 将 LogEntry 格式化为字符串写入 Channel。
/// ⚠️ 性能提示：每次 Emit 会产生一次 string 堆分配（UTF-8 解码为 UTF-16）。
/// 若消费者直接写网络/文件，建议改用 <see cref="ByteChannelTarget"/>。
/// </summary>
public sealed class StringChannelTarget : ChannelTarget<string>, IJsonTextTarget, ITextTarget
{
    public bool ToJson { get; set; } = false;
    
    public bool IsColor { get; set; } = false;
    public TextOutputIncludeConfig TextOutputIncludeConfig { get; set; } = new TextOutputIncludeConfig();

    public StringChannelTarget(ChannelWriter<string> writer, bool isColor)
        : base(writer)
    {
        IsColor = isColor;
    }

    protected override string Transform(LogEntry entry)
    {
        // 根据配置选择格式化器
        // ToJson 优先级最高，其次是是否使用颜色(如果输出为Json则不使用颜色)
        // ToJson?
        //    是 -> LogJsonWriter
        //    否 -> _isColor?
        //           是 -> LogColorTextWriter
        //           否 -> LogTextWriter
        LogWriter logWriter = ToJson
            ? WriterPool.Get<LogJsonWriter>()
            : IsColor
                ? WriterPool.Get<LogColorTextWriter>()
                : WriterPool.Get<LogTextWriter>();
        try
        {
            if (logWriter is ITextTarget textTarget)
            {
                logWriter.Render(entry, TextOutputIncludeConfig);
            }
            else
            {
                logWriter.Render(entry);
            }
            return logWriter.ToString();
        }
        finally
        {
            logWriter.Return();
        }
    }
}

/// <summary>
/// 将 LogEntry 格式化为 UTF-8 字节数组写入 Channel。
/// 相比 <see cref="StringChannelTarget"/>，跳过 UTF-8→string 解码步骤，
/// 适合消费者直接进行网络传输或文件写入的场景。
/// ⚠️ 性能提示：每次 Emit 仍会产生一次 byte[] 堆分配（ToArray()）。
/// </summary>
public sealed class ByteChannelTarget : ChannelTarget<byte[]>, IJsonTextTarget, ITextTarget
{
    public bool ToJson { get; set; } = false;
    public bool IsColor { get; set; } = false;
    public TextOutputIncludeConfig TextOutputIncludeConfig { get; set; } = new TextOutputIncludeConfig();

    public ByteChannelTarget(ChannelWriter<byte[]> writer, bool isColor)
        : base(writer)
    {
        IsColor = isColor;
    }

    protected override byte[] Transform(LogEntry entry)
    {
        LogWriter logWriter = ToJson
            ? WriterPool.Get<LogJsonWriter>()
            : IsColor
                ? WriterPool.Get<LogColorTextWriter>()
                : WriterPool.Get<LogTextWriter>();
        try
        {
            logWriter.Render(entry);
            return logWriter.GetWrittenBytes();
        }
        finally
        {
            logWriter.Return();
        }
    }
}

/// <summary>
/// 将 LogEntry 原样写入 Channel。
/// </summary>
public sealed class LogEntryChannelTarget : ChannelTarget<LogEntry>, ILogEntryTarget
{
    public LogEntryChannelTarget(ChannelWriter<LogEntry> writer) : base(writer) { }

    protected override LogEntry Transform(LogEntry entry) => entry;
}


/// <summary>
/// 将 LogEntry 转换为自定义类型 T 写入 Channel。
/// </summary>
internal sealed class DelegateChannelTarget<T> : ChannelTarget<T>
{
    private readonly Func<LogEntry, T> _transform; // 存起来备用

    public DelegateChannelTarget(ChannelWriter<T> writer, Func<LogEntry, T> transform)
        : base(writer)
    {
        _transform = transform;
    }

    protected override T Transform(LogEntry entry) => _transform(entry); // 转发给委托
}
