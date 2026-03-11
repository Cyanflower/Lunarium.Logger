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

using System.Text.RegularExpressions;
using FluentAssertions;
using Lunarium.Logger.Models;
using Lunarium.Logger.Parser;
using Lunarium.Logger.Target;
using Xunit;

namespace Lunarium.Logger.IntegrationTests.ConsoleSinkTests;

// Global constraint for console hijacking.
[CollectionDefinition("ConsoleTests", DisableParallelization = true)]
public class ConsoleTestsCollectionDef { }

[Collection("ConsoleTests")]
public class ConsoleSinkTests : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly StringWriter _outWriter;
    private readonly StringWriter _errorWriter;
    private readonly ConsoleTarget _target;

    public ConsoleSinkTests()
    {
        // Save original streams
        _originalOut = Console.Out;
        _originalError = Console.Error;

        // Hijack
        _outWriter = new StringWriter();
        _errorWriter = new StringWriter();
        Console.SetOut(_outWriter);
        Console.SetError(_errorWriter);

        _target = new ConsoleTarget();
    }

    public void Dispose()
    {
        // Restore original streams
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);

        _outWriter.Dispose();
        _errorWriter.Dispose();
        _target.Dispose(); // Should be a no-op, just for coverage completely
    }

    private static LogEntry MakeEntry(LogLevel level, string message)
    {
        return new LogEntry(
            loggerName: "ConsoleScope",
            timestamp: DateTimeOffset.UtcNow,
            logLevel: level,
            message: message,
            properties: [],
            context: "",
            messageTemplate: LogParser.ParseMessage(message),
            exception: null
        );
    }

    [Fact]
    public void Emit_InfoLevel_ToConsoleOut()
    {
        var entry = MakeEntry(LogLevel.Info, "Hello out");
        _target.Emit(entry);

        var output = _outWriter.ToString();
        var error = _errorWriter.ToString();

        output.Should().Contain("Hello out");
        error.Should().BeEmpty();
    }

    [Fact]
    public void Emit_ErrorLevel_ToConsoleError()
    {
        var entry = MakeEntry(LogLevel.Error, "Hello error");
        _target.Emit(entry);

        var output = _outWriter.ToString();
        var error = _errorWriter.ToString();

        error.Should().Contain("Hello error");
        output.Should().BeEmpty();
    }

    [Fact]
    public void Emit_FatalLevel_ToConsoleError()
    {
        var entry = MakeEntry(LogLevel.Critical, "Hello fatal");
        _target.Emit(entry);

        var error = _errorWriter.ToString();
        error.Should().Contain("Hello fatal");
    }

    [Fact]
    public void Emit_WithJson_OutputsJsonFormat()
    {
        var entry = MakeEntry(LogLevel.Debug, "Hello json");
        _target.ToJson = true;
        _target.Emit(entry);

        var output = _outWriter.ToString();
        // Since we are checking JSON format:
        output.Should().Contain("\"Level\":\"Debug\"");
        output.Should().Contain("\"OriginalMessage\":\"Hello json\"");
    }

    [Fact]
    public void Emit_WithIsColorFalse_OutputsPlainTextWithoutAnsi()
    {
        var target = new ConsoleTarget { IsColor = false };
        var entry = MakeEntry(LogLevel.Info, "plain text test");
        target.Emit(entry);

        var output = _outWriter.ToString();
        output.Should().Contain("plain text test");
        output.Should().NotContain("\x1b[");
    }

    [Fact]
    public void Emit_WithToJsonAndErrorLevel_OutputsJsonToErrorStream()
    {
        var target = new ConsoleTarget { ToJson = true };
        var entry = MakeEntry(LogLevel.Error, "json error message");
        target.Emit(entry);

        _errorWriter.ToString().Should().Contain("\"Level\":\"Error\"");
        _outWriter.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Emit_ToJson_CanBeToggledAtRuntime()
    {
        var target = new ConsoleTarget { ToJson = true };
        target.Emit(MakeEntry(LogLevel.Info, "first json"));
        target.ToJson = false;
        target.Emit(MakeEntry(LogLevel.Info, "second plain"));

        var output = _outWriter.ToString();
        output.Should().Contain("\"Level\"");       // first was JSON
        output.Should().Contain("second plain");    // second was plain text
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        var target = new ConsoleTarget();
        Action act = () => { target.Dispose(); target.Dispose(); };
        act.Should().NotThrow();
    }
}
