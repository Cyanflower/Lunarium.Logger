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

using Lunarium.Logger.Parser;

namespace Lunarium.Logger.Writer;

internal sealed class LogJsonWriter : LogWriter
{
    // ============= 池化管理 API =============
    protected override void ReturnToPool() => WriterPool.Return(this);

    #region 公共 API
    // =============== 公共 API ===============
    // 重写 BeginEntry 输出 JSON 开始符号
    protected override LogWriter BeginEntry()
    {
        _bufferWriter.Append('{');
        return this;
    }

    // 钩子：在渲染消息前写入 MessageTemplate
    protected override void BeforeRenderMessage(LogEntry logEntry)
    {
        WriteOriginalMessage(logEntry.MessageTemplate.MessageTemplateTokens);
    }

    // 钩子：在渲染消息后写入 PropertyValue
    protected override void AfterRenderMessage(LogEntry logEntry)
    {
        WritePropertyValue(logEntry.MessageTemplate.MessageTemplateTokens, logEntry.Properties);
    }

    // 重写 EndEntry 输出 JSON 结束符号
    protected override LogWriter EndEntry()
    {
        _bufferWriter.Append('}');
        return this;
    }

    protected override LogJsonWriter WriteTimestamp(DateTimeOffset timestamp)
    {
        switch (TimestampFormatConfig.JsonMode)
        {
            case JsonTimestampMode.Unix:
                _bufferWriter.Append("\"Timestamp\":");
                _bufferWriter.AppendFormattable(timestamp.ToUnixTimeSeconds());
                _bufferWriter.Append(',');
                break;
            case JsonTimestampMode.UnixMs:
                _bufferWriter.Append("\"Timestamp\":");
                _bufferWriter.AppendFormattable(timestamp.ToUnixTimeMilliseconds());
                _bufferWriter.Append(',');
                break;
            case JsonTimestampMode.ISO8601:
                _bufferWriter.Append("\"Timestamp\":");
                AppendJsonString(timestamp.ToString("O"));
                _bufferWriter.Append(',');
                break;
            case JsonTimestampMode.Custom:
                _bufferWriter.Append("\"Timestamp\":");
                AppendJsonString(timestamp.ToString(TimestampFormatConfig.JsonCustomFormat));
                _bufferWriter.Append(',');
                break;
        }
        return this;
    }

    protected override LogJsonWriter WriteLevel(LogLevel level)
    {
        var levelStr = level switch
        {
            LogLevel.Debug => "Debug",
            LogLevel.Info => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => "Unknown"
        };
        _bufferWriter.Append("\"Level\":\"");
        _bufferWriter.Append(levelStr);
        _bufferWriter.Append("\",\"LogLevel\":");
        _bufferWriter.AppendFormattable((int)level);
        _bufferWriter.Append(',');
        return this;
    }

    protected override LogJsonWriter WriteContext(string? context)
    {
        if (!string.IsNullOrEmpty(context))
        {
            _bufferWriter.Append("\"Context\":");
            AppendJsonString(context);
            _bufferWriter.Append(',');
        }
        return this;
    }

