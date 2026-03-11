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

using Lunarium.Logger.Target;
using Lunarium.Logger.SinkConfig;
using Lunarium.Logger.Wrapper;
using NSubstitute;

namespace Lunarium.Logger.Tests;

/// <summary>
/// Tests for LoggerExtensions (ForContext, ForContext&lt;T&gt;) and
/// LoggerBuilderExtensions (AddConsoleSink, AddChannelSink, AddSink(ISinkConfig)).
/// </summary>
public class LoggerExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. LoggerExtensions.ForContext(string)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForContext_String_ReturnsLoggerWrapper()
    {
        var logger = Substitute.For<ILogger>();
        var wrapped = logger.ForContext("MyModule");
        wrapped.Should().BeOfType<LoggerWrapper>();
    }

    [Fact]
    public void ForContext_String_NullLogger_Throws()
    {
        ILogger? nullLogger = null;
        Action act = () => nullLogger!.ForContext("ctx");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForContext_String_DelegatesToLog()
    {
        var logger = Substitute.For<ILogger>();
        var wrapped = logger.ForContext("SomeContext");
        wrapped.Info("hello");
        logger.Received(1).Log(
            LogLevel.Info, "hello", "SomeContext", null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. LoggerExtensions.ForContext<T>()
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForContextGeneric_UsesFullTypeName()
    {
        var logger = Substitute.For<ILogger>();
        var wrapped = logger.ForContext<LoggerExtensionsTests>();
        wrapped.Should().BeOfType<LoggerWrapper>();

        // The context should be the full type name
        wrapped.Info("x");
        logger.Received(1).Log(
            LogLevel.Info, "x",
            typeof(LoggerExtensionsTests).FullName!, null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. LoggerBuilderExtensions.AddConsoleSink
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddConsoleSink_ReturnsSameBuilder()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddConsoleSink();
        returned.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddConsoleSink_WithOutputConfig_ReturnsSameBuilder()
    {
        var builder = new LoggerBuilder();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Warning };
        var returned = builder.AddConsoleSink(cfg);
        returned.Should().BeSameAs(builder);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. LoggerBuilderExtensions.AddChannelSink
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddStringChannelSink_ReturnsBuilderAndReader()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddStringChannelSink(out var reader);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    [Fact]
    public void AddStringChannelSink_WithCapacityAndColor_Works()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddStringChannelSink(out var reader, capacity: 100, isColor: true);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. LoggerBuilderExtensions.AddSink(ISinkConfig) dispatch
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddSink_ConsoleSinkConfig_ReturnsSameBuilder()
    {
        var builder = new LoggerBuilder();
        ISinkConfig config = new ConsoleSinkConfig();
        var returned = builder.AddSink(config);
        returned.Should().BeSameAs(builder);
    }

    [Fact]
    public async Task AddSink_FileTargetConfig_ReturnsSameBuilder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lunarium-ext-{Guid.NewGuid():N}.log");
        var builder = new LoggerBuilder();
        ISinkConfig config = new FileSinkConfig { LogFilePath = path };
        var returned = builder.AddSink(config);
        returned.Should().BeSameAs(builder);
        await builder.Build().DisposeAsync();
    }

    [Fact]
    public void AddSink_CustomISinkConfig_UsesCreateTarget()
    {
        var builder = new LoggerBuilder();
        var customTarget = Substitute.For<ILogTarget>();
        var customConfig = Substitute.For<ISinkConfig>();
        customConfig.CreateTarget().Returns(customTarget);
        customConfig.SinkOutputConfig.Returns((SinkOutputConfig?)null);

        var returned = builder.AddSink(customConfig);
        returned.Should().BeSameAs(builder);
        customConfig.Received(1).CreateTarget();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. LoggerBuilderExtensions.AddTimedRotatingFileSink / AddSizedRotatingFileSink
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddTimedRotatingFileSink_ReturnsSameBuilder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lunarium-timed-{Guid.NewGuid():N}.log");
        var builder = new LoggerBuilder();
        var returned = builder.AddTimedRotatingFileSink(path, maxFile: 3);
        returned.Should().BeSameAs(builder);
        await builder.Build().DisposeAsync();
    }

    [Fact]
    public async Task AddSizedRotatingFileSink_ReturnsSameBuilder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lunarium-sized-{Guid.NewGuid():N}.log");
        var builder = new LoggerBuilder();
        var returned = builder.AddSizedRotatingFileSink(path, maxFileSizeMB: 5, maxFile: 2);
        returned.Should().BeSameAs(builder);
        await builder.Build().DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. LoggerBuilderExtensions.AddLogEntryChannelSink
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddLogEntryChannelSink_ReturnsBuilderAndReader()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddLogEntryChannelSink(out var reader);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    [Fact]
    public void AddLogEntryChannelSink_WithCapacity_ReturnsBuilderAndReader()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddLogEntryChannelSink(out var reader, capacity: 50);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    [Fact]
    public void AddLogEntryChannelSink_WithSinkOutputConfig_ReturnsBuilder()
    {
        var builder = new LoggerBuilder();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Warning };
        var returned = builder.AddLogEntryChannelSink(out _, sinkOutputConfig: cfg);
        returned.Should().BeSameAs(builder);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. LoggerBuilderExtensions.AddChannelSink<T>
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddChannelSink_WithTransform_ReturnsBuilderAndReader()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddChannelSink<int>(out var reader, transform: e => (int)e.LogLevel);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    [Fact]
    public void AddChannelSink_WithCapacity_ReturnsBuilder()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddChannelSink<string>(out _, transform: e => e.Message, capacity: 10);
        returned.Should().BeSameAs(builder);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 9. AddStringChannelSink — toJson / SinkOutputConfig 分支
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddStringChannelSink_WithToJsonTrue_ReturnsBuilder()
    {
        var builder = new LoggerBuilder();
        var returned = builder.AddStringChannelSink(out var reader, toJson: true);
        returned.Should().BeSameAs(builder);
        reader.Should().NotBeNull();
    }

    [Fact]
    public void AddStringChannelSink_WithSinkOutputConfig_ReturnsBuilder()
    {
        var builder = new LoggerBuilder();
        var cfg = new SinkOutputConfig { LogMinLevel = LogLevel.Debug };
        var returned = builder.AddStringChannelSink(out _, sinkOutputConfig: cfg);
        returned.Should().BeSameAs(builder);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 10. LoggerBuilderExtensions.AddRotatingFileSink
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRotatingFileSink_BothStrategies_ReturnsSameBuilder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lunarium-rotating-{Guid.NewGuid():N}.log");
        var builder = new LoggerBuilder();
        var returned = builder.AddRotatingFileSink(
            path,
            maxFileSizeMB: 10,
            rotateOnNewDay: true,
            maxFile: 5);
        returned.Should().BeSameAs(builder);
        await builder.Build().DisposeAsync();
    }
}
