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
using Lunarium.Logger.Parser;

namespace Lunarium.Logger.Writer;


// 格式: [颜色指令]文本[重置指令]
// $"{AnsiPrefix}{colorCode}m{text}{AnsiReset}";
// ANSI标准允许用分号组合多个代码
// $"{AnsiPrefix}{foregroundCode};{backgroundCode}m{text}{AnsiReset}";
internal sealed class LogColorTextWriter : LogWriter
{
    // =============== 编译时常量区 ===============
    // ANSI 指令的前缀
    private const string AnsiPrefix = "\x1b["; // \x1b 是 ESC 字符的十六进制表示
    // 重置所有颜色和样式的指令
    private const string AnsiReset = "\x1b[0m";
    private const string prefix = $"{AnsiPrefix}90m[{AnsiReset}";
    private const string suffix = $"{AnsiPrefix}90m]{AnsiReset}";
    // ===== 颜色定义 =====
    private const ConsoleColor TimestampColor = ConsoleColor.Green;
    private const ConsoleColor LevelDebugColor = ConsoleColor.DarkGray;
    private const ConsoleColor LevelInfoColor = ConsoleColor.Green;
    private const ConsoleColor LevelWarningColor = ConsoleColor.Yellow;
    private const ConsoleColor LevelErrorColor = ConsoleColor.Red;
    private const ConsoleColor LevelCriticalBgColor = ConsoleColor.DarkRed;
    private const ConsoleColor ContextColor = ConsoleColor.Cyan;
    private const ConsoleColor ExceptionColor = ConsoleColor.Red;

    // 值类型颜色
    private const ConsoleColor StringColor = ConsoleColor.Magenta;
    private const ConsoleColor NumberColor = ConsoleColor.Yellow;
    private const ConsoleColor BooleanColor = ConsoleColor.Blue;
    private const ConsoleColor NullColor = ConsoleColor.DarkBlue;
    private const ConsoleColor OtherColor = ConsoleColor.Gray; // 其他未知类型
    // =============== 编译时常量区 ===============

    // ============= 池化管理 API =============
    protected override void ReturnToPool() => WriterPool.Return(this);

    #region 公共 API
    // =============== 公共 API ===============
    protected override LogColorTextWriter WriteTimestamp(DateTimeOffset timestamp)
    {
        _bufferWriter.Append(prefix);
        SetColor(TimestampColor);
        switch (TimestampFormatConfig.TextMode)
        {
            case TextTimestampMode.Unix:
                _bufferWriter.AppendFormattable(timestamp.ToUnixTimeSeconds());
                break;
            case TextTimestampMode.UnixMs:
                _bufferWriter.AppendFormattable(timestamp.ToUnixTimeMilliseconds());
                break;
            case TextTimestampMode.ISO8601:
                _bufferWriter.AppendFormattable(timestamp, "O");
                break;
            case TextTimestampMode.Custom:
                _bufferWriter.AppendFormattable(timestamp, TimestampFormatConfig.TextCustomFormat.AsSpan());
                break;
        }
        _bufferWriter.Append(suffix);
        _bufferWriter.Append(' ');
        return this;
    }

