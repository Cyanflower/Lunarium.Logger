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
    internal MessageTemplate(MessageTemplateTokens[] messageTemplateTokens)
    {
        MessageTemplateTokens = messageTemplateTokens;
    }
}

// Token基类
public abstract record MessageTemplateTokens;

public record TextToken : MessageTemplateTokens
{
    public string Text { get; }
    internal TextToken(string text)
    {
        Text = text;
    }
}

public record PropertyToken : MessageTemplateTokens
{
    public string PropertyName { get; }
    public TextToken RawText { get; }
    public string? Format { get; }
    public int? Alignment { get; }
    public Destructuring Destructuring { get; }

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
    }
}