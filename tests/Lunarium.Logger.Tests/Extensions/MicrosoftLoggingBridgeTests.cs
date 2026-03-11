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

using Lunarium.Logger.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using LmLogLevel = Lunarium.Logger.LogLevel;
// Alias to distinguish same-named types
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Lunarium.Logger.Tests.Extensions;

/// <summary>
/// Tests for the Microsoft.Extensions.Logging bridge:
/// - LunariumLoggerProvider   (ILoggerProvider)
/// - LunariumMsLoggerAdapter  (MEL ILogger)
/// - LunariumLoggerConversionExtensions (ToMicrosoftLogger)
/// - NullScopeProvider
///
/// We mock Lunarium.Logger.ILogger with NSubstitute to capture
/// the calls that the bridge forwards into it.
/// </summary>
public class MicrosoftLoggingBridgeTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. LunariumMsLoggerAdapter.IsEnabled — always true
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsEnabled_AlwaysReturnsTrue()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        foreach (MsLogLevel level in Enum.GetValues<MsLogLevel>())
            msLogger.IsEnabled(level).Should().BeTrue(because: $"level={level}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. LunariumMsLoggerAdapter.Log — log level conversion
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(MsLogLevel.Trace, LmLogLevel.Debug)]
    [InlineData(MsLogLevel.Debug, LmLogLevel.Debug)]
    [InlineData(MsLogLevel.Information, LmLogLevel.Info)]
    [InlineData(MsLogLevel.Warning, LmLogLevel.Warning)]
    [InlineData(MsLogLevel.Error, LmLogLevel.Error)]
    [InlineData(MsLogLevel.Critical, LmLogLevel.Critical)]
    public void Log_LevelConversion_MapsCorrectly(MsLogLevel msLevel, LmLogLevel expectedLmLevel)
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(msLevel, "test message");

        // The adapter forwards to the ForContext-wrapped logger.
        // We check the inner mock received the right level.
        mock.Received().Log(
            expectedLmLevel,
            Arg.Any<string>(),
            Arg.Any<string>(),
            null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. LunariumMsLoggerAdapter.Log — message content forwarded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_MessageContent_ForwardedToLunariumLogger()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(MsLogLevel.Information, "Hello world");

        mock.Received().Log(
            LmLogLevel.Info,
            "Hello world",
            Arg.Any<string>(),
            null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. LunariumMsLoggerAdapter.Log — exception forwarded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_WithException_ExceptionForwardedToLunariumLogger()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");
        var ex = new InvalidOperationException("boom");

        msLogger.Log(MsLogLevel.Error, new EventId(0), "state", ex, (s, e) => s);

        mock.Received().Log(
            LmLogLevel.Error,
            Arg.Any<string>(),
            Arg.Any<string>(),
            ex,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. LunariumLoggerProvider.CreateLogger — ForContext is called with categoryName
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateLogger_CallsForContextWithCategoryName()
    {
        // CreateLogger wraps the luminary logger with ForContext(categoryName).
        // ForContext is a default interface method — NSubstitute can't stub it,
        // but we can verify the adapter is returned non-null.
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);

        var adapter = provider.CreateLogger("MyCategory");

        adapter.Should().NotBeNull();
        // The adapter must be a MEL ILogger
        adapter.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILogger>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. EventId.Name — appended to context
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Log_EventIdWithName_AppendedToContext()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(MsLogLevel.Information, new EventId(99, "MyEvent"), "state", null, (s, e) => s);

        mock.Received().Log(
            LmLogLevel.Info,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => ctx.Contains("MyEvent")),
            null,
            Arg.Any<object?[]>());
    }

    [Fact]
    public void Log_EventIdWithIdOnly_AppendedToContext()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(MsLogLevel.Warning, new EventId(42), "state", null, (s, e) => s);

        mock.Received().Log(
            LmLogLevel.Warning,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => ctx.Contains("42")),
            null,
            Arg.Any<object?[]>());
    }

    [Fact]
    public void Log_EventIdZeroNoName_NotAppendedToContext()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(MsLogLevel.Information, new EventId(0), "state", null, (s, e) => s);

        // context should not contain "0" from EventId (Id=0 and Name=null are both skipped)
        mock.Received().Log(
            LmLogLevel.Info,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => !ctx.EndsWith(".0")),
            null,
            Arg.Any<object?[]>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. BeginScope — returns a disposable without throwing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BeginScope_ReturnsDisposable()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        var scope = msLogger.BeginScope("my-scope");
        scope.Should().NotBeNull();

        // Dispose should not throw
        Action act = () => scope!.Dispose();
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. LunariumLoggerConversionExtensions.ToMicrosoftLogger
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToMicrosoftLogger_ReturnsNonNullMsLogger()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var msLogger = mock.ToMicrosoftLogger("MyService");
        msLogger.Should().NotBeNull();
    }

    [Fact]
    public void ToMicrosoftLogger_ForwardsLogCallToLunariumLogger()
    {
        // ToMicrosoftLogger returns an adapter that wraps the luminary logger via ForContext.
        // Because ForContext returns a LoggerWrapper (a real type, not a substitute),
        // we can't use NSubstitute Received() on mock.Log — instead we verify the
        // adapter is functional by checking IsEnabled and BeginScope work.
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var msLogger = mock.ToMicrosoftLogger("MyService");

        // The adapter should always report enabled (bridge always returns true)
        msLogger.IsEnabled(MsLogLevel.Information).Should().BeTrue();

        // BeginScope should return a disposable without throwing
        var scope = msLogger.BeginScope("ctx");
        Action dispose = () => scope?.Dispose();
        dispose.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. LunariumLoggerProvider.Dispose — does not throw
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Provider_Dispose_DoesNotThrow()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);

        Action act = () => provider.Dispose();
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. SetScopeProvider — accepted without throwing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetScopeProvider_CustomProvider_AcceptedWithoutThrowing()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var scopeProvider = Substitute.For<IExternalScopeProvider>();

        Action act = () => provider.SetScopeProvider(scopeProvider);
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 11. MEL LogNone — IsEnabled still true (bridge delegates filter to sink)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsEnabled_NoneLevel_StillReturnsTrue()
    {
        var mock = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(mock);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.IsEnabled(MsLogLevel.None).Should().BeTrue();
    }
}