    protected override LogColorTextWriter WriteLevel(LogLevel level)
    {
        var levelStr = level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "UNK"
        };
        Action levelColor = level switch
        {
            LogLevel.Debug => () => SetColor(LevelDebugColor),
            LogLevel.Info => () => SetColor(LevelInfoColor),
            LogLevel.Warning => () => SetColor(LevelWarningColor),
            LogLevel.Error => () => SetColor(LevelErrorColor),
            LogLevel.Critical => () => SetColor(ConsoleColor.White, LevelCriticalBgColor),
            _ => () => SetColor(ConsoleColor.White, ConsoleColor.Yellow)
        };
        _bufferWriter.Append(prefix);
        levelColor.Invoke();
        _bufferWriter.Append(levelStr);
        _bufferWriter.Append(AnsiReset);
        _bufferWriter.Append(suffix);
        _bufferWriter.Append(' ');
        return this;
    }

    protected override LogColorTextWriter WriteContext(string? context)
    {
        if (!string.IsNullOrEmpty(context))
        {
            _bufferWriter.Append(prefix);
            SetColor(ContextColor);
            _bufferWriter.Append(context);
            _bufferWriter.Append(suffix);
            _bufferWriter.Append(' ');
        }
        return this;
    }

    protected override LogColorTextWriter WriteRenderedMessage(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        int i = 0;
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    _bufferWriter.Append(textToken.Text);
                    break;
                case PropertyToken propertyToken:
                    if (propertys.Length == 0)
                    {
                        _bufferWriter.Append(propertyToken.RawText.Text);
                    }
                    else
                    {
                        RenderPropertyToken(propertyToken, propertys, i);
                        i++;
                    }
                    break;
            }
        }
        return this;
    }

    protected override LogColorTextWriter WriteException(Exception? exception)
    {
        if (exception != null)
        {
            _bufferWriter.AppendLine();
            SetColor(ConsoleColor.Red);
            _bufferWriter.Append(exception);
            _bufferWriter.Append(AnsiReset);
        }
        return this;
    }

    // ========================================
    #endregion



    #region 辅助方法
    // ================ 辅助方法 ================

    private void RenderPropertyToken(PropertyToken propertyToken, object?[] propertys, int i)
    {
        try
        {
            // 获取要渲染的值
            object? value = GetPropertyValue(propertys, i, out bool found);

            if (!found)
            {
                // 如果找不到对应的参数，输出原始文本
                _bufferWriter.Append(propertyToken.RawText.Text);
                return;
            }

            if (value is null)
            {
                SetValueColor(value);
                _bufferWriter.Append("null");
                _bufferWriter.Append(AnsiReset);
                return;
            }

            // 当具有解构标识或设置了默认解构(且是集合类型)时, 尝试解构对象且跳过对齐和格式化(对json格式无意义)
            if (propertyToken.Destructuring == Destructuring.Destructure || (DestructuringConfig.AutoDestructureCollections && IsCommonCollectionType(value)))
            {
                SetValueColor(value);
                // 当遇到 {@...} 时，使用 JsonSerializer 把它变成一个易读的 JSON 字符串
                _bufferWriter.Append(TrySerializeToJson(value) ?? value.ToString() ?? "null");
                _bufferWriter.Append(AnsiReset);
                return; // 处理完就返回, 跳过构建格式字符串
            }

            // 构建格式字符串，支持对齐和格式化
            string formatString = BuildFormatString(propertyToken.Alignment, propertyToken.Format);
            SetValueColor(value);
            _bufferWriter.AppendFormat(formatString, value);
            _bufferWriter.Append(AnsiReset);
        }
        catch (Exception ex)
        {
            _bufferWriter.Append(propertyToken.RawText.Text);
            InternalLogger.Error(ex, $"LogWriter WriteValue Failed: {propertyToken.RawText.Text}");
        }
    }

    // 获取属性值
    private static object? GetPropertyValue(object?[] propertys, int namedIndex, out bool found)
    {
        if (namedIndex >= propertys.Length || namedIndex < 0)
        {
            found = false;
            return null;
        }
        found = true;
        return propertys[namedIndex];
    }

    // 构建格式字符串: {index,alignment:format}
    private static string BuildFormatString(int? alignment, string? format)
    {
        // 如果超长则回退到堆分配
        // yyyy-MM-dd HH:mm:ss.fff zzz 在格式化中较长的时间格式化, 96个字符也走过容纳前面的例子x3了
        if (format is not null && format.Length > 96) return BuildFormatStringByHeap(alignment, format);
        Span<char> buffer = stackalloc char[128];
        int pos = 0;

        buffer[pos++] = '{';
        buffer[pos++] = '0';

        if (alignment.HasValue)
        {
            buffer[pos++] = ',';
            alignment.Value.TryFormat(buffer[pos..], out int written);
            pos += written;
        }

        if (!string.IsNullOrEmpty(format))
        {
            buffer[pos++] = ':';
            format.AsSpan().CopyTo(buffer[pos..]);
            pos += format.Length;
        }

        buffer[pos++] = '}';
        return new string(buffer[..pos]);
    }

    // 构建格式字符串: {index,alignment:format}
    private static string BuildFormatStringByHeap(int? alignment, string? format)
    {
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

    /// <summary>
    /// (扩展方法) 使用指定的颜色包裹字符串，为其添加 ANSI 颜色代码。
    /// </summary>
    /// <param name="text">要添加颜色的原始字符串。</param>
    /// <param name="color">要应用的 ConsoleColor 枚举值。</param>
    /// <returns>一个带有 ANSI 颜色代码的新字符串。在兼容的终端中会显示为彩色。</returns>
    private void SetColor(ConsoleColor color)
    {
        // 格式: [颜色指令]文本[重置指令]
        _bufferWriter.Append(AnsiPrefix);
        _bufferWriter.Append(ForegroundCode(color));
        _bufferWriter.Append('m');
    }

    private void SetColor(ConsoleColor foreground, ConsoleColor background)
    {
        // ANSI标准允许用分号组合多个代码
        _bufferWriter.Append(AnsiPrefix);
        _bufferWriter.Append(ForegroundCode(foreground));
        _bufferWriter.Append(';');
        _bufferWriter.Append(BackgroundCode(background));
        _bufferWriter.Append('m');
    }

    /// <summary>
    /// 将 ConsoleColor 枚举值映射到对应的 ANSI 前景色代码。
    /// </summary>
    /// <param name="color">要转换的 ConsoleColor。</param>
    /// <returns>表示 ANSI 颜色代码的字符串。</returns>
    private static string ForegroundCode(ConsoleColor color)
    {
        return color switch
        {
            // 这是标准的前景色代码
            ConsoleColor.Black => "30",
            ConsoleColor.DarkRed => "31",
            ConsoleColor.DarkGreen => "32",
            ConsoleColor.DarkYellow => "33",
            ConsoleColor.DarkBlue => "34",
            ConsoleColor.DarkMagenta => "35",
            ConsoleColor.DarkCyan => "36",
            ConsoleColor.Gray => "37",
            ConsoleColor.DarkGray => "90",
            ConsoleColor.Red => "91",
            ConsoleColor.Green => "92",
            ConsoleColor.Yellow => "93",
            ConsoleColor.Blue => "94",
            ConsoleColor.Magenta => "95",
            ConsoleColor.Cyan => "96",
            ConsoleColor.White => "97",
            _ => "37" // 默认灰色
        };
    }
    // 背景色映射方法 (背景色代码通常是前景色+10)
    private static string BackgroundCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "40",
            ConsoleColor.DarkRed => "41",
            ConsoleColor.DarkGreen => "42",
            ConsoleColor.DarkYellow => "43",
            ConsoleColor.DarkBlue => "44",
            ConsoleColor.DarkMagenta => "45",
            ConsoleColor.DarkCyan => "46",
            ConsoleColor.Gray => "47",
            ConsoleColor.DarkGray => "100",
            ConsoleColor.Red => "101",
            ConsoleColor.Green => "102",
            ConsoleColor.Yellow => "103",
            ConsoleColor.Blue => "104",
            ConsoleColor.Magenta => "105",
            ConsoleColor.Cyan => "106",
            ConsoleColor.White => "107",
            _ => "40" // 默认黑色背景
        };
    }


    private void SetValueColor(object? value)
    {
        if (value == null) SetColor(NullColor);
        else switch (value)
            {
                // ===== 数值类型 (Numbers) =====
                case int:
                case long:
                case short:
                case byte:
                case uint:
                case ulong:
                case ushort:
                case sbyte:
                case decimal:
                case double:
                case float:
                    SetColor(NumberColor);
                    break;

                // ===== 布尔类型 (Boolean) =====
                case bool:
                    SetColor(BooleanColor);
                    break;

                // ===== 其他常见类型，必须字符串化 =====
                case string:
                case char:
                case DateTime:
                case DateTimeOffset:
                case Guid:
                case TimeSpan:
                case Uri:
                    SetColor(StringColor);
                    break;

                // ===== 默认回退 (Default Fallback) =====
                default:
                    SetColor(OtherColor);
                    break;
            }
    }
    #endregion
}