    private LogJsonWriter WriteOriginalMessage(IReadOnlyList<MessageTemplateTokens> tokens)
    {
        _bufferWriter.Append("\"OriginalMessage\":\"");
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    AppendJsonStringContent(textToken.Text);
                    break;
                case PropertyToken propertyToken:
                    AppendJsonStringContent(propertyToken.RawText.Text);
                    break;
            }
        }
        _bufferWriter.Append("\",");

        return this;
    }

    protected override LogJsonWriter WriteRenderedMessage(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        _bufferWriter.Append("\"RenderedMessage\":\"");
        int i = 0;
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken textToken:
                    AppendJsonStringContent(textToken.Text);
                    break;
                case PropertyToken propertyToken:
                    if (propertys.Length == 0)
                    {
                        AppendJsonStringContent(propertyToken.RawText.Text);
                    }
                    else
                    {
                        RenderPropertyToken(propertyToken, propertys, i);
                        i++;
                    }
                    break;
            }
        }
        _bufferWriter.Append("\",");
        return this;
    }

    private LogJsonWriter WritePropertyValue(IReadOnlyList<MessageTemplateTokens> tokens, object?[] propertys)
    {
        _bufferWriter.Append("\"Propertys\":{");
        if (propertys.Length != 0)
        {
            int iteration = 0;
            foreach (var token in tokens)
            {
                if (token is PropertyToken propertyToken)
                {
                    _bufferWriter.Append('"');
                    AppendJsonStringContent(propertyToken.PropertyName);
                    _bufferWriter.Append("\":");

                    if (iteration >= propertys.Length)
                    {
                        _bufferWriter.Append("null,");
                        continue;
                    }
                    object? value = propertys[iteration];

                    // 处理解构（Destructuring）
                    if (propertyToken.Destructuring == Destructuring.Destructure || (DestructuringConfig.AutoDestructureCollections && IsCommonCollectionType(value)))
                    {
                        var valueJson = TrySerializeToJson(value);
                        if (valueJson != null)
                            _bufferWriter.Append(valueJson);
                        else
                            ToJsonValue(value);
                    }
                    else
                    {
                        ToJsonValue(value);
                    }

                    _bufferWriter.Append(',');
                    iteration++;
                }
            }

            // 移除末尾多余的逗号
            if (_bufferWriter.Length > 0 && _bufferWriter[_bufferWriter.Length - 1] == ',')
            {
                _bufferWriter.RemoveLast();
            }
        }

        _bufferWriter.Append("},");
        return this;
    }

    protected override LogJsonWriter WriteException(Exception? exception)
    {
        if (exception != null)
        {
            _bufferWriter.Append("\"Exception\":");
            AppendJsonString(exception.ToString());
        }
        else
        {
            // 移除末尾多余的逗号
            if (_bufferWriter.Length > 0 && _bufferWriter[_bufferWriter.Length - 1] == ',')
            {
                _bufferWriter.RemoveLast();
            }
        }
        return this;
    }

    // ========================================
    #endregion

    #region 辅助方法
    // ================ 辅助方法 ================

    /// <summary>
    /// 将字符串转义并添加 JSON 引号输出到缓冲区
    /// 例如：Hello "World" → "Hello \"World\""
    /// </summary>
    /// <param name="value">要转义的字符串</param>
    private void AppendJsonString(string value)
    {
        _bufferWriter.Append('"');
        AppendJsonStringContent(value);
        _bufferWriter.Append('"');
    }

    /// <summary>
    /// 将字符串内容转义后输出（不添加引号）
    /// 处理 JSON 必须转义的字符：" \ 换行 回车 制表符等
    /// 同时处理代理对（Surrogate Pairs）以支持 Emoji 等特殊字符
    /// 中文等 Unicode 字符直接输出，不转义（符合 JSON 标准且减小文件体积）
    /// </summary>
    /// <param name="value">要转义的字符串内容</param>
    private void AppendJsonStringContent(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            // 检查是否是高代理（Emoji 等的第一部分）
            // 代理对（Surrogate Pairs）是 UTF-16 编码中用于表示 Unicode 补充字符的机制
            // 例如 Emoji "😀" 由两个 char 组成：\uD83D\uDE00
            if (char.IsHighSurrogate(c) && i + 1 < value.Length)
            {
                char low = value[i + 1];
                if (char.IsLowSurrogate(low))
                {
                    // 保持代理对完整，直接输出（JSON 标准支持）
                    _bufferWriter.Append(c);
                    _bufferWriter.Append(low);
                    i++; // 跳过低代理
                    continue;
                }
            }

            // 处理必须转义的字符
            switch (c)
            {
                case '"':  // 双引号 - 必须转义，否则会破坏 JSON 结构
                    _bufferWriter.Append("\\\"");
                    break;
                case '\\': // 反斜杠 - 必须转义，否则会与转义序列冲突
                    _bufferWriter.Append("\\\\");
                    break;
                case '\n': // 换行符 - 必须转义，JSON 字符串不能包含实际换行
                    _bufferWriter.Append("\\n");
                    break;
                case '\r': // 回车符 - 必须转义
                    _bufferWriter.Append("\\r");
                    break;
                case '\t': // 制表符 - 必须转义
                    _bufferWriter.Append("\\t");
                    break;
                case '\b': // 退格符 - 必须转义
                    _bufferWriter.Append("\\b");
                    break;
                case '\f': // 换页符 - 必须转义
                    _bufferWriter.Append("\\f");
                    break;
                default:
                    // 控制字符（0x00-0x1F）必须转义为 \uXXXX 格式
                    // 这些字符在 JSON 字符串中不可见且可能导致解析问题
                    if (c < 0x20)
                    {
                        _bufferWriter.Append("\\u");
                        _bufferWriter.AppendFormattable((int)c, "x4");
                    }
                    else
                    {
                        // 中文、日文、韩文等 Unicode 字符直接输出
                        // JSON 标准（RFC 8259）完全支持 UTF-8 编码的 Unicode 字符
                        // 不转义的优点：
                        // 1. 文件体积更小（中文转义后会变成 \uXXXX，体积增大约 3-6 倍）
                        // 2. 性能更好（无需转义处理）
                        // 3. 直接可读（便于调试和查看日志文件）
                        // 4. 所有主流日志分析工具都完美支持 UTF-8
                        _bufferWriter.Append(c);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 在 RenderedMessage 中渲染属性值（应用格式化）
    /// 此方法用于消息模板渲染，会应用用户指定的格式字符串
    /// </summary>
    private void RenderPropertyToken(PropertyToken propertyToken, object?[] propertys, int i)
    {
        try
        {
            if (i >= propertys.Length)
            {
                AppendJsonStringContent(propertyToken.RawText.Text);
                return;
            }

            if (propertys[i] is null)
            {
                _bufferWriter.Append("null");
                return;
            }

            // 应用格式化，然后转义输出
            var formatted = string.Format($"{{0:{propertyToken.Format}}}", propertys[i]);
            AppendJsonStringContent(formatted);
        }
        catch (Exception ex)
        {
            AppendJsonStringContent(propertyToken.RawText.Text);
            InternalLogger.Error(ex, $"LogWriter RenderPropertyToken Failed: {propertyToken.RawText.Text}");
        }
    }

    /// <summary>
    /// 将 .NET 对象转换为 JSON 值格式
    /// 数值和布尔类型直接输出（不带引号）
    /// 字符串类型需要转义并添加引号
    /// </summary>
    private void ToJsonValue(object? value)
    {
        if (value == null)
        {
            _bufferWriter.Append("null");
            return;
        }

        switch (value)
        {
            // ===== 数值类型 (Numbers) =====
            // 数值类型在 JSON 中不需要引号，直接格式化为 UTF-8，不经过 string 分配
            case int i:
                _bufferWriter.AppendFormattable(i);
                break;
            case long l:
                _bufferWriter.AppendFormattable(l);
                break;
            case short s:
                _bufferWriter.AppendFormattable((int)s);
                break;
            case byte b:
                _bufferWriter.AppendFormattable((uint)b);
                break;
            case uint ui:
                _bufferWriter.AppendFormattable(ui);
                break;
            case ulong ul:
                _bufferWriter.AppendFormattable(ul);
                break;
            case ushort us:
                _bufferWriter.AppendFormattable((uint)us);
                break;
            case sbyte sb:
                _bufferWriter.AppendFormattable((int)sb);
                break;
            case decimal dec:
                _bufferWriter.AppendFormattable(dec);
                break;

            // 浮点数需要特殊处理 NaN 和 Infinity
            // JSON 标准不支持 NaN 和 Infinity，需要转为字符串
            case double d:
                if (double.IsNaN(d))
                    _bufferWriter.Append("\"NaN\"");
                else if (double.IsPositiveInfinity(d))
                    _bufferWriter.Append("\"Infinity\"");
                else if (double.IsNegativeInfinity(d))
                    _bufferWriter.Append("\"-Infinity\"");
                else
                    // "R" 格式确保往返转换精度（Round-trip）
                    _bufferWriter.AppendFormattable(d, "R");
                break;
            case float f:
                if (float.IsNaN(f))
                    _bufferWriter.Append("\"NaN\"");
                else if (float.IsPositiveInfinity(f))
                    _bufferWriter.Append("\"Infinity\"");
                else if (float.IsNegativeInfinity(f))
                    _bufferWriter.Append("\"-Infinity\"");
                else
                    _bufferWriter.AppendFormattable(f, "R");
                break;

            // ===== 布尔类型 (Boolean) =====
            // JSON 布尔值是小写的 true/false（不带引号）
            case bool b:
                _bufferWriter.Append(b ? "true" : "false");
                break;

            // ===== 字符串类型 =====
            // 所有字符串类型都需要转义并添加引号
            case string s:
                AppendJsonString(s);
                break;
            case char c:
                AppendJsonString(c.ToString());
                break;

            // ===== 日期时间类型 =====
            // 使用 ISO 8601 格式（"O"）以确保跨平台兼容性和精度
            case DateTime dt:
                _bufferWriter.Append('"');
                _bufferWriter.AppendFormattable(dt, "O");
                _bufferWriter.Append('"');
                break;
            case DateTimeOffset dto:
                _bufferWriter.Append('"');
                _bufferWriter.AppendFormattable(dto, "O");
                _bufferWriter.Append('"');
                break;

            // ===== 其他常见类型 =====
            case Guid g:
                _bufferWriter.Append('"');
                _bufferWriter.AppendFormattable(g);
                _bufferWriter.Append('"');
                break;
            case TimeSpan ts:
                AppendJsonString(ts.ToString());
                break;
            case Uri u:
                AppendJsonString(u.ToString());
                break;

            // ===== 默认回退 (Default Fallback) =====
            // 对于未知类型，调用 ToString() 并作为字符串处理
            default:
                AppendJsonString(value.ToString() ?? "null");
                break;
        }
    }
    #endregion
}
