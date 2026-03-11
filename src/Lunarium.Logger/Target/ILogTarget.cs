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

namespace Lunarium.Logger.Target;

/// <summary>
/// 定义了日志输出目标（Target）的通用接口。
/// 所有日志输出目标（如控制台、文件等）都必须实现此接口。
/// 它定义了如何发出日志条目以及如何释放相关资源。
/// </summary>
public interface ILogTarget : IDisposable
{
    /// <summary>
    /// 将单个日志条目发送或写入到最终的目标。
    /// </summary>
    /// <param name="entry">要发出的日志条目。</param>
    void Emit(LogEntry entry);
}

/// <summary>
/// 定义了文本日志的Json格式化能力和是否启用Json格式化
/// </summary>
public interface IJsonTextTarget
{
    public bool ToJson { get; set; }
}

/// <summary>
/// 定义了文本日志的颜色的能力和是否启用颜色输出
/// </summary>
public interface IColorTextTarget
{
    public bool IsColor { get; set; }
}

/// <summary>
/// 定义了一个日志输出目标接口，标志其能够接收日志对象 (LogEntry) 并保持以 LogEntry 输出到某个目的地（如控制台、文件、远程服务器等）。
/// </summary>
public interface ILogEntryTarget;