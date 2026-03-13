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
using Xunit;
using FluentAssertions;
using System.Text;

namespace Lunarium.Logger.Tests.Target;

public class ChannelTargetTests
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

    [Fact]
    public async Task ByteChannelTarget_ShouldWriteEncodedBytes()
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        var target = new ByteChannelTarget(channel.Writer, isColor: false);
        
        var entry = MakeEntry("byte test");
        target.Emit(entry);
        
        if (channel.Reader.TryRead(out var bytes))
        {
            var result = Encoding.UTF8.GetString(bytes);
            result.Should().Contain("byte test");
        }
        else
        {
            Assert.Fail("Failed to read from channel");
        }
    }

    [Fact]
    public async Task ByteChannelTarget_WithJson_ShouldWriteJsonBytes()
    {
        var channel = Channel.CreateUnbounded<byte[]>();
        var target = new ByteChannelTarget(channel.Writer, isColor: false) { ToJson = true };
        
        var entry = MakeEntry("json byte test");
        target.Emit(entry);
        
        if (channel.Reader.TryRead(out var bytes))
        {
            var result = Encoding.UTF8.GetString(bytes);
            result.Should().Contain("\"OriginalMessage\":\"json byte test\"");
        }
        else
        {
            Assert.Fail("Failed to read from channel");
        }
    }

    [Fact]
    public void StringChannelTarget_ToJson_CanBeSet()
    {
        var channel = Channel.CreateUnbounded<string>();
        var target = new StringChannelTarget(channel.Writer, isColor: false);
        target.ToJson = true;
        target.ToJson.Should().BeTrue();
    }
}
