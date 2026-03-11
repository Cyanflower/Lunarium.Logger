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
/// 全局配置锁，用于确保全局配置只能被设置一次
/// </summary>
internal static class GlobalConfigLock
{
    /// <summary>
    /// 标识全局配置是否已完成
    /// </summary>
    internal static bool Configured = false;

    /// <summary>
    /// 完成全局配置并锁定
    /// </summary>
    internal static void CompleteConfig()
    {
        Configured = true;
    }
}

