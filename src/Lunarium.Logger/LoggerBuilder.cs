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

using Lunarium.Logger.Target;

namespace Lunarium.Logger;

/// <summary>
/// 一个用于配置和构建 LunariumLogger 实例的构建器。
/// 使用流式接口（fluent interface）来链式调用配置方法。
/// </summary>
public sealed class LoggerBuilder
{
    // 持有所有已配置的日志输出目标
    private List<Sink> _sinks = new();
    // 日志记录器的名称
    private string _loggerName = "LoggerName:Undefined";

    /// <summary>
    /// 初始化一个新的 LoggerBuilder 实例。
    /// </summary>
    public LoggerBuilder() { }

    /// <summary>
    /// 设置日志记录器的名称。
    /// </summary>
    /// <param name="loggerName">要设置的名称。</param>
    /// <returns>返回当前构建器实例，以便进行链式调用。</returns>
    public LoggerBuilder LoggerName(string loggerName)
    {
        _loggerName = loggerName;
        return this;
    }

    /// <summary>
    /// 向构建器添加一个日志输出目标（Sink）。
    /// </summary>
    /// <param name="target">要添加的 ILogTarget 实现。</param>
    /// <returns>返回当前构建器实例，以便进行链式调用。</returns>
    public LoggerBuilder AddSink(ILogTarget target, SinkOutputConfig? cfg = null)
    {
        if (cfg == null) cfg = new SinkOutputConfig();
        _sinks.Add(new Sink(target, cfg));
        return this;
    }

    /// <summary>
    /// 根据当前配置构建并返回一个新的 LunariumLogger 实例。
    /// </summary>
    /// <returns>配置完成的 LunariumLogger 实例。</returns>
    public ILogger Build()
    {
        // 如果用户没有配置全局设置，应用默认配置
        GlobalConfigurator.ApplyDefaultIfNotConfigured();
        try
        {
            var logger = new Logger(_sinks, _loggerName);
            return logger;
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex, "LoggerBuilder: Build Failed");
            throw;
        }
    }
}