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

using FluentAssertions;

namespace Lunarium.Logger.Tests.Config;

/// <summary>
/// Tests for SinkOutputConfig record — default values and IgnoreFilterCase linkage.
/// </summary>
public class SinkOutputConfigTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. Default values
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SinkOutputConfig_DefaultValues_AreCorrect()
    {
        var cfg = new SinkOutputConfig();
        cfg.LogMinLevel.Should().Be(LogLevel.Info);
        cfg.LogMaxLevel.Should().Be(LogLevel.Critical);
        cfg.ToJson.Should().BeNull();
        cfg.ContextFilterIncludes.Should().BeNull();
        cfg.ContextFilterExcludes.Should().BeNull();
        cfg.IgnoreFilterCase.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. IgnoreFilterCase drives ComparisonType
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SinkOutputConfig_IgnoreFilterCase_True_SetsOrdinalIgnoreCase()
    {
        var cfg = new SinkOutputConfig { IgnoreFilterCase = true };
        cfg.ComparisonType.Should().Be(StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SinkOutputConfig_IgnoreFilterCase_False_SetsOrdinal()
    {
        var cfg = new SinkOutputConfig { IgnoreFilterCase = false };
        cfg.ComparisonType.Should().Be(StringComparison.Ordinal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Custom level range
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SinkOutputConfig_CustomLevelRange_SetCorrectly()
    {
        var cfg = new SinkOutputConfig
        {
            LogMinLevel = LogLevel.Error,
            LogMaxLevel = LogLevel.Critical
        };
        cfg.LogMinLevel.Should().Be(LogLevel.Error);
        cfg.LogMaxLevel.Should().Be(LogLevel.Critical);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. ToJson flag
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SinkOutputConfig_ToJsonTrue_SetCorrectly()
    {
        var cfg = new SinkOutputConfig { ToJson = true };
        cfg.ToJson.Should().BeTrue();
    }
}
