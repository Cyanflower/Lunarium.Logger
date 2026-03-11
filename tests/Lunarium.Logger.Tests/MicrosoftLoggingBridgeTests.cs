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
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Lunarium.Logger.Tests;

/// <summary>
/// Tests for MicrosoftLoggingBridge:
///   - LunariumLoggerProvider (ILoggerProvider, ISupportExternalScope)
///   - LunariumMsLoggerAdapter (Microsoft.Extensions.Logging.ILogger)
///   - LunariumLoggerExtensions.AddLunariumLogger ILoggingBuilder extension
///   - LunariumLoggerConversionExtensions.ToMicrosoftLogger extension
/// </summary>
public class MicrosoftLoggingBridgeTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. LunariumLoggerProvider
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Provider_CreateLogger_ReturnsMsILoggerInstance()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);

        var msLogger = provider.CreateLogger("MyCategory");

        msLogger.Should().NotBeNull();
        msLogger.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILogger>();
    }

    [Fact]
    public void Provider_CreateLogger_AppliesCategoryAsContext()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);

        var msLogger = provider.CreateLogger("MyService");
        msLogger.Log(MsLogLevel.Information, "hello");

        // The underlying ILogger.Log should have been called with "MyService" as context
        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Any<string>(),
            "MyService",
            Arg.Any<Exception?>());
    }

    [Fact]
    public void Provider_SetScopeProvider_DoesNotThrow()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);

        var scopeProvider = Substitute.For<IExternalScopeProvider>();
        Action act = () => provider.SetScopeProvider(scopeProvider);
        act.Should().NotThrow();
    }

    [Fact]
    public void Provider_Dispose_DoesNotThrow()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        var provider = new LunariumLoggerProvider(lunariumLogger);
        Action act = () => provider.Dispose();
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. LunariumMsLoggerAdapter — IsEnabled
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(MsLogLevel.Trace)]
    [InlineData(MsLogLevel.Debug)]
    [InlineData(MsLogLevel.Information)]
    [InlineData(MsLogLevel.Warning)]
    [InlineData(MsLogLevel.Error)]
    [InlineData(MsLogLevel.Critical)]
    [InlineData(MsLogLevel.None)]
    public void Adapter_IsEnabled_AlwaysReturnsTrue(MsLogLevel level)
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.IsEnabled(level).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. LunariumMsLoggerAdapter — Log — ConvertLogLevel mapping
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(MsLogLevel.Trace, Lunarium.Logger.LogLevel.Debug)]
    [InlineData(MsLogLevel.Debug, Lunarium.Logger.LogLevel.Debug)]
    [InlineData(MsLogLevel.Information, Lunarium.Logger.LogLevel.Info)]
    [InlineData(MsLogLevel.Warning, Lunarium.Logger.LogLevel.Warning)]
    [InlineData(MsLogLevel.Error, Lunarium.Logger.LogLevel.Error)]
    [InlineData(MsLogLevel.Critical, Lunarium.Logger.LogLevel.Critical)]
    [InlineData(MsLogLevel.None, Lunarium.Logger.LogLevel.Info)]   // fallback → Info
    public void Adapter_Log_MapsLevelCorrectly(MsLogLevel msLevel, Lunarium.Logger.LogLevel expectedLevel)
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.Log(msLevel, "test message");

        lunariumLogger.Received(1).Log(
            expectedLevel,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Exception?>());
    }

    [Fact]
    public void Adapter_Log_ForwardsFormattedMessage()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("Cat");

        msLogger.LogInformation("Hello {Name}", "World");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Is<string>(s => s.Contains("World")),
            Arg.Any<string>(),
            Arg.Any<Exception?>());
    }

    [Fact]
    public void Adapter_Log_ForwardsException()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("Cat");
        var ex = new InvalidOperationException("kaboom");

        msLogger.LogError(ex, "Oops");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Error,
            Arg.Any<string>(),
            Arg.Any<string>(),
            ex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Scope — EventId with Name gets appended to context
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Adapter_Log_EventIdWithName_AppendedToContext()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("App");

        msLogger.Log<string>(MsLogLevel.Information, new EventId(0, "RequestStarted"), null!, null,
            (state, _) => "msg");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => ctx.Contains("RequestStarted")),
            Arg.Any<Exception?>());
    }

    [Fact]
    public void Adapter_Log_EventIdWithIdOnly_AppendedToContext()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("App");

        msLogger.Log<string>(MsLogLevel.Warning, new EventId(42, ""), null!, null,
            (state, _) => "msg");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Warning,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => ctx.Contains("42")),
            Arg.Any<Exception?>());
    }

    [Fact]
    public void Adapter_Log_EmptyEventId_ContextIsEmpty()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("App");

        // EventId(0, null/empty) → no event-related suffix
        msLogger.Log<string>(MsLogLevel.Information, new EventId(0, ""), null!, null,
            (state, _) => "msg");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => !ctx.Contains("0") || ctx == ""),
            Arg.Any<Exception?>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Scope — BeginScope
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Adapter_BeginScope_ReturnsDisposable()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);
        var msLogger = provider.CreateLogger("Cat");

        using var scope = msLogger.BeginScope("my-scope");
        scope.Should().NotBeNull();
    }

    [Fact]
    public void Adapter_BeginScope_ScopeIncludedInContext_WhenScopeProviderSet()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        using var provider = new LunariumLoggerProvider(lunariumLogger);

        // Push a scope via a real ExternalScopeProvider
        var realScopeProvider = new LoggerExternalScopeProvider();
        provider.SetScopeProvider(realScopeProvider);

        var msLogger = provider.CreateLogger("Cat");
        using (msLogger.BeginScope("RequestId=abc"))
        {
            msLogger.LogInformation("inside scope");
        }

        // The context logged should include the scope string
        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Any<string>(),
            Arg.Is<string>(ctx => ctx.Contains("RequestId=abc")),
            Arg.Any<Exception?>());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. LunariumLoggerExtensions.AddLunariumLogger (ILoggingBuilder extension)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddLunariumLogger_RegistersProvider_LoggingWorks()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();

        using var host = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddLunariumLogger(lunariumLogger);
        });

        var msLogger = host.CreateLogger("TestCategory");
        msLogger.LogInformation("Test message");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Info,
            Arg.Any<string>(),
            "TestCategory",
            Arg.Any<Exception?>());
    }

    [Fact]
    public void AddLunariumLogger_ReturnsSameBuilder()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        ILoggingBuilder? capturedBuilder = null;

        using var _ = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            var returned = builder.AddLunariumLogger(lunariumLogger);
            capturedBuilder = builder;
            returned.Should().BeSameAs(builder);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. LunariumLoggerConversionExtensions.ToMicrosoftLogger
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToMicrosoftLogger_ReturnsMsILogger()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        var msLogger = lunariumLogger.ToMicrosoftLogger("MyContext");

        msLogger.Should().NotBeNull();
        msLogger.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILogger>();
    }

    [Fact]
    public void ToMicrosoftLogger_LogsToUnderlyingLunariumLogger()
    {
        var lunariumLogger = Substitute.For<Lunarium.Logger.ILogger>();
        var msLogger = lunariumLogger.ToMicrosoftLogger("ConvCtx");

        msLogger.LogWarning("Watch out");

        lunariumLogger.Received(1).Log(
            Lunarium.Logger.LogLevel.Warning,
            Arg.Any<string>(),
            "ConvCtx",
            Arg.Any<Exception?>());
    }
}
