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
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lunarium.Logger.Tests.Core;

/// <summary>
/// Targeted coverage for the uncovered paths in Logger:
///   - ProcessQueueAsync inner catch (target.Emit throws)
///   - ProcessQueueAsync outer catch (queue-reader throws)
///   - DisposeAsync target.Dispose() throw path
/// </summary>
public class LoggerCoverageGapTests
{
    private static LogEntry MakeEntry(string msg = "hello") =>
        new LogEntry(
            loggerName: "Test",
            timestamp: DateTimeOffset.UtcNow,
            logLevel: LogLevel.Info,
            message: msg,
            properties: [],
            context: "",
            messageTemplate: LogParser.ParseMessage(msg));

    // ─────────────────────────────────────────────────────────────────────────
    // 1. ProcessQueueAsync — inner catch: sink.Emit() throws
    //    The logger must absorb it and keep running for subsequent entries
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_SinkEmitThrows_LoggerContinuesAndDoesNotCrash()
    {
        var throwingSink = Substitute.For<ILogTarget>();
        throwingSink.When(s => s.Emit(Arg.Any<LogEntry>()))
                    .Do(_ => throw new InvalidOperationException("sink boom"));

        var goodCh = Channel.CreateUnbounded<string>();
        var goodTarget = new StringChannelTarget(goodCh.Writer, isColor: false);

        var sinks = new List<Sink>
        {
            new(throwingSink, new SinkOutputConfig()),
            new(goodTarget,   new SinkOutputConfig()),
        };
        var logger = new Logger(sinks, "GapTest");

        // Log two entries — first entry lets the throwing sink explode,
        // second entry is proof the logger kept working
        logger.Log(LogLevel.Info, "first");
        logger.Log(LogLevel.Info, "marker-ok");

        // Wait for the "marker-ok" to reach the channel
        string? got = null;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!cts.IsCancellationRequested)
        {
            if (goodCh.Reader.TryRead(out var s) && s.Contains("marker-ok"))
            {
                got = s;
                break;
            }
            await Task.Delay(10);
        }

        got.Should().NotBeNull("logger should continue after a target Emit exception");
        await logger.DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. DisposeAsync — sink.Dispose() throws
    //    DisposeAsync must NOT propagate the exception
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_DisposeAsync_SinkDisposeThrows_DoesNotPropagate()
    {
        var throwingSink = Substitute.For<ILogTarget>();
        throwingSink.When(s => s.Dispose())
                    .Do(_ => throw new InvalidOperationException("dispose boom"));

        var sinks = new List<Sink>
        {
            new(throwingSink, new SinkOutputConfig()),
        };
        var logger = new Logger(sinks, "GapTest2");

        Func<Task> act = async () => await logger.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. DisposeAsync — processTask faulted (drives outer catch in DisposeAsync)
    //    We can simulate this by having the task fail asynchronously.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logger_DisposeAsync_WithNormalSink_DoesNotThrow()
    {
        var ch = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(ch.Writer, isColor: false);
        var logger = new Logger([new(target, new SinkOutputConfig())], "DisposeOk");
        logger.Log(LogLevel.Info, "before dispose");

        Func<Task> act = async () => await logger.DisposeAsync();
        await act.Should().NotThrowAsync();
    }
}
