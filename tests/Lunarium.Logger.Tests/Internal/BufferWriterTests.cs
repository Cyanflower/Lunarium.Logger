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

using System.Text;
using Lunarium.Logger.GlobalConfig;
using Lunarium.Logger.Internal;
using Xunit;
using FluentAssertions;

namespace Lunarium.Logger.Tests.Internal;

public class BufferWriterTests
{
    [Fact]
    public void Remove_ShouldRemoveCorectByteRange()
    {
        using var writer = new BufferWriter(16);
        writer.Append("abcdefg"); // 7 bytes
        
        // Remove "cd" (index 2, count 2)
        writer.Remove(2, 2);
        
        writer.ToString().Should().Be("abefg");
        writer.Length.Should().Be(5);
        writer.WrittenCount.Should().Be(5);
    }

    [Fact]
    public void Remove_AtEnd_ShouldBeEquivalentToRemoveLast()
    {
        using var writer = new BufferWriter(16);
        writer.Append("abcde");
        
        writer.Remove(3, 2); // Remove "de"
        
        writer.ToString().Should().Be("abc");
        writer.Length.Should().Be(3);
    }

    [Fact]
    public void RemoveLast_ShouldDecreaseIndex()
    {
        using var writer = new BufferWriter(16);
        writer.Append("abcde");
        
        writer.RemoveLast(2);
        
        writer.ToString().Should().Be("abc");
        writer.Length.Should().Be(3);
    }

    [Fact]
    public void Append_NonAsciiChar_ShouldEncodeCorrectly()
    {
        using var writer = new BufferWriter(16);
        writer.Append('€'); // 3 bytes in UTF-8
        writer.Append('中'); // 3 bytes in UTF-8
        
        var expected = "€中";
        writer.ToString().Should().Be(expected);
        writer.Length.Should().Be(6);
    }

    [Fact]
    public void GetSpan_ShouldReturnCorrectSliceAndEnsureCapacity()
    {
        using var writer = new BufferWriter(10);
        writer.Append("12345");
        
        // Request span that fits
        var span = writer.GetSpan(5);
        span.Length.Should().BeGreaterThanOrEqualTo(5);
        
        // Request span that causes expansion
        var bigSpan = writer.GetSpan(100);
        bigSpan.Length.Should().BeGreaterThanOrEqualTo(100);
        writer.Capacity.Should().BeGreaterThanOrEqualTo(105);
    }

    [Fact]
    public void Indexer_ShouldReturnCorrectByte()
    {
        using var writer = new BufferWriter(16);
        writer.Append("abc");
        
        writer[0].Should().Be((byte)'a');
        writer[1].Should().Be((byte)'b');
        writer[2].Should().Be((byte)'c');
    }

    [Fact]
    public void WrittenSpan_ShouldReturnCorrectView()
    {
        using var writer = new BufferWriter(16);
        writer.Append("abc");
        
        var span = writer.WrittenSpan;
        span.Length.Should().Be(3);
        span[0].Should().Be((byte)'a');
        span[1].Should().Be((byte)'b');
        span[2].Should().Be((byte)'c');
    }

    [Fact]
    public void Dispose_WithInterlockedEnabled_ShouldWork()
    {
        // We need to set AtomicOpsConfig.BufferWriterDisposeInterlocked via reflection if we are not using GlobalConfigurator
        // But here we can just set it if we have access.
        // Since it's internal we might need to use reflection or just change it if it's internal visible.
        
        var field = typeof(AtomicOpsConfig).GetProperty("BufferWriterDisposeInterlocked", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        bool originalValue = AtomicOpsConfig.BufferWriterDisposeInterlocked;
        
        try
        {
            // Use internal method to set it
            AtomicOpsConfig.EnableBufferWriterInterlocked();
            
            var writer = new BufferWriter(16);
            writer.Append("test");
            writer.Dispose();
            writer.Dispose(); // Should be safe
            
            // Check if buffer is null (via reflection since it's private)
            var bufferField = typeof(BufferWriter).GetField("_buffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var bufferValue = bufferField?.GetValue(writer);
            bufferValue.Should().BeNull();
        }
        finally
        {
            if (!originalValue) AtomicOpsConfig.DisableBufferWriterInterlocked();
        }
    }
}
