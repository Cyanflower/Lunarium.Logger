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

using System.Threading.Channels;
using Lunarium.Logger.Target;

namespace Lunarium.Logger;


/// <summary>
/// 为 LoggerBuilder 提供方便的扩展方法，用于快速添加各种日志输出目标 (Sink)。
/// </summary>
public static class LoggerBuilderExtensions
{
    /// <summary>
    /// 为日志构建器添加一个控制台输出目标 (Sink)。
    /// 日志会直接输出到应用程序的控制台。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddConsoleSink(
        this LoggerBuilder builder,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        return builder.AddSink(target: new ConsoleTarget(), cfg: sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个文件输出目标 (Sink)，将日志写入指定路径的文件中。
    /// 如果文件不存在会自动创建，日志会追加到现有文件末尾。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="logFilePath">日志文件的路径。</param>
    /// <param name="sinkOutputConfig">输出配置。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddFileSink(
        this LoggerBuilder builder,
        string logFilePath,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        return builder.AddSink(target: new FileTarget(logFilePath), cfg: sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个按时间轮转的文件输出目标 (Sink)。
    /// 每天会生成一个新的日志文件。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="logFilePath">日志文件的基础路径。文件名将根据日期自动生成，例如 'path/to/log.txt' 会变成 'path/to/log-2023-10-27.txt'。</param>
    /// <param name="maxFile">保留的最大日志文件数量。0 或负数表示不限制。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddTimedRotatingFileSink(
        this LoggerBuilder builder,
        string logFilePath,
        int maxFile = 0, SinkOutputConfig? 
        sinkOutputConfig = null)
    {
        return builder.AddSink(
            target: new FileTarget(logFilePath, rotateOnNewDay: true, maxFile: maxFile),
            cfg: sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个按大小轮转的文件输出目标 (Sink)。
    /// 当日志文件达到指定大小时，会自动创建新的日志文件。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="logFilePath">日志文件的基础路径。当文件达到大小限制时，会自动创建带时间戳的新文件。</param>
    /// <param name="maxFileSizeMB">单个日志文件的最大大小 (MB)。</param>
    /// <param name="maxFile">保留的最大日志文件数量。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddSizedRotatingFileSink(
        this LoggerBuilder builder,
        string logFilePath,
        double maxFileSizeMB = 10,
        int maxFile = 10,
        SinkOutputConfig? 
        sinkOutputConfig = null)
    {
        return builder.AddSink(
            target: new FileTarget(logFilePath, maxFileSizeMB: maxFileSizeMB, maxFile: maxFile),
            cfg: sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个文件输出目标 (Sink)，可灵活组合按大小和/或按天的轮转策略。
    /// 两种策略可独立启用，也可同时启用（任一条件满足即触发轮转）。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="logFilePath">日志文件的基础路径，例如 "logs/app.log"。</param>
    /// <param name="maxFileSizeMB">
    /// 单个文件的最大大小 (MB)。超过此大小时触发轮转，文件名附加时间戳。
    /// ≤0 表示不按大小轮转。
    /// </param>
    /// <param name="rotateOnNewDay">是否在新的一天开始时触发轮转。</param>
    /// <param name="maxFile">
    /// 保留的最大日志文件数量。≤0 表示不限制。
    /// ⚠️ 注意：当 maxFile > 0 时，至少须启用一种轮转策略。
    /// </param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddRotatingFileSink(
        this LoggerBuilder builder, string logFilePath,
        double maxFileSizeMB = 10,
        bool rotateOnNewDay = true,
        int maxFile = 10,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        return builder.AddSink(
            target: new FileTarget(logFilePath, maxFileSizeMB, rotateOnNewDay, maxFile),
            cfg: sinkOutputConfig);
    }
    
    /// <summary>
    /// 为日志构建器添加一个将日志格式化为字符串并写入 Channel 的 Sink。
    /// 通过返回的 <paramref name="reader"/> 可在外部消费者中异步读取格式化后的日志文本。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="reader">输出参数，用于在外部消费者中读取日志字符串的 ChannelReader。</param>
    /// <param name="isColor">是否在输出文本中包含 ANSI 颜色转义码。默认为 false。</param>
    /// <param name="toJson">是否将日志格式化为 JSON 字符串。优先级高于 <paramref name="isColor"/>，启用时颜色设置无效。默认为 false。</param>
    /// <param name="capacity">Channel 的容量限制。为 null 时创建无界 Channel；指定容量时，Channel 满后新日志将被丢弃。</param>
    /// <param name="sinkOutputConfig">可选的输出过滤配置，用于控制日志级别等过滤条件。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddStringChannelSink(
        this LoggerBuilder builder,
        out ChannelReader<string> reader,
        bool isColor = false,
        bool toJson = false,
        int? capacity = null,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        var bridge = new LogChannelBridge<string>(capacity);
        reader = bridge.Reader;
        var sink = new StringChannelTarget(bridge.Writer, isColor) { ToJson = toJson };
        return builder.AddSink(sink, sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个将结构化 <see cref="LogEntry"/> 原样写入 Channel 的 Sink。
    /// 适用于下游消费者需要访问日志的完整结构化数据（如级别、时间戳、上下文等）的场景。
    /// 通过返回的 <paramref name="reader"/> 可在外部消费者中异步读取日志条目。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="reader">输出参数，用于在外部消费者中读取 <see cref="LogEntry"/> 的 ChannelReader。</param>
    /// <param name="capacity">Channel 的容量限制。为 null 时创建无界 Channel；指定容量时，Channel 满后新日志将被丢弃。</param>
    /// <param name="sinkOutputConfig">可选的输出过滤配置，用于控制日志级别等过滤条件。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddLogEntryChannelSink(
        this LoggerBuilder builder,
        out ChannelReader<LogEntry> reader,
        int? capacity = null,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        var bridge = new LogChannelBridge<LogEntry>(capacity);
        reader = bridge.Reader;
        return builder.AddSink(new LogEntryChannelTarget(bridge.Writer), sinkOutputConfig);
    }

    /// <summary>
    /// 为日志构建器添加一个自定义类型的 Channel Sink。
    /// 调用方通过 <paramref name="transform"/> 委托自定义 <see cref="LogEntry"/> 到目标类型 <typeparamref name="T"/> 的转换逻辑，
    /// 无需继承或实现任何基类/接口。
    /// 通过返回的 <paramref name="reader"/> 可在外部消费者中异步读取转换后的数据。
    /// </summary>
    /// <typeparam name="T">Channel 中传递的目标数据类型。</typeparam>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="reader">输出参数，用于在外部消费者中读取 <typeparamref name="T"/> 类型数据的 ChannelReader。</param>
    /// <param name="transform">将 <see cref="LogEntry"/> 转换为 <typeparamref name="T"/> 的委托，由调用方提供具体实现。</param>
    /// <param name="capacity">Channel 的容量限制。为 null 时创建无界 Channel；指定容量时，Channel 满后新日志将被丢弃。</param>
    /// <param name="sinkOutputConfig">可选的输出过滤配置，用于控制日志级别等过滤条件。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddChannelSink<T>(
        this LoggerBuilder builder,
        out ChannelReader<T> reader,
        Func<LogEntry, T> transform,
        int? capacity = null,
        SinkOutputConfig? sinkOutputConfig = null)
    {
        var bridge = new LogChannelBridge<T>(capacity);
        reader = bridge.Reader;
        return builder.AddSink(new DelegateChannelTarget<T>(bridge.Writer, transform), sinkOutputConfig);
    }

    /// <summary>
    /// 调用 <see cref="ISinkConfig.CreateTarget"/> 创建目标并注册 Sink，支持任意第三方实现。
    /// </summary>
    /// <param name="builder">要配置的 LoggerBuilder 实例。</param>
    /// <param name="sinkConfig">包含 Sink 专属参数与通用过滤配置的配置对象。</param>
    /// <returns>返回配置后的 LoggerBuilder 实例，以便进行链式调用。</returns>
    public static LoggerBuilder AddSink(this LoggerBuilder builder, ISinkConfig sinkConfig)
        => builder.AddSink(sinkConfig.CreateTarget(), sinkConfig.SinkOutputConfig);
}