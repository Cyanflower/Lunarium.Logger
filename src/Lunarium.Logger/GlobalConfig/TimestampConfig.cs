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
/// 日志时间戳模式
/// </summary>
internal enum LogTimestampMode
{
    Local,  // default
    Utc,
    Custom
}

/// <summary>
/// 日志时间全局配置
/// </summary>
internal static class LogTimestampConfig
{
    /// <summary>
    /// 日志时间戳模式（默认为 UTC）
    /// </summary>
    internal static LogTimestampMode Mode { get; private set; } = LogTimestampMode.Local;
    
    /// <summary>
    /// 自定义时区信息（仅在 Mode 为 Custom 时使用）
    /// </summary>
    internal static TimeZoneInfo CustomTimeZone { get; private set; } = TimeZoneInfo.Utc;


    /// <summary>
    /// 设置为使用本地时间
    /// </summary>
    internal static void UseLocalTime()
    {
        Mode = LogTimestampMode.Local;
    }

    /// <summary>
    /// 设置为使用 UTC 时间
    /// </summary>
    internal static void UseUtcTime()
    {
        Mode = LogTimestampMode.Utc;
        CustomTimeZone = TimeZoneInfo.Utc;
    }

    /// <summary>
    /// 设置为使用自定义时区
    /// </summary>
    /// <param name="timeZone">指定的时区信息</param>
    /// <exception cref="ArgumentNullException">timeZone 为 null</exception>
    internal static void UseCustomTimeZone(TimeZoneInfo timeZone)
    {
        if (timeZone == null)
            throw new ArgumentNullException(nameof(timeZone));
            
        Mode = LogTimestampMode.Custom;
        CustomTimeZone = timeZone;
    }
    
    /// <summary>
    /// 获取当前配置的时间戳
    /// </summary>
    /// <returns>根据配置模式返回的 DateTime</returns>
    internal static DateTimeOffset GetTimestamp()
    {
        return Mode switch
        {
            LogTimestampMode.Local => DateTimeOffset.Now,
            LogTimestampMode.Utc => DateTimeOffset.UtcNow,
            LogTimestampMode.Custom => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CustomTimeZone),
            _ => DateTimeOffset.UtcNow
        };
    }
}