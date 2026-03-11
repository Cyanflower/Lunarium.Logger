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

using Microsoft.Extensions.Logging;
using Lunarium.Logger;
using Lunarium.Logger.Models;

namespace Lunarium.Logger.Extensions;

/// <summary>
/// 将 Microsoft.Extensions.Logging 桥接到 LunariumLogger 的适配器
/// </summary>
public class LunariumLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly Lunarium.Logger.ILogger _lunariumLogger;
    private IExternalScopeProvider _scopeProvider = NullScopeProvider.Instance;

    public LunariumLoggerProvider(Lunarium.Logger.ILogger lunariumLogger)
    {
        _lunariumLogger = lunariumLogger;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new LunariumMsLoggerAdapter(_lunariumLogger.ForContext(categoryName), _scopeProvider);
    }

    public void Dispose()
    {
        // -------------------------------------------------------
        // LunariumLogger 的生命周期由外部管理
        // 由于谁创建, 谁管理生命周期原则, 这里的 Dispose 不做任何操作
        // -------------------------------------------------------
        // 只有当DI管理的是根Logger时，才允许调用 DisposeAsync
        // if (_lunariumLogger is IAsyncDisposable ad) ad.DisposeAsync().GetAwaiter().GetResult();
    }
}

/// <summary>
/// 将 Microsoft.Extensions.Logging.ILogger 适配到 LunariumLogger 的适配器
/// </summary>
internal sealed class LunariumMsLoggerAdapter : Microsoft.Extensions.Logging.ILogger
{
    private readonly Lunarium.Logger.ILogger _lunariumLogger;
    private readonly IExternalScopeProvider _scopeProvider;

    internal LunariumMsLoggerAdapter(Lunarium.Logger.ILogger lunariumLogger, IExternalScopeProvider scopeProvider)
    {
        _lunariumLogger = lunariumLogger;
        _scopeProvider = scopeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider.Push(state);

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        // 实际过滤在 Sink 层执行，此处保守地返回 true
        return true;
    }

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var level = ConvertLogLevel(logLevel);

        // 收集当前异步流中的 scope 链
        var scopeParts = new List<string>();
        _scopeProvider.ForEachScope((scope, list) =>
        {
            var str = scope?.ToString();
            if (!string.IsNullOrEmpty(str)) list.Add(str);
        }, scopeParts);

        // eventId 有意义时追加到末尾
        if (!string.IsNullOrEmpty(eventId.Name))
            scopeParts.Add(eventId.Name);
        else if (eventId.Id != 0)
            scopeParts.Add(eventId.Id.ToString());

        var context = string.Join(".", scopeParts);

        _lunariumLogger.Log(level, message, context, exception);
    }

    private static Lunarium.Logger.LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel msLogLevel)
    {
        return msLogLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => Lunarium.Logger.LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Debug => Lunarium.Logger.LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => Lunarium.Logger.LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => Lunarium.Logger.LogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => Lunarium.Logger.LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => Lunarium.Logger.LogLevel.Critical,
            _ => Lunarium.Logger.LogLevel.Info
        };

    }
}

/// <summary>
/// 扩展方法，方便注册自定义日志提供程序
/// </summary>
public static class LunariumLoggerExtensions
{
    public static ILoggingBuilder AddLunariumLogger(
        this ILoggingBuilder builder,
        Lunarium.Logger.ILogger lunariumLogger)
    {
        builder.ClearProviders(); // 清除所有默认提供程序
        builder.AddProvider(new LunariumLoggerProvider(lunariumLogger));
        return builder;
    }
}

public static class LunariumLoggerConversionExtensions
{
    /// <summary>
    /// 将 LunariumLogger 转换为 Microsoft.Extensions.Logging.ILogger
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger ToMicrosoftLogger(
        this Lunarium.Logger.ILogger logger,
        string categoryName)
    {
        return new LunariumMsLoggerAdapter(logger.ForContext(categoryName), NullScopeProvider.Instance);
    }
}

/// <summary>
/// 无操作的 IExternalScopeProvider，用于不经过 DI 注册的场景。
/// </summary>
internal sealed class NullScopeProvider : IExternalScopeProvider
{
    public static NullScopeProvider Instance { get; } = new();

    public void ForEachScope<TState>(Action<object?, TState> callback, TState state) { }

    public IDisposable Push(object? state) => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}