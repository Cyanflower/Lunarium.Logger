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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Lunarium.Logger.Parser;

namespace Lunarium.Logger.Writer;

internal sealed class LogJsonWriter : LogWriter
{
    private Utf8JsonWriter? _jsonWriter;

    private Utf8JsonWriter JsonWriter => _jsonWriter ??= new Utf8JsonWriter(_bufferWriter, new JsonWriterOptions
    {
        Indented = JsonSerializationConfig.Options.WriteIndented,
        Encoder = JsonSerializationConfig.Options.Encoder
    });

    protected override void ReturnToPool() => WriterPool.Return(this);

    #region 公共 API

    protected override LogWriter BeginEntry()
    {
        JsonWriter.Reset(_bufferWriter);
        JsonWriter.WriteStartObject();
        return this;
    }

    protected override LogWriter EndEntry()
    {
        JsonWriter.WriteEndObject();
        JsonWriter.Flush();
        return this;
    }

    protected override void BeforeRenderMessage(LogEntry logEntry)
    {
        WriteOriginalMessage(JsonWriter, logEntry.MessageTemplate.MessageTemplateTokens);
    }

    protected override void AfterRenderMessage(LogEntry logEntry)
    {
        WritePropertyValue(JsonWriter, logEntry.MessageTemplate.MessageTemplateTokens, logEntry.Properties);
    }

    #endregion

    #region Write Methods

    protected override LogWriter WriteTimestamp(DateTimeOffset timestamp)
    {
        JsonWriter.WritePropertyName("Timestamp");
        switch (TimestampFormatConfig.JsonMode)
        {
            case JsonTimestampMode.Unix:
                JsonWriter.WriteNumberValue(timestamp.ToUnixTimeSeconds());
                break;
            case JsonTimestampMode.UnixMs:
                JsonWriter.WriteNumberValue(timestamp.ToUnixTimeMilliseconds());
                break;
            case JsonTimestampMode.ISO8601:
                JsonWriter.WriteStringValue(timestamp.ToString("O"));
                break;
            case JsonTimestampMode.Custom:
                JsonWriter.WriteStringValue(timestamp.ToString(TimestampFormatConfig.JsonCustomFormat));
                break;
        }
        return this;
    }

    protected override LogWriter WriteLevel(LogLevel level)
    {
        JsonWriter.WriteString("Level", level.ToString());
        JsonWriter.WriteNumber("LogLevel", (int)level);
        return this;
    }

    protected override LogWriter WriteContext(string? context)
    {
        if (!string.IsNullOrEmpty(context))
        {
            JsonWriter.WriteString("Context", context);
        }
        return this;
    }

    private void WriteOriginalMessage(Utf8JsonWriter json, IReadOnlyList<MessageTemplateTokens> tokens)
    {
        json.WriteString("OriginalMessage", BuildOriginalMessage(tokens));
    }

    private static string BuildOriginalMessage(IReadOnlyList<MessageTemplateTokens> tokens)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    sb.Append(textToken.Text);
                    break;
                case PropertyToken propertyToken:
                    sb.Append(propertyToken.RawText.Text);
                    break;
            }
        }
        return sb.ToString();
    }

    protected override LogWriter WriteRenderedMessage(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        JsonWriter.WriteString("RenderedMessage", BuildRenderedMessage(tokens, propertys));
        return this;
    }

    private string BuildRenderedMessage(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        var sb = new System.Text.StringBuilder();
        int propIndex = 0;

        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    sb.Append(textToken.Text);
                    break;
                case PropertyToken propertyToken:
                    if (propIndex < propertys.Length)
                    {
                        sb.Append(FormatPropertyValue(propertyToken, propertys[propIndex]));
                        propIndex++;
                    }
                    else
                    {
                        sb.Append(propertyToken.RawText.Text);
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    private static string FormatPropertyValue(PropertyToken propertyToken, object? value)
    {
        if (value is null)
            return "null";

        try
        {
            return string.Format(CultureInfo.InvariantCulture, $"{{0:{propertyToken.Format}}}", value);
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, $"LogJsonWriter FormatPropertyValue Failed: {propertyToken.RawText.Text}");
            return propertyToken.RawText.Text;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JSON serialization uses JsonSerializationConfig.Options which can be configured with JsonTypeInfoResolver for AOT compatibility.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Same as IL2026.")]
    private void WritePropertyValue(Utf8JsonWriter json, IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        json.WriteStartObject("Propertys");

        if (propertys.Length != 0)
        {
            int i = 0;
            foreach (var token in tokens)
            {
                if (token is PropertyToken propertyToken)
                {
                    json.WritePropertyName(propertyToken.PropertyName);

                    if (i >= propertys.Length || propertys[i] is null)
                    {
                        json.WriteNullValue();
                    }
                    else if (propertyToken.Destructuring == Destructuring.Destructure ||
                             (DestructuringConfig.AutoDestructureCollections && IsCommonCollectionType(propertys[i])))
                    {
                        JsonSerializer.Serialize(json, propertys[i], JsonSerializationConfig.Options);
                    }
                    else
                    {
                        WriteJsonValue(json, propertys[i]);
                    }

                    i++;
                }
            }
        }

        json.WriteEndObject();
    }

    private static void WriteJsonValue(Utf8JsonWriter json, object? value)
    {
        switch (value)
        {
            case null:
                json.WriteNullValue();
                break;
            case int i:
                json.WriteNumberValue(i);
                break;
            case long l:
                json.WriteNumberValue(l);
                break;
            case short s:
                json.WriteNumberValue(s);
                break;
            case byte b:
                json.WriteNumberValue(b);
                break;
            case uint ui:
                json.WriteNumberValue(ui);
                break;
            case ulong ul:
                json.WriteNumberValue(ul);
                break;
            case ushort us:
                json.WriteNumberValue(us);
                break;
            case sbyte sb:
                json.WriteNumberValue(sb);
                break;
            case decimal dec:
                json.WriteNumberValue(dec);
                break;
            case double d:
                if (double.IsNaN(d))
                    json.WriteStringValue("NaN");
                else if (double.IsPositiveInfinity(d))
                    json.WriteStringValue("Infinity");
                else if (double.IsNegativeInfinity(d))
                    json.WriteStringValue("-Infinity");
                else
                    json.WriteNumberValue(d);
                break;
            case float f:
                if (float.IsNaN(f))
                    json.WriteStringValue("NaN");
                else if (float.IsPositiveInfinity(f))
                    json.WriteStringValue("Infinity");
                else if (float.IsNegativeInfinity(f))
                    json.WriteStringValue("-Infinity");
                else
                    json.WriteNumberValue(f);
                break;
            case bool b:
                json.WriteBooleanValue(b);
                break;
            case string s:
                json.WriteStringValue(s);
                break;
            case char c:
                json.WriteStringValue(c.ToString());
                break;
            case DateTime dt:
                json.WriteStringValue(dt.ToString("O"));
                break;
            case DateTimeOffset dto:
                json.WriteStringValue(dto.ToString("O"));
                break;
            case Guid g:
                json.WriteStringValue(g.ToString());
                break;
            case TimeSpan ts:
                json.WriteStringValue(ts.ToString());
                break;
            case Uri u:
                json.WriteStringValue(u.ToString());
                break;
            default:
                json.WriteStringValue(value.ToString());
                break;
        }
    }

    protected override LogWriter WriteException(Exception? exception)
    {
        if (exception != null)
        {
            JsonWriter.WriteString("Exception", exception.ToString());
        }
        return this;
    }

    #endregion
}
