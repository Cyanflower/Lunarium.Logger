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

using Lunarium.Logger.GlobalConfig;
using Lunarium.Logger.Models;
using Lunarium.Logger.Parser;
using Lunarium.Logger.Writer;

namespace Lunarium.Logger.Tests.Writer;

/// <summary>
/// Targeted coverage for the uncovered paths in LogJsonWriter, LogColorTextWriter,
/// LogTextWriter, and the base LogWriter.
/// </summary>
public class WriterCoverageGapTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static LogEntry MakeEntry(
        string template,
        object?[] props,
        LogLevel level = LogLevel.Info,
        string context = "",
        Exception? ex = null)
    {
        var parsed = LogParser.ParseMessage(template);
        return new LogEntry(
            loggerName: "Test",
            timestamp: DateTimeOffset.UtcNow,
            logLevel: level,
            message: template,
            properties: props,
            context: context,
            messageTemplate: parsed,
            exception: ex);
    }

    private static string Render<T>(LogEntry entry) where T : LogWriter, new()
    {
        var writer = WriterPool.Get<T>();
        writer.Render(entry);
        var result = writer.ToString();
        writer.Return();
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // A. LogJsonWriter — AppendJsonStringContent escape sequences
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogJsonWriter_EscapeTab_RenderedAsBackslashT()
    {
        var entry = MakeEntry("{S}", ["\t"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\\t");
    }

    [Fact]
    public void LogJsonWriter_EscapeCarriageReturn_RenderedAsBackslashR()
    {
        var entry = MakeEntry("{S}", ["\r"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\\r");
    }

    [Fact]
    public void LogJsonWriter_EscapeBackspace_RenderedAsBackslashB()
    {
        var entry = MakeEntry("{S}", ["\b"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\\b");
    }

    [Fact]
    public void LogJsonWriter_EscapeFormFeed_RenderedAsBackslashF()
    {
        var entry = MakeEntry("{S}", ["\f"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\\f");
    }

    [Fact]
    public void LogJsonWriter_ControlChar_RenderedAsHexEscape()
    {
        // 0x01 is a control character < 0x20 that isn't given a named escape
        var entry = MakeEntry("{S}", ["\x01"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\\u0001");
    }

    [Fact]
    public void LogJsonWriter_SurrogatePair_PassedThrough()
    {
        // "😀" = U+1F600, encoded as \uD83D\uDE00 surrogate pair in UTF-16
        const string emoji = "😀";
        var entry = MakeEntry("{E}", [emoji]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain(emoji);
    }

    [Fact]
    public void LogJsonWriter_ChineseCharacters_NotEscaped()
    {
        var entry = MakeEntry("{C}", ["你好"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("你好");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // B. LogJsonWriter — ToJsonValue: types not yet covered
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogJsonWriter_ShortProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(short)7]);
        Render<LogJsonWriter>(entry).Should().Contain("7");
    }

    [Fact]
    public void LogJsonWriter_ByteProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(byte)255]);
        Render<LogJsonWriter>(entry).Should().Contain("255");
    }

    [Fact]
    public void LogJsonWriter_UIntProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(uint)42u]);
        Render<LogJsonWriter>(entry).Should().Contain("42");
    }

    [Fact]
    public void LogJsonWriter_ULongProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(ulong)100ul]);
        Render<LogJsonWriter>(entry).Should().Contain("100");
    }

    [Fact]
    public void LogJsonWriter_UShortProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(ushort)8]);
        Render<LogJsonWriter>(entry).Should().Contain("8");
    }

    [Fact]
    public void LogJsonWriter_SByteProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [(sbyte)-5]);
        Render<LogJsonWriter>(entry).Should().Contain("-5");
    }

    [Fact]
    public void LogJsonWriter_DecimalProperty_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [3.14m]);
        Render<LogJsonWriter>(entry).Should().Contain("3.14");
    }

    [Fact]
    public void LogJsonWriter_FloatNaN_IsNaNString()
    {
        var entry = MakeEntry("{V}", [float.NaN]);
        Render<LogJsonWriter>(entry).Should().Contain("\"NaN\"");
    }

    [Fact]
    public void LogJsonWriter_FloatPositiveInfinity_IsInfinityString()
    {
        var entry = MakeEntry("{V}", [float.PositiveInfinity]);
        Render<LogJsonWriter>(entry).Should().Contain("\"Infinity\"");
    }

    [Fact]
    public void LogJsonWriter_FloatNegativeInfinity_IsNegInfinityString()
    {
        var entry = MakeEntry("{V}", [float.NegativeInfinity]);
        Render<LogJsonWriter>(entry).Should().Contain("\"-Infinity\"");
    }

    [Fact]
    public void LogJsonWriter_FloatNormal_RenderedAsNumber()
    {
        var entry = MakeEntry("{V}", [1.5f]);
        Render<LogJsonWriter>(entry).Should().Contain("1.5");
    }

    [Fact]
    public void LogJsonWriter_CharProperty_IsQuotedString()
    {
        var entry = MakeEntry("{V}", ['Z']);
        Render<LogJsonWriter>(entry).Should().Contain("\"Z\"");
    }

    [Fact]
    public void LogJsonWriter_DateTimeProperty_IsIso8601()
    {
        var dt = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var entry = MakeEntry("{V}", [dt]);
        Render<LogJsonWriter>(entry).Should().Contain("2025");
    }

    [Fact]
    public void LogJsonWriter_TimeSpanProperty_IsString()
    {
        var ts = TimeSpan.FromHours(2);
        var entry = MakeEntry("{V}", [ts]);
        Render<LogJsonWriter>(entry).Should().Contain("02:00:00");
    }

    [Fact]
    public void LogJsonWriter_UriProperty_IsString()
    {
        var uri = new Uri("https://example.com");
        var entry = MakeEntry("{V}", [uri]);
        Render<LogJsonWriter>(entry).Should().Contain("https://example.com");
    }

    [Fact]
    public void LogJsonWriter_UnknownType_FallsBackToString()
    {
        // Use a custom struct that has a ToString()
        var custom = new { Tag = "custom-obj" };
        var entry = MakeEntry("{V}", [custom]);
        Render<LogJsonWriter>(entry).Should().Contain("custom-obj");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // C. LogJsonWriter — WriteRenderedMessage with propertys.Length == 0
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogJsonWriter_TemplateWithNoProperties_OutpuRawTokenText()
    {
        // When a template has a property token but properties array is empty,
        // the raw token text "{N}" should appear in RenderedMessage
        var entry = MakeEntry("{N}", []);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("RenderedMessage");
        output.Should().Contain("{N}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // D. LogColorTextWriter — WriteRenderedMessage with propertys.Length == 0
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogColorTextWriter_TemplateWithNoProperties_OutputsRawTokenText()
    {
        var entry = MakeEntry("{N}", []);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("{N}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // E. LogColorTextWriter — SetValueColor: remaining type branches
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogColorTextWriter_CharValue_RendersColor()
    {
        var entry = MakeEntry("{V}", ['X']);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("X");
    }

    [Fact]
    public void LogColorTextWriter_DateTimeValue_RendersColor()
    {
        var entry = MakeEntry("{V}", [new DateTime(2025, 6, 1)]);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("2025");
    }

    [Fact]
    public void LogColorTextWriter_DateTimeOffsetValue_RendersColor()
    {
        var dto = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var entry = MakeEntry("{V}", [dto]);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("2025");
    }

    [Fact]
    public void LogColorTextWriter_TimeSpanValue_RendersColor()
    {
        var entry = MakeEntry("{V}", [TimeSpan.FromMinutes(5)]);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("00:05:00");
    }

    [Fact]
    public void LogColorTextWriter_UriValue_RendersColor()
    {
        var entry = MakeEntry("{V}", [new Uri("https://test.com")]);
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("https://test.com");
    }

    [Fact]
    public void LogColorTextWriter_UnknownType_FallsBackToOtherColor()
    {
        // An anonymous type falls to the default color branch
        object custom = new { Name = "test" };
        var entry = MakeEntry("{V}", [custom]);
        // Should render without throwing; check ANSI escape is present
        var output = Render<LogColorTextWriter>(entry);
        output.Should().Contain("\x1b["); // some ANSI prefix
    }

    // ═════════════════════════════════════════════════════════════════════════
    // F. LogWriter base — GetBufferCapacity
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogWriter_GetBufferCapacity_ReturnsNonNegative()
    {
        var writer = WriterPool.Get<LogTextWriter>();
        var capacity = writer.GetBufferCapacity();
        capacity.Should().BeGreaterThanOrEqualTo(0);
        writer.Return();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // G. LogWriter base — IsCommonCollectionType edge cases
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogTextWriter_NullCollectionAutoDestructure_OutputsNull()
    {
        DestructuringConfig.EnableAutoDestructuring();
        // null property — should output "null" not crash
        var entry = MakeEntry("{X}", [null]);
        var output = Render<LogTextWriter>(entry);
        output.Should().Contain("null");
    }

    [Fact]
    public void LogTextWriter_StringWithAutoDestructure_NotDestructured()
    {
        // String is excluded from collection detection — should not be JSON-serialized
        DestructuringConfig.EnableAutoDestructuring();
        var entry = MakeEntry("{S}", ["hello"]);
        var output = Render<LogTextWriter>(entry);
        // Should contain the raw value rendered as-is (not as a JSON array/object)
        output.Should().Contain("hello");
        // JSON-serialized string would be "hello" with quotes; plain render should be without braces
        output.Should().NotContain("{\"");  // no JSON object wrapper
    }

    [Fact]
    public void LogTextWriter_ArrayWithAutoDestructure_IsDestructured()
    {
        DestructuringConfig.EnableAutoDestructuring();
        int[] arr = [10, 20, 30];
        var entry = MakeEntry("{A}", [arr]);
        var output = Render<LogTextWriter>(entry);
        output.Should().Contain("10");
        output.Should().Contain("20");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // H. LogJsonWriter — WritePropertyValue: more-properties-than-tokens path
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void LogJsonWriter_MoreTokensThanProperties_ExtraTokensAreNull()
    {
        // Template has 2 property tokens, but only 1 property value
        // The second property slot should be "null"
        var entry = MakeEntry("{A} and {B}", ["x"]);
        var output = Render<LogJsonWriter>(entry);
        output.Should().Contain("\"A\"");
        output.Should().Contain("\"B\"");
        output.Should().Contain("null");
    }
}
