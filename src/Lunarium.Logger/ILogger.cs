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

using System.Runtime.CompilerServices;
using Lunarium.Logger.Models;

namespace Lunarium.Logger;

/// <summary>
/// 定义了日志记录器的核心功能接口，提供了不同级别的日志记录方法。
/// </summary>
public interface ILogger : IAsyncDisposable
{

    string GetContext();

    ReadOnlyMemory<byte> GetContextSpan();

    /// <summary>
    /// 记录一条具有指定级别和详细信息的日志消息。
    /// 这是所有日志记录方法的基础实现。
    /// </summary>
    /// <param name="level">日志的严重级别。</param>
    /// <param name="message">要记录的主要日志消息，可以包含 {Property} 格式的占位符。</param>
    /// <param name="context">日志的上下文信息，用于区分日志来源（如模块、类名等）。</param>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    /// <param name="scope">Scope信息，由 MS 适配器自动填充，无需手动提供。</param>
    /// <param name="propertyValues">用于替换消息模板中占位符的属性值。</param>
    void Log(LogLevel level, Exception? ex = null, string message = "", string context = "", ReadOnlyMemory<byte> contextBytes = default, string scope = "", params object?[] propertyValues);

    // ========================================================================
    // Debug 方法重载
    // ========================================================================

    /// <summary>
    /// 记录一条 Debug 级别的日志消息，通常用于详细的调试信息。
    /// </summary>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Debug(string message, params object?[] propertyValues)
        => Log(level: LogLevel.Debug, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Debug 级别的日志消息，通常用于详细的调试信息。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Debug(Exception? ex, string message, params object?[] propertyValues)
        => Log(level: LogLevel.Debug, ex: ex, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Debug 级别的日志消息，通常用于详细的调试信息。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    void Debug(Exception? ex)
        => Log(level: LogLevel.Debug, ex: ex);

    // ========================================================================
    // Info 方法重载
    // ========================================================================

    /// <summary>
    /// 记录一条 Info 级别的日志消息，用于常规的操作信息。
    /// </summary>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Info(string message, params object?[] propertyValues)
        => Log(level: LogLevel.Info, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Info 级别的日志消息，用于常规的操作信息。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Info(Exception? ex, string message, params object?[] propertyValues)
        => Log(level: LogLevel.Info, ex: ex, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Info 级别的日志消息，用于常规的操作信息。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    void Info(Exception? ex)
        => Log(level: LogLevel.Info, ex: ex);

    // ========================================================================
    // Warning 方法重载
    // ========================================================================

    /// <summary>
    /// 记录一条 Warning 级别的日志消息，用于指示潜在问题。
    /// </summary>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Warning(string message, params object?[] propertyValues)
        => Log(level: LogLevel.Warning, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Warning 级别的日志消息，用于指示潜在问题。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Warning(Exception? ex, string message, params object?[] propertyValues)
        => Log(level: LogLevel.Warning, ex: ex, message: message, propertyValues: propertyValues);

    /// <summary>
    /// 记录一条 Warning 级别的日志消息，用于指示潜在问题。
    /// </summary>
    /// <param name="ex">与日志条目关联的异常（如果有）。</param>
    /// <param name="message">要记录的日志消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Warning(Exception? ex)
        => Log(level: LogLevel.Warning, ex: ex);

    // ========================================================================
    // Error 方法重载
    // ========================================================================

    /// <summary>
    /// 记录一条 Error 级别的日志消息，用于描述一个可恢复的错误。
    /// </summary>
    /// <param name="message">要记录的错误消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Error(string message, params object?[] propertyValues)
        => Log(level: LogLevel.Error, message: message, ex: null, propertyValues: propertyValues);

    /// <summary>
    /// 记录一个 Error 级别的异常信息。
    /// </summary>
    /// <param name="ex">要记录的异常对象。</param>
    void Error(Exception? ex)
        => Log(level: LogLevel.Error, message: "", ex: ex, propertyValues: []);

    /// <summary>
    /// 记录一条 Error 级别的日志消息，并附带一个异常的详细信息。
    /// </summary>
    /// <param name="message">描述错误的上下文消息。</param>
    /// <param name="ex">与错误关联的异常对象。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Error(Exception? ex, string message, params object?[] propertyValues)
        => Log(level: LogLevel.Error, ex: ex, message: message, propertyValues: propertyValues);

    // ========================================================================
    // Critical 方法重载
    // ========================================================================

    /// <summary>
    /// 记录一条 Critical 级别的日志消息，用于描述可能导致应用程序终止的严重错误。
    /// </summary>
    /// <param name="message">要记录的严重错误消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Critical(string message, params object?[] propertyValues)
        => Log(level: LogLevel.Critical, message: message, ex: null, propertyValues: propertyValues);

    /// <summary>
    /// 记录一个 Critical 级别的异常信息。
    /// </summary>
    /// <param name="ex">要记录的严重异常对象。</param>
    void Critical(Exception? ex)
        => Log(level: LogLevel.Critical, ex: ex, message: "", propertyValues: []);

    /// <summary>
    /// 记录一个 Critical 级别的异常信息，并附带一条描述性的消息。
    /// </summary>
    /// <param name="ex">要记录的严重异常对象。</param>
    /// <param name="message">描述严重错误的上下文消息。</param>
    /// <param name="propertyValues">用于消息模板的附加属性值。</param>
    void Critical(Exception? ex, string message, params object?[] propertyValues)
        => Log(level: LogLevel.Critical, ex: ex, message: message, propertyValues: propertyValues);

}