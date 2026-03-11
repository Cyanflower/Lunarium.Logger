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

namespace Lunarium.Logger.GlobalConfig;

/// <summary>
/// 配置是否默认对复杂对象进行结构
/// </summary>
internal static class DestructuringConfig
{
    /// <summary>
    /// 标识是否默认对复杂对象进行结构
    /// </summary>
    internal static bool AutoDestructureCollections { get; private set; } = false;

    /// <summary>
    /// 设置默认对复杂对象进行结构
    /// </summary>
    internal static void EnableAutoDestructuring()
    {
        AutoDestructureCollections = true;
    }
}
