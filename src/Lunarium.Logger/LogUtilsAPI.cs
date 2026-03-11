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

public static class LogUtils
{
    public static DateTimeOffset GetLogSystemTimestamp()
    {
        return LogTimestampConfig.GetTimestamp();
    }
    public static string GetLogSystemFormattedTimestamp()
    {
        var formattedTimestamp = TimestampFormatConfig.TextMode switch
        {
            TextTimestampMode.Unix => LogTimestampConfig.GetTimestamp().ToUnixTimeSeconds().ToString(),
            TextTimestampMode.UnixMs => LogTimestampConfig.GetTimestamp().ToUnixTimeMilliseconds().ToString(),
            TextTimestampMode.ISO8601 => $"{LogTimestampConfig.GetTimestamp():O}",
            TextTimestampMode.Custom => LogTimestampConfig.GetTimestamp().ToString(TimestampFormatConfig.TextCustomFormat),
            _ => $"{LogTimestampConfig.GetTimestamp():O}"
        };
        return formattedTimestamp;
    }
}