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
using Lunarium.Logger.Wrapper;
using NSubstitute;

namespace Lunarium.Logger.Tests.Wrapper;

/// <summary>
/// Tests for LoggerWrapper — the ForContext decorator.
/// Uses NSubstitute to mock the underlying ILogger and capture Log() arguments.
/// </summary>
public class LoggerWrapperTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (ILogger mock, LoggerWrapper wrapper) MakeWrapper(string context)
    {
        var mock = Substitute.For<ILogger>();
        var wrapper = new LoggerWrapper(mock, context);
        return (mock, wrapper);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Basic context attachment
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_EmptyPassedContext_UsesWrapperContext()
    {
        var (mock, wrapper) = MakeWrapper("MyCtx");
        wrapper.Log(LogLevel.Info, "msg", "");

        mock.Received(1).Log(
            LogLevel.Info,
            "msg",
            "MyCtx",
            (Exception?)null,
            Arg.Any<object?[]>());
    }

    [Fact]
    public void Log_NullPassedContext_UsesWrapperContext()
    {
        var (mock, wrapper) = MakeWrapper("MyCtx");
        // Passing null context — should still attach wrapper's context
        wrapper.Log(LogLevel.Warning, "msg", null!);

        mock.Received(1).Log(
            LogLevel.Warning,
            "msg",
            "MyCtx",
            (Exception?)null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Context combination (inner context non-empty)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_NonEmptyPassedContext_CombinedWithDot()
    {
        var (mock, wrapper) = MakeWrapper("Outer");
        wrapper.Log(LogLevel.Info, "msg", "Inner");

        mock.Received(1).Log(
            LogLevel.Info,
            "msg",
            "Outer.Inner",
            (Exception?)null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Nested wrappers (ForContext chained)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_NestedWrappers_ContextPathBuiltCorrectly()
    {
        var innerMock = Substitute.For<ILogger>();
        var first = new LoggerWrapper(innerMock, "A");
        var second = new LoggerWrapper(first, "B");

        second.Log(LogLevel.Info, "msg", "");

        // "B" is the outer wrapper, forwarded to "A" wrapper which becomes "A.B"
        // second calls first.Log with context "B", first combines to "A.B" before calling innerMock
        innerMock.Received(1).Log(
            LogLevel.Info,
            "msg",
            "A.B",
            (Exception?)null,
            Arg.Any<object?[]>());
    }

    [Fact]
    public void Log_TripleNested_ContextPathBuiltCorrectly()
    {
        var innerMock = Substitute.For<ILogger>();
        var w1 = new LoggerWrapper(innerMock, "A");
        var w2 = new LoggerWrapper(w1, "B");
        var w3 = new LoggerWrapper(w2, "C");

        w3.Log(LogLevel.Debug, "msg", "");

        innerMock.Received(1).Log(
            LogLevel.Debug,
            "msg",
            "A.B.C",
            (Exception?)null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Exception passthrough
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_WithException_ExceptionPassedThrough()
    {
        var (mock, wrapper) = MakeWrapper("Ctx");
        var ex = new InvalidOperationException("test");
        wrapper.Log(LogLevel.Error, "msg", "", ex);

        mock.Received(1).Log(
            LogLevel.Error,
            "msg",
            "Ctx",
            ex,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Property values passthrough
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_WithProperties_PropertiesPassedThrough()
    {
        var (mock, wrapper) = MakeWrapper("Ctx");
        wrapper.Log(LogLevel.Info, "Hello {Name}", "", null, "Alice");

        mock.Received(1).Log(
            LogLevel.Info,
            "Hello {Name}",
            "Ctx",
            null,
            Arg.Is<object?[]>(arr => arr.Length == 1 && (string?)arr[0] == "Alice"));
    }
}
