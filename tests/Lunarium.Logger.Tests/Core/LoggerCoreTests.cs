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
using Lunarium.Logger.Target;

namespace Lunarium.Logger.Tests.Core;

/// <summary>
/// Unit tests for Logger core paths:
///   - DisposeAsync (flushes queue and disposes sinks)
///   - Post-dispose Log() call is silently dropped (cancellation guard)
///   - Duplicate ILogTarget path guard in constructor
///   - LoggerBuilder.LoggerName() and LoggerBuilder.Build() flow
/// 
/// These use Logger's internal constructor directly (InternalsVisibleTo).
/// </summary>
public class LoggerCoreTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (Logger logger, Channel<string> ch) MakeLogger(
        LogLevel minLevel = LogLevel.Debug,
        string name = "UnitTest")
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false);
        var cfg = new SinkOutputConfig { LogMinLevel = minLevel, LogMaxLevel = LogLevel.Critical };
        var sinks = new List<Sink> { new(target, cfg) };
        var logger = new Logger(sinks, name);
        return (logger, ch);
    }

    private static async Task<string?> ReadWithTimeoutAsync(
        Channel<string> ch, string marker, int timeoutMs = 2000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await foreach (var msg in ch.Reader.ReadAllAsync(cts.Token))
                if (msg.Contains(marker)) return msg;
        }
        catch (OperationCanceledException) { }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Basic log dispatch through internal constructor
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_Basic_LogDispatchesToSink()
    {
        var (logger, ch) = MakeLogger();
        var id = Guid.NewGuid().ToString("N")[..8];

        logger.Log(LogLevel.Info, $"Hello {id}");

        var result = await ReadWithTimeoutAsync(ch, id);
        result.Should().NotBeNull();
        result!.Should().Contain(id);

        await logger.DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. DisposeAsync — subsequent Log() is silently dropped
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_AfterDispose_LogIsDropped()
    {
        var (logger, ch) = MakeLogger();
        await logger.DisposeAsync();

        var id = Guid.NewGuid().ToString("N")[..8];
        // Should not throw; the message should simply be dropped
        logger.Log(LogLevel.Error, $"Should not appear {id}");

        await Task.Delay(200);
        var result = await ReadWithTimeoutAsync(ch, id, timeoutMs: 200);
        result.Should().BeNull("messages after Dispose should be dropped");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. DisposeAsync — disposes all sinks (channel completed = no more reads)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_DisposeAsync_CompletesWithoutException()
    {
        var (logger, _) = MakeLogger();
        logger.Log(LogLevel.Info, "Pre-dispose message");
        // DisposeAsync should not throw
        var act = async () => await logger.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Duplicate FileTarget path — throws InvalidOperationException at construction
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FileTarget_DuplicateFilePath_ThrowsInvalidOperation()
    {
        const string path = "/tmp/lunarium_dup_test.log";
        var sink1 = new FileTarget(path);
        try
        {
            Action act = () => _ = new FileTarget(path);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{path}*");
        }
        finally
        {
            sink1.Dispose();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Logger with multiple sinks — both receive messages
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_MultipleSinks_BothReceiveMessage()
    {
        var ch1 = Channel.CreateUnbounded<string>();
        var ch2 = Channel.CreateUnbounded<string>();
        var sinks = new List<Sink>
        {
            new(new StringChannelTarget(ch1.Writer, false), new SinkOutputConfig()),
            new(new StringChannelTarget(ch2.Writer, false), new SinkOutputConfig()),
        };
        var logger = new Logger(sinks, "MultiSink");
        var id = Guid.NewGuid().ToString("N")[..8];
        logger.Log(LogLevel.Info, $"Multi {id}");

        var r1 = await ReadWithTimeoutAsync(ch1, id);
        var r2 = await ReadWithTimeoutAsync(ch2, id);
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();

        await logger.DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. LoggerBuilder — LoggerName() fluent chaining
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LoggerBuilder_LoggerName_ReturnsSameBuilder()
    {
        var builder = new LoggerBuilder();
        var returned = builder.LoggerName("TestApp");
        returned.Should().BeSameAs(builder);
    }

    [Fact]
    public void LoggerBuilder_AddSink_ReturnsSameBuilder()
    {
        var builder = new LoggerBuilder();
        var ch = Channel.CreateUnbounded<string>();
        var returned = builder.AddSink(new StringChannelTarget(ch.Writer, false));
        returned.Should().BeSameAs(builder);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. LoggerBuilder — 允许多次 Build()，返回独立实例
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoggerBuilder_Build_CanBuildMultipleInstances()
    {
        var ch1 = Channel.CreateUnbounded<string>();
        var ch2 = Channel.CreateUnbounded<string>();

        var logger1 = new LoggerBuilder()
            .AddSink(new StringChannelTarget(ch1.Writer, false))
            .Build();
        var logger2 = new LoggerBuilder()
            .AddSink(new StringChannelTarget(ch2.Writer, false))
            .Build();

        logger1.Should().NotBeSameAs(logger2);

        var id = Guid.NewGuid().ToString("N")[..8];
        logger1.Log(LogLevel.Info, $"L1-{id}");
        logger2.Log(LogLevel.Info, $"L2-{id}");

        var r1 = await ReadWithTimeoutAsync(ch1, $"L1-{id}");
        var r2 = await ReadWithTimeoutAsync(ch2, $"L2-{id}");
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();

        await logger1.DisposeAsync();
        await logger2.DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. FileTarget — Dispose 后同路径可重用
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FileTarget_SamePath_AfterDispose_CanReuseSuccessfully()
    {
        const string path = "/tmp/lunarium_reuse_test.log";
        var target1 = new FileTarget(path);
        target1.Dispose();

        var target2 = new FileTarget(path);
        target2.Dispose();
    }
}

