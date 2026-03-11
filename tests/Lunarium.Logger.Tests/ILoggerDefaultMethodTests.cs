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

using NSubstitute;

namespace Lunarium.Logger.Tests;

/// <summary>
/// Tests for ILogger default interface methods (DIM).
/// These delegate to Log() with specific parameters — we test via NSubstitute
/// to capture the Log() call and verify all 11 convenience methods forward correctly.
/// </summary>
public class ILoggerDefaultMethodTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helper: create a mock that actually invokes the DIM implementations
    // DIMs are only reachable through an ILogger-typed reference.
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class CapturingLogger : ILogger
    {
        public record LogCall(LogLevel Level, string Message, string Context, Exception? Ex, object?[] Props);
        public List<LogCall> Calls { get; } = [];

        public void Log(LogLevel level, string message = "", string context = "", Exception? ex = null, params object?[] propertyValues)
            => Calls.Add(new(level, message, context, ex, propertyValues));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // Returns (ILogger for calling DIMs, CapturingLogger for reading Calls)
    private static (ILogger iface, CapturingLogger concrete) Make()
    {
        var l = new CapturingLogger();
        return (l, l);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Debug
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Debug_ForwardsToLog()
    {
        var (l, c) = Make();
        l.Debug("dbg msg", "v1");
        c.Calls.Should().HaveCount(1);
        c.Calls[0].Level.Should().Be(LogLevel.Debug);
        c.Calls[0].Message.Should().Be("dbg msg");
        c.Calls[0].Ex.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Info
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Info_ForwardsToLog()
    {
        var (l, c) = Make();
        l.Info("info msg");
        c.Calls[0].Level.Should().Be(LogLevel.Info);
        c.Calls[0].Message.Should().Be("info msg");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Warning
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Warning_ForwardsToLog()
    {
        var (l, c) = Make();
        l.Warning("warn msg");
        c.Calls[0].Level.Should().Be(LogLevel.Warning);
        c.Calls[0].Message.Should().Be("warn msg");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Error overloads
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Error_MessageOnly_ForwardsToLog()
    {
        var (l, c) = Make();
        l.Error("err msg");
        c.Calls[0].Level.Should().Be(LogLevel.Error);
        c.Calls[0].Message.Should().Be("err msg");
        c.Calls[0].Ex.Should().BeNull();
    }

    [Fact]
    public void Error_ExceptionOnly_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new InvalidOperationException("boom");
        l.Error(ex);
        c.Calls[0].Level.Should().Be(LogLevel.Error);
        c.Calls[0].Message.Should().Be("");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }

    [Fact]
    public void Error_MessageAndException_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new Exception("oops");
        l.Error("ctx", ex, "val");
        c.Calls[0].Level.Should().Be(LogLevel.Error);
        c.Calls[0].Message.Should().Be("ctx");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }

    [Fact]
    public void Error_ExceptionThenMessage_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new Exception("oops");
        l.Error(ex, "err ctx");
        c.Calls[0].Level.Should().Be(LogLevel.Error);
        c.Calls[0].Message.Should().Be("err ctx");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Critical overloads
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Critical_MessageOnly_ForwardsToLog()
    {
        var (l, c) = Make();
        l.Critical("crit msg");
        c.Calls[0].Level.Should().Be(LogLevel.Critical);
        c.Calls[0].Message.Should().Be("crit msg");
        c.Calls[0].Ex.Should().BeNull();
    }

    [Fact]
    public void Critical_ExceptionOnly_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new Exception("fatal");
        l.Critical(ex);
        c.Calls[0].Level.Should().Be(LogLevel.Critical);
        c.Calls[0].Message.Should().Be("");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }

    [Fact]
    public void Critical_MessageAndException_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new Exception("fatal");
        l.Critical("crit msg", ex, "v");
        c.Calls[0].Level.Should().Be(LogLevel.Critical);
        c.Calls[0].Message.Should().Be("crit msg");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }

    [Fact]
    public void Critical_ExceptionThenMessage_ForwardsToLog()
    {
        var (l, c) = Make();
        var ex = new Exception("fatal");
        l.Critical(ex, "ctx msg");
        c.Calls[0].Level.Should().Be(LogLevel.Critical);
        c.Calls[0].Message.Should().Be("ctx msg");
        c.Calls[0].Ex.Should().BeSameAs(ex);
    }
}
