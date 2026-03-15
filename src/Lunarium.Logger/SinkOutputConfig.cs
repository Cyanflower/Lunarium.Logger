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

namespace Lunarium.Logger;

/// <summary>
/// 封装单个日志输出目标 (Sink) 的相关配置
/// </summary>
public record SinkOutputConfig
{
    // Defualt: null (Unset) - Sink will use its own default behavior
    // 使用 Json 格式输出日志
    public bool? ToJson { get; init; } = null;

    // 使用颜色输出日志（仅适用于支持颜色的 IColorTextTarget， 如: ConsoleTarget）
    public bool? IsColor { get; init; } = null;

    public TextOutputIncludeConfig? TextOutputIncludeConfig { get; init; } = null;

    /// <summary>
    /// 上下文前缀过滤器:
    /// 包含规则列表。
    /// 只有当日志上下文以列表中的任意一个前缀开头时，才算匹配。
    /// 如果此列表为 null 或为空，则视为匹配所有日志。
    /// </summary>
    public List<string>? ContextFilterIncludes { get; init; }

    /// <summary>
    /// 排除规则列表。
    /// 如果日志上下文以列表中的任意一个前缀开头，则会被明确排除。
    /// </summary>
    public List<string>? ContextFilterExcludes { get; init; }


    /// <summary>
    /// 匹配上下文时是否忽略大小写。
    /// </summary>
    public bool IgnoreFilterCase { get; init; } = false;

    /// <summary>
    /// <para>用于上下文前缀匹配的字符串比较方式。此属性由 <see cref="IgnoreFilterCase"/> 自动设置，不应直接修改。</para>
    /// <para>当 <see cref="IgnoreFilterCase"/> 为 true 时为 <see cref="StringComparison.OrdinalIgnoreCase"/>，
    /// 否则为 <see cref="StringComparison.Ordinal"/>。</para>
    /// </summary>
    internal StringComparison ComparisonType =>
        IgnoreFilterCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// 允许记录的最低日志级别。
    /// </summary>
    public LogLevel LogMinLevel { get; init; } = LogLevel.Info;

    /// <summary>
    /// 允许记录的最高日志级别。
    /// </summary>
    public LogLevel LogMaxLevel { get; init; } = LogLevel.Critical;
}

public record class TextOutputIncludeConfig
{
    public bool IncludeTimestamp { get; init; } = true;
    public bool IncludeLoggerName { get; init; } = true;
    public bool IncludeLevel { get; init; } = true;
    public bool IncludeContext { get; init; } = true;
}