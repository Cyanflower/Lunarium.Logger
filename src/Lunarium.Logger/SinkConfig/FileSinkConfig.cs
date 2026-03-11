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

using Lunarium.Logger.Target;

namespace Lunarium.Logger.SinkConfig;

/// <summary>
/// 文件 Sink 的完整配置，涵盖按大小轮转、按天轮转及两者叠加的所有模式。
/// <para>与 <c>FileTarget</c> 构造参数一一对应：不填 <see cref="MaxFileSizeMB"/> 则不按大小轮转，
/// 不填 <see cref="RotateOnNewDay"/> 则不按天轮转。</para>
/// </summary>
public sealed record FileSinkConfig : ISinkConfig
{
    /// <summary>
    /// 日志文件基础路径，例如 <c>"Logs/app.log"</c>。
    /// </summary>
    public required string LogFilePath { get; init; }

    /// <summary>
    /// 单个文件的最大大小 (MB)。超过此大小时触发轮转。
    /// ≤0 表示不按大小轮转。
    /// </summary>
    public double MaxFileSizeMB { get; init; } = 0;

    /// <summary>
    /// 是否在新的一天开始时触发轮转。
    /// </summary>
    public bool RotateOnNewDay { get; init; } = false;

    /// <summary>
    /// 保留的最大日志文件数量。≤0 表示不限制。
    /// <para>⚠️ 当 <see cref="MaxFile"/> > 0 时，至少须启用一种轮转策略。</para>
    /// </summary>
    public int MaxFile { get; init; } = 0;

    /// <inheritdoc/>
    public SinkOutputConfig? SinkOutputConfig { get; init; }

    /// <inheritdoc/>
    public ILogTarget CreateTarget() =>
        new FileTarget(LogFilePath, MaxFileSizeMB, RotateOnNewDay, MaxFile);
}
