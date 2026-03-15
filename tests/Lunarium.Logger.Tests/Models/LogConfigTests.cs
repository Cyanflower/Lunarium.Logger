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

using System.Threading.Channels;
using Lunarium.Logger.Models;
using Lunarium.Logger.Parser;
using Lunarium.Logger.Target;
using Lunarium.Logger.SinkConfig;

namespace Lunarium.Logger.Tests.Models;

/// Tests for Sink, ConsoleSinkConfig, FileSinkConfig.
/// </summary>
public class LogConfigTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. Sink — constructors and Deconstruct overloads
    // ─────────────────────────────────────────────────────────────────────────

    private static ILogTarget MakeSink()
        => new StringChannelTarget(System.Threading.Channels.Channel.CreateUnbounded<string>().Writer, false);

    [Fact]
    public void Sink_Ctor_WithConfig_StoresAll()
    {
        var target = MakeSink();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Warning };
        var s = new Sink(target, cfg);
        s.Target.Should().BeSameAs(target);
        s.Configuration.LogMinLevel.Should().Be(LogLevel.Warning);
        s.LoggerFilter.Should().NotBeNull();
    }

    [Fact]
    public void Sink_Ctor_WithoutConfig_UsesDefaults()
    {
        var target = MakeSink();
        var s = new Sink(target);
        s.Target.Should().BeSameAs(target);
        s.Configuration.Should().NotBeNull();
        s.LoggerFilter.Should().NotBeNull();
    }

    [Fact]
    public void Sink_Deconstruct_TwoOut()
    {
        var target = MakeSink();
        var cfg = new SinkOutputConfig();
        var sink = new Sink(target, cfg);
        var (t, c) = sink;
        t.Should().BeSameAs(target);
        c.Should().Be(cfg);
    }

    [Fact]
    public void Sink_Deconstruct_ThreeOut()
    {
        var target = MakeSink();
        var cfg = new SinkOutputConfig();
        var sink = new Sink(target, cfg);
        var (t, c, f) = sink;
        t.Should().BeSameAs(target);
        c.Should().Be(cfg);
        f.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. ConsoleSinkConfig — public record with optional SinkOutputConfig
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConsoleSinkConfig_DefaultConstruction()
    {
        var cfg = new ConsoleSinkConfig();
        cfg.SinkOutputConfig.Should().BeNull();
    }

    [Fact]
    public void ConsoleSinkConfig_WithOutputConfig()
    {
        var outCfg = new SinkOutputConfig { LogMinLevel = LogLevel.Error };
        var cfg = new ConsoleSinkConfig { SinkOutputConfig = outCfg };
        cfg.SinkOutputConfig.Should().NotBeNull();
        cfg.SinkOutputConfig!.LogMinLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void ConsoleSinkConfig_IsISinkConfig()
    {
        var cfg = new ConsoleSinkConfig();
        cfg.Should().BeAssignableTo<ISinkConfig>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. FileSinkConfig — record with required LogFilePath and defaults
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FileSinkConfig_DefaultValues()
    {
        var cfg = new FileSinkConfig { LogFilePath = "logs/app.log" };
        cfg.LogFilePath.Should().Be("logs/app.log");
        cfg.MaxFileSizeMB.Should().Be(0);
        cfg.RotateOnNewDay.Should().BeFalse();
        cfg.MaxFile.Should().Be(0);
        cfg.SinkOutputConfig.Should().BeNull();
    }

    [Fact]
    public void FileSinkConfig_AllPropertiesSet()
    {
        var outCfg = new SinkOutputConfig { LogMinLevel = LogLevel.Info };
        var cfg = new FileSinkConfig
        {
            LogFilePath = "logs/test.log",
            MaxFileSizeMB = 50.5,
            RotateOnNewDay = true,
            MaxFile = 7,
            SinkOutputConfig = outCfg
        };
        cfg.LogFilePath.Should().Be("logs/test.log");
        cfg.MaxFileSizeMB.Should().Be(50.5);
        cfg.RotateOnNewDay.Should().BeTrue();
        cfg.MaxFile.Should().Be(7);
        cfg.SinkOutputConfig.Should().BeSameAs(outCfg);
    }

    [Fact]
    public void FileSinkConfig_IsISinkConfig()
    {
        var cfg = new FileSinkConfig { LogFilePath = "x" };
        cfg.Should().BeAssignableTo<ISinkConfig>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Sink.SetJsonSinkProperty — ToJson path (IJsonTextTarget)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sink_SetJsonSinkProperty_WithToJsonTrue_SetsToJsonOnTarget()
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false);
        var cfg = new SinkOutputConfig { ToJson = true };
        _ = new Sink(target, cfg);
        target.ToJson.Should().BeTrue();
    }

    [Fact]
    public void Sink_SetJsonSinkProperty_WithToJsonFalse_OverridesTargetDefault()
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false) { ToJson = true };
        var cfg = new SinkOutputConfig { ToJson = false };
        _ = new Sink(target, cfg);
        target.ToJson.Should().BeFalse();
    }

    [Fact]
    public void Sink_SetJsonSinkProperty_WithToJsonNull_DoesNotModifyTarget()
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false); // default ToJson = false
        var cfg = new SinkOutputConfig { ToJson = null };
        _ = new Sink(target, cfg);
        target.ToJson.Should().BeFalse(); // unchanged
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Sink.SetJsonSinkProperty — IsColor path (IColorTextTarget)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sink_SetJsonSinkProperty_WithIsColorFalse_SetsIsColorOnTarget()
    {
        var target = new ConsoleTarget(); // default IsColor = true
        var cfg = new SinkOutputConfig { IsColor = false };
        _ = new Sink(target, cfg);
        target.IsColor.Should().BeFalse();
    }

    [Fact]
    public void Sink_SetJsonSinkProperty_WithIsColorTrue_KeepsIsColorTrue()
    {
        var target = new ConsoleTarget(); // default IsColor = true
        var cfg = new SinkOutputConfig { IsColor = true };
        _ = new Sink(target, cfg);
        target.IsColor.Should().BeTrue();
    }

    [Fact]
    public void Sink_SetJsonSinkProperty_WithIsColorNull_DoesNotModifyTarget()
    {
        var target = new ConsoleTarget(); // default IsColor = true
        var cfg = new SinkOutputConfig { IsColor = null };
        _ = new Sink(target, cfg);
        target.IsColor.Should().BeTrue(); // unchanged
    }

    [Fact]
    public void Sink_SetJsonSinkProperty_TargetNotIColorTextTarget_DoesNotThrow()
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false);
        var cfg = new SinkOutputConfig { IsColor = true };
        Action act = () => _ = new Sink(target, cfg);
        act.Should().NotThrow();
        target.Should().NotBeAssignableTo<IColorTextTarget>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Sink.Emit — filter pass / block
    // ─────────────────────────────────────────────────────────────────────────

    private static LogEntry MakeLogEntry(LogLevel level = LogLevel.Info, string msg = "hello")
        => new LogEntry(
            loggerName: "Test",
            timestamp: DateTimeOffset.UtcNow,
            logLevel: level,
            message: msg,
            properties: [],
            context: "",
            contextBytes: default,
            scope: "",
            messageTemplate: LogParser.EmptyMessageTemplate);

    [Fact]
    public void Sink_Emit_WhenFilterPasses_CallsTargetEmit()
    {
        var mockTarget = Substitute.For<ILogTarget>();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Debug };
        var sink = new Sink(mockTarget, cfg);
        sink.Emit(MakeLogEntry(LogLevel.Info));
        mockTarget.Received(1).Emit(Arg.Any<LogEntry>());
    }

    [Fact]
    public void Sink_Emit_WhenFilterBlocks_DoesNotCallTargetEmit()
    {
        var mockTarget = Substitute.For<ILogTarget>();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Warning };
        var sink = new Sink(mockTarget, cfg);
        sink.Emit(MakeLogEntry(LogLevel.Debug));
        mockTarget.DidNotReceive().Emit(Arg.Any<LogEntry>());
    }
}
