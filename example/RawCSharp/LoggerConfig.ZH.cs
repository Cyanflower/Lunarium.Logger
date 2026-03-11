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

// ============================================================
//  本文件是 Lunarium.Logger 的完整使用示例，仅供参考，不参与编译。
//  展示了从全局配置、多 Sink 构建到日志写入的完整使用流程。
// ============================================================

using System.Threading.Channels;

namespace Lunarium.Logger;

/// <summary>
/// 日志构建示例 —— 演示 Lunarium.Logger 的典型配置方式。
///
/// <para><b>整体思路：</b></para>
/// <list type="bullet">
/// <item>主日志文件（Runtime.log）接收除特定模块外的所有日志，避免被高频模块刷屏。</item>
/// <item>每个高频/重要模块拥有独立日志文件，便于单独追查。</item>
/// <item>全局 Error.log 汇聚所有模块的错误，一站式排查线上异常。</item>
/// <item>Warning.log 仅收 Warning 级别，单独归档警告记录。</item>
/// <item>控制台输出用于开发期实时观察，级别可通过环境变量动态调整。</item>
/// <item>ChannelSink 将日志推送给外部消费者（如 UI 层、WebSocket 广播）。</item>
/// </list>
/// </summary>
public static class LoggerConfigurator
{
    /// <summary>
    /// 构建全局单例日志记录器。
    ///
    /// <para><b>环境变量：</b></para>
    /// <list type="table">
    /// <item><term>FILE_LOG_LEVEL</term><description>文件 Sink 最低日志级别，默认 Info。可选值：Debug / Info / Warning / Error / Critical（大小写不敏感）。</description></item>
    /// <item><term>CONSOLE_LOG_LEVEL</term><description>控制台 Sink 最低日志级别，默认 Info。开发期可设为 Debug 查看详细输出。</description></item>
    /// </list>
    /// </summary>
    /// <param name="outReader">
    /// 输出参数，返回 ChannelSink 对应的 <see cref="ChannelReader{T}"/>。
    /// 调用方可在后台任务中持续读取，将日志推送至 UI 或其他消费端。
    /// </param>
    /// <param name="context">
    /// 可选的根上下文。传入非 null 值时，返回的 ILogger 会自动为所有日志附加此上下文前缀。
    /// 例如传入 "MyApp" 后，日志上下文将变为 "MyApp.xxx"。
    /// </param>
    /// <returns>配置完毕的全局 ILogger 实例。应作为单例持有并注入到各模块。</returns>
    public static ILogger Build(out ChannelReader<string> outReader, string? context = null)
    {
        // ===================================================================
        // 【配置来源说明】
        //
        // Sink 的配置（SinkOutputConfig / ISinkConfig）本质上只是普通的 C# 对象，
        // 不要求硬编码在代码里。你可以从任何来源读取并构造它们：
        //
        //   - 环境变量（本示例的做法）
        //   - appsettings.json / IConfiguration（ASP.NET Core 推荐）
        //   - .env 文件（dotenv-net 等库）
        //   - 数据库 / 远程配置中心（Consul、Nacos 等）
        //   - 命令行参数
        //
        // 只需在 AddSink() / AddConsoleSink() / AddSizedRotatingILogTarget() 等方法
        // 中传入对应的 SinkOutputConfig 或 ISinkConfig 对象即可，构建器不关心值从哪里来。
        // ===================================================================

        // ===================================================================
        // 步骤一：从环境变量读取配置（仅作示例，并非推荐规范）
        //
        // 此处示例仅演示"通过环境变量动态控制日志级别"这一常见需求。
        // 这里恰好定义了两个变量（一个文件整体级别、一个控制台级别），
        // 并不意味着"每套 Sink 只能有一个级别变量"或"必须这样分组"。
        //
        // 实际上，SinkOutputConfig 的每个字段都可以来自环境变量，例如：
        //   - 为每个 Sink 单独配置 MinLevel / MaxLevel
        //   - 动态控制 ContextFilterIncludes / Excludes
        //   - 按环境切换 ToJson 开关
        //
        // 示例（仅演示思路，未在本构建器中使用）：
        //   var proxyMinLevel  = ParseEnvLevel("PROXY_LOG_LEVEL",  LogLevel.Info);
        //   var loginMinLevel  = ParseEnvLevel("LOGIN_LOG_LEVEL",  LogLevel.Debug);
        //   var errorFilePath  = Environment.GetEnvironmentVariable("ERROR_LOG_PATH") ?? "Logs/Error.log";
        //   var enableJsonMode = bool.Parse(Environment.GetEnvironmentVariable("LOG_JSON") ?? "false");
        //
        // 完全由你决定哪些参数需要外部化、哪些可以固定。
        // ===================================================================
        var envFileLogLevel = Environment.GetEnvironmentVariable("FILE_LOG_LEVEL");
        var fileLogLevel = LogLevel.Info;
        if (!string.IsNullOrEmpty(envFileLogLevel) &&
            Enum.TryParse<LogLevel>(envFileLogLevel, true, out var parsedFileLevel))
        {
            fileLogLevel = parsedFileLevel;
        }

        var envConsoleLogLevel = Environment.GetEnvironmentVariable("CONSOLE_LOG_LEVEL");
        var consoleLogLevel = LogLevel.Info;
        if (!string.IsNullOrEmpty(envConsoleLogLevel) &&
            Enum.TryParse<LogLevel>(envConsoleLogLevel, true, out var parsedConsoleLevel))
        {
            consoleLogLevel = parsedConsoleLevel;
        }

        // ===================================================================
        // 步骤二：全局配置（必须在 Build() 之前完成，且只能调用一次）
        //
        // GlobalConfigurator.Configure() 返回流式构建器，最后调用 Apply() 生效。
        // 若不调用，Build() 会自动应用默认配置：
        //   - 时区：本地时间
        //   - JSON 时间戳：ISO 8601（"O"）
        //   - 文本时间戳："yyyy-MM-dd HH:mm:ss.fff"
        //   - 集合自动解构：关闭
        //   - JSON 中文转义：保留（不转义）
        //   - JSON 缩进：紧凑（单行）
        // ===================================================================
        GlobalConfigurator.Configure()
            // 使用 Asia/Shanghai 时区（UTC+8）。
            // 服务器通常运行在 UTC，手动指定时区可确保日志时间与业务时间一致。
            // 其他选项：.UseUtcTimeZone() / .UseLocalTimeZone()
            .UseCustomTimezone(TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"))

            // JSON Sink 的时间戳格式：ISO 8601（如 "2026-03-09T14:30:00.000+08:00"）。
            // 其他选项：.UseJsonUnixTimestamp()（秒） / .UseJsonUnixMsTimestamp()（毫秒）
            //           .UseJsonCustomTimestamp("yyyy-MM-ddTHH:mm:ss")
            .UseJsonISO8601Timestamp()

            // 文本 Sink 的时间戳格式，含时区偏移量（zzz），便于跨时区排查。
            // 其他选项：.UseTextISO8601Timestamp() / .UseTextUnixTimestamp()
            //           .UseTextUnixMsTimestamp()
            .UseTextCustomTimestamp("yyyy-MM-dd HH:mm:ss.fff zzz")

            // 启用集合自动解构：List<T>、Dictionary、数组等集合类型在记录时
            // 无需手动加 {@} 前缀，会自动序列化为 JSON 数组/对象。
            // 若不启用，集合只会输出 ToString() 结果（通常是类型名）。
            .EnableAutoDestructuring()

            // 其他可用配置（此示例未启用）：
            // .UseIndentedJson()              // JSON 输出换行缩进（便于阅读，体积略大）
            // .UseCompactJson()               // 显式设为紧凑模式（与默认行为相同，适用于表达意图）
            // .EscapeChineseCharacters()      // 将中文转义为 \uXXXX（默认保留原始中文）
            // .PreserveChineseCharacters()    // 显式保留中文（与默认行为相同，适用于表达意图）
            .Apply();

        // ===================================================================
        // 步骤三：构建 Logger 实例并配置多个 Sink
        //
        // 每个 Sink 可独立配置：
        //   - LogMinLevel / LogMaxLevel：只接收指定级别范围内的日志
        //   - ContextFilterIncludes：白名单，只接收以指定前缀开头的 Context
        //   - ContextFilterExcludes：黑名单，排除以指定前缀开头的 Context
        //   - IgnoreFilterCase：上下文前缀匹配是否忽略大小写（默认区分）
        //   - ToJson：以 JSON 格式输出（默认文本格式）
        //
        // Include 和 Exclude 可以同时存在：
        //   先执行 Include 过滤（通过后继续），再执行 Exclude 过滤（匹配则排除）。
        //   Include 为空 = 全部通过。
        // ===================================================================
        var rootLogger = new LoggerBuilder()
            .LoggerName("Runtime")  // 日志记录器名称，输出在日志中以区分多个 Logger 实例

            // ---------------------------------------------------------------
            // 主日志文件：接收除 ProxyService、LoginService 之外的所有模块日志
            //
            // 排除高频模块避免主日志被刷屏，高频模块有自己的专属文件。
            // 轮转策略：单文件最大 10 MB，最多保留 5 个历史文件（约 50 MB 上限）。
            // ---------------------------------------------------------------
            .AddSizedRotatingILogTarget(
                logFilePath: "Logs/Runtime/Runtime.log",
                maxFileSizeMB: 10,
                maxFile: 5,
                sinkOutputConfig: new SinkOutputConfig
                {
                    ContextFilterExcludes = ["Runtime.ProxyService", "Runtime.LoginService"],
                    LogMinLevel = fileLogLevel,
                })

            // ---------------------------------------------------------------
            // ProxyService 专属日志：只收 Runtime.ProxyService 前缀的日志
            //
            // Include 过滤确保此文件只记录代理服务相关日志，方便单独追查。
            // 保留 10 个历史文件，代理服务日志量大，给更多空间。
            // ---------------------------------------------------------------
            .AddSizedRotatingILogTarget(
                logFilePath: "Logs/ProxyService/ProxyService.log",
                maxFileSizeMB: 10,
                maxFile: 10,
                sinkOutputConfig: new SinkOutputConfig
                {
                    ContextFilterIncludes = ["Runtime.ProxyService"],
                    LogMinLevel = fileLogLevel
                })

            // ---------------------------------------------------------------
            // LoginService 专属日志：只收 Runtime.LoginService 前缀的日志
            //
            // 登录相关操作独立归档，便于安全审计和登录问题排查。
            // ---------------------------------------------------------------
            .AddSizedRotatingILogTarget(
                logFilePath: "Logs/Login/Login.log",
                maxFileSizeMB: 10,
                maxFile: 10,
                sinkOutputConfig: new SinkOutputConfig
                {
                    ContextFilterIncludes = ["Runtime.LoginService"],
                    LogMinLevel = fileLogLevel
                })

            // ---------------------------------------------------------------
            // 全局错误日志：汇聚所有模块的 Error 及以上级别日志
            //
            // 无上下文过滤，所有模块的错误都写入此文件。
            // 生产环境排查线上问题时，优先看此文件。
            // ---------------------------------------------------------------
            .AddSizedRotatingILogTarget(
                logFilePath: "Logs/EW/Error.log",
                maxFileSizeMB: 10,
                maxFile: 5,
                sinkOutputConfig: new SinkOutputConfig
                {
                    LogMinLevel = LogLevel.Error
                    // LogMaxLevel 默认为 Critical，即接收 Error 和 Critical
                })

            // ---------------------------------------------------------------
            // Warning 专属日志：仅收 Warning 级别（精确匹配单一级别）
            //
            // 通过同时设置 Min 和 Max 为 Warning，实现"仅此一个级别"的精确过滤。
            // Warning 通常代表需要关注但不紧急的潜在问题，独立归档便于周期性回顾。
            // ---------------------------------------------------------------
            .AddSizedRotatingILogTarget(
                logFilePath: "Logs/EW/Warning.log",
                maxFileSizeMB: 10,
                maxFile: 5,
                sinkOutputConfig: new SinkOutputConfig
                {
                    LogMinLevel = LogLevel.Warning,
                    LogMaxLevel = LogLevel.Warning  // Min == Max，精确匹配单个级别
                })

            // ---------------------------------------------------------------
            // 按天轮转的文件 Sink 示例
            //
            // 每天生成一个新文件，文件名含日期：
            //   Audit-2026-03-09.log / Audit-2026-03-10.log ...
            // 适合按日期归档的审计日志，便于按时间段检索。
            // maxFile: 保留最近 30 天，更早的自动删除。
            // ---------------------------------------------------------------
            .AddTimedRotatingILogTarget(
                logFilePath: "Logs/Audit/Audit.log",
                maxFile: 30,
                sinkOutputConfig: new SinkOutputConfig
                {
                    ContextFilterIncludes = ["Runtime.Audit"],
                    LogMinLevel = LogLevel.Info
                })

            // ---------------------------------------------------------------
            // 叠加轮转示例：按大小 + 按天同时启用
            //
            // 任一条件满足即触发轮转：
            //   - 文件超过 50 MB → 立即轮转，文件名含完整时间戳
            //       System-2026-03-10-09-30-00.log
            //   - 新的一天开始 → 轮转到新文件
            //       System-2026-03-11-00-00-01.log
            //
            // 适合流量大、同时又需要按天归档的核心系统日志。
            // maxFile: 保留最近 14 个文件（跨天轮转和超大文件均计入总数）。
            // ---------------------------------------------------------------
            .AddRotatingILogTarget(
                logFilePath: "Logs/System/System.log",
                maxFileSizeMB: 50,
                rotateOnNewDay: true,
                maxFile: 14,
                sinkOutputConfig: new SinkOutputConfig
                {
                    ContextFilterIncludes = ["Runtime.System"],
                    LogMinLevel = fileLogLevel
                })

            // ---------------------------------------------------------------
            // 控制台 Sink：输出到终端，开发期实时观察
            //
            // 非重定向时自动使用 ANSI 彩色输出（LogColorTextWriter），
            // 重定向（如 > file 或 Docker 日志采集）时自动降级为纯文本。
            // Error / Critical 级别自动写入 stderr，其余写入 stdout。
            // ---------------------------------------------------------------
            .AddConsoleSink(
                sinkOutputConfig: new SinkOutputConfig
                {
                    LogMinLevel = consoleLogLevel
                })

            // ---------------------------------------------------------------
            // ChannelSink：将日志推送到 .NET Channel 供外部消费
            //
            // 典型用途：
            //   - Blazor / WinForms UI 实时展示日志
            //   - WebSocket 将日志广播给前端调试面板
            //   - 跨线程传递日志到其他处理管道
            //
            // channelCapacity: 1000 —— 有界 Channel，满时丢弃新写入（DropWrite），
            //   防止消费者慢时内存无限增长。
            // isColor: true —— 输出含 ANSI 转义码的彩色文本（适合终端展示，或前端模拟的 Log 终端页面利用ANSI进行着色时）。
            //   若下游需要解析日志内容，应设为 false 或启用 ToJson。
            // ToJson: true —— 当 isColor 和 ToJson 同时设置时，JSON 优先，
            //   确保下游 JSON 解析不被颜色转义码干扰。
            // ---------------------------------------------------------------
            .AddChannelSink(
                out var reader,
                channelCapacity: 1000,
                isColor: true,
                sinkOutputConfig: new SinkOutputConfig
                {
                    LogMinLevel = LogLevel.Info,
                    ToJson = true  // JSON 优先级高于 isColor，下游可直接反序列化
                })

            .Build();

        // --- ISinkConfig 配置对象化用法（适合外部配置源场景）---
        //
        // 上面使用的是 "AddXxxSink(参数)" 直接传参方式。
        // 如果 Sink 配置来自外部（如 JSON 文件、数据库），可使用 ISinkConfig 实现类。
        // 内置可用：ILogTargetConfig（所有文件模式）、ConsoleSinkConfig
        //
        // ISinkConfig[] sinkConfigs =
        // [
        //     new ILogTargetConfig
        //     {
        //         LogFilePath = "Logs/Runtime/Runtime.log",
        //         MaxFileSizeMB = 10,
        //         MaxFile = 5,
        //         SinkOutputConfig = new SinkOutputConfig
        //         {
        //             ContextFilterExcludes = ["Runtime.ProxyService"],
        //             LogMinLevel = fileLogLevel
        //         }
        //     },
        //     new ConsoleSinkConfig
        //     {
        //         SinkOutputConfig = new SinkOutputConfig { LogMinLevel = consoleLogLevel }
        //     }
        // ];
        // var builder2 = new LoggerBuilder().LoggerName("Runtime");
        // foreach (var cfg in sinkConfigs) builder2.AddSink(cfg);
        // var logger2 = builder2.Build();

        // 注意：Build() 内部有全局单例锁，同一进程内第二次调用会抛出 InvalidOperationException。
        // Logger 应作为全局单例持有，应用启动阶段创建一次，其后通过依赖注入或静态属性共享给各模块。
        outReader = reader;

        // 若调用方传入了根上下文，返回附加了该上下文的 LoggerWrapper。
        // 例如传入 "MyApp"，则所有日志的 Context 将以 "MyApp." 为前缀。
        if (context is not null) return rootLogger.ForContext(context);
        else return rootLogger;
    }
}
