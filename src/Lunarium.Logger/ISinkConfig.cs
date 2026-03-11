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

namespace Lunarium.Logger;

/// <summary>
/// 表示一个完整的 Sink 配置，同时包含 Sink 专属参数和通用的 <see cref="SinkOutputConfig"/>。
/// </summary>
public interface ISinkConfig
{
    /// <summary>
    /// 通用 Sink 配置（日志级别过滤、上下文过滤、输出格式等）。
    /// </summary>
    SinkOutputConfig? SinkOutputConfig { get; }

    /// <summary>
    /// 根据当前配置创建对应的日志输出目标。
    /// </summary>
    ILogTarget CreateTarget();
}
