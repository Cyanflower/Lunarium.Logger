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
/// 控制台 Sink 的完整配置。
/// <para>ConsoleTarget 无专属参数，此类仅作为 <see cref="ISinkConfig"/> 的统一入口封装 <see cref="SinkOutputConfig"/>。</para>
/// </summary>
public sealed record ConsoleSinkConfig : ISinkConfig
{
    /// <inheritdoc/>
    public SinkOutputConfig? SinkOutputConfig { get; init; }

    /// <inheritdoc/>
    public ILogTarget CreateTarget() => new ConsoleTarget();
}
