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

using Lunarium.Logger.Writer;

namespace Lunarium.Logger.Target;

/// <summary>
/// 一个将日志条目输出到标准控制台的 Target。
/// 它会使用 ANSI 颜色代码来格式化不同级别的日志，使其更易于阅读。
/// 它支持纯文本和 JSON 两种输出模式，并对结构化数据进行智能染色。
/// 输出通过底层字节流写入，避免 char→byte 中间转换。
/// </summary>
public sealed class ConsoleTarget : ILogTarget, IJsonTextTarget, IColorTextTarget
{
    // 线程安全锁，以防止多条日志消息的输出在控制台中交错
    private readonly Lock _lock = new();

    // 缓存底层字节流，避免每次 Emit 时重复调用 OpenStandard*()
    private readonly Stream _stdout;
    private readonly Stream _stderr;

    public bool ToJson { get; set; } = false;
    public bool IsColor { get; set; } = true;

    public ConsoleTarget()
    {
        _stdout = Console.OpenStandardOutput();
        _stderr = Console.OpenStandardError();
    }

    /// <summary>
    /// 格式化日志条目并将其写入控制台。
    /// </summary>
    /// <param name="entry">要输出的日志条目。</param>
    public void Emit(LogEntry entry)
    {
        // 根据配置选择格式化器
        LogWriter logWriter;
        if (ToJson)
        {
            logWriter = WriterPool.Get<LogJsonWriter>();
        }
        else if (IsColor && !Console.IsOutputRedirected)
        {
            logWriter = WriterPool.Get<LogColorTextWriter>();
        }
        else
        {
            logWriter = WriterPool.Get<LogTextWriter>();
        }

        try
        {
            // 渲染日志
            logWriter.Render(entry);

            // 选择输出流
            Stream output = entry.LogLevel >= LogLevel.Error ? _stderr : _stdout;

            // 输出
            lock (_lock)
            {
                logWriter.FlushTo(output);
            }
        }
        finally
        {
            // 归还对象池
            logWriter.Return();
        }
    }

    /// <summary>
    /// 释放控制台字节流资源。
    /// </summary>
    public void Dispose()
    {
        _stdout.Dispose();
        _stderr.Dispose();
    }
}
