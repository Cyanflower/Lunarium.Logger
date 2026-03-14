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

namespace Lunarium.Logger.Parser;

public enum Destructuring
{
    Default,      // {Name}
    Destructure,  // {@Object}
    Stringify     // {$Object}
}

public record MessageTemplate
{
    internal readonly IReadOnlyList<MessageTemplateTokens> MessageTemplateTokens;

    internal readonly ReadOnlyMemory<byte> OriginalMessageBytes;


    internal MessageTemplate(MessageTemplateTokens[] messageTemplateTokens)
    {
        MessageTemplateTokens = messageTemplateTokens;
        OriginalMessageBytes = BuildOriginalMessage(messageTemplateTokens);
    }

    private static ReadOnlyMemory<byte> BuildOriginalMessage(IReadOnlyList<MessageTemplateTokens> tokens)
    {
        int count = 0;
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    count += textToken.TextBytes.Length;
                    break;
                case PropertyToken propertyToken:
                    count += propertyToken.RawText.TextBytes.Length;
                    break;
            }
        }
        byte[] bytes = new byte[count];
        int index = 0;
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    textToken.TextBytes.CopyTo(bytes[index..]);
                    index += textToken.TextBytes.Length;
                    break;
                case PropertyToken propertyToken:
                    propertyToken.RawText.TextBytes.CopyTo(bytes[index..]);
                    index += propertyToken.RawText.TextBytes.Length;
                    break;
            }
        }
        return bytes;
    }
}

// Token基类
public abstract record MessageTemplateTokens;

public record TextToken : MessageTemplateTokens
{
    public string Text { get; }
    public ReadOnlyMemory<byte> TextBytes { get; }
    internal TextToken(string text)
    {
        Text = text;
        TextBytes = Encoding.UTF8.GetBytes(text);
    }
}

public record PropertyToken : MessageTemplateTokens
{
    public string PropertyName { get; }
    public TextToken RawText { get; }
    public string? Format { get; }
    public int? Alignment { get; }
    public string FormatString { get; }
    public Destructuring Destructuring { get; }
    
    // byte area
    public ReadOnlyMemory<byte> PropertyNameBytes { get; }

    internal PropertyToken(
        string propertyName,
        TextToken rawText,
        string? format = null,
        int? alignment = null,
        Destructuring destructuring = Destructuring.Default
    )
    {
        PropertyName = propertyName;
        RawText = rawText;
        Format = format;
        Alignment = alignment;
        Destructuring = destructuring;
        FormatString = BuildFormatString(alignment, format);

        PropertyNameBytes = Encoding.UTF8.GetBytes(propertyName);
    }

    private static string BuildFormatString(int? alignment, string? format)
    {
        if (!alignment.HasValue && string.IsNullOrEmpty(format)) return "{0}";

        var sb = new StringBuilder("{0");

        // 添加对齐: {0,10} 或 {0,-10}
        if (alignment.HasValue)
        {
            sb.Append(',');
            sb.Append(alignment.Value);
        }

        // 添加格式: {0:D} 或 {0,10:D}
        if (!string.IsNullOrEmpty(format))
        {
            sb.Append(':');
            sb.Append(format);
        }

        sb.Append('}');
        return sb.ToString();
    }
}