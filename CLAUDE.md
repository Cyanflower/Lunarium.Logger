# CLAUDE.md — Lunarium.Logger

## 项目概述

.NET 10.0 结构化日志库（Class Library）。参考 Serilog 设计的消息模板解析，基于 .NET Channel 的异步日志分发架构。

- **工作目录**: `/home/cyanflower/Projects/Lunarium.Logger/`
- **构建**: `dotnet build`
- **目标框架**: net10.0，唯一外部依赖 `Microsoft.Extensions.Logging`（用于 MEL 桥接）
- **示例代码**: `example/`

---

## example/ 目录结构

`example/` 中的所有文件均为纯参考性伪代码，可自由使用占位符（`someCondition`、`...` 等）。

### 文件命名规则

每份示例提供**四个文件**，以 `<名称>.<语言>.<后缀>` 格式组织：

| 文件 | 说明 |
|------|------|
| `<Name>.ZH.md` | 中文 Markdown 版（主要阅读格式，含 Markdown 表格与说明） |
| `<Name>.EN.md` | 英文 Markdown 版 |
| `RawCSharp/<Name>.ZH.cs` | 中文 C# 源文件（Markdown 版的完全等价的原始代码形式） |
| `RawCSharp/<Name>.EN.cs` | 英文 C# 源文件 |

> **MD 版与 RawCSharp 版的关系**：两者内容完全对应，MD 版在代码块之间额外插入了 Markdown 标题、表格和说明文字，对同一代码片段的解释更丰富；RawCSharp 版适合直接复制到 IDE 中参考或对照阅读。

### 当前示例文件

| 示例名 | 主题 |
|--------|------|
| `UsageExample` | 核心用法：日志级别、ForContext、消息模板语法、Channel Sink 消费、MEL 桥接、`ILogger.Log()` 底层调用 |
| `LoggerConfig` | 完整配置：GlobalConfigurator、多 Sink 策略（大小/按天/叠加轮转）、ISinkConfig 配置对象化、IgnoreFilterCase |
| `AdvancedUsage` | 进阶用法：自定义 `ILogTarget` 实现（内存 Sink + 异步数据库 Sink）、LogEntry 字段说明 |

---

## 核心架构

### 日志写入流程

```
调用方: _logger.{level}(...)
        ↓
ILogger.Log(level, ...)
        ↓
TryWrite(LogEntry)
        ↓
Channel<LogEntry>
        ↓
后台 ProcessQueueAsync
        ↓
foreach Sink → Sink.Emit(logEntry)   ← 过滤/解析/输出均封装在此
    ↓
LoggerFilter.ShouldEmit()    ← 级别 + 上下文前缀过滤
LogEntry.ParseMessage()      ← 延迟解析模板
ILogTarget.Emit(logEntry)
        ↓
WriterPool.Get<T>()
LogWriter.Render()
FlushTo(output)
LogWriter.Return()
```

### 单例约束

- `GlobalConfigLock`：`GlobalConfigurator` 只能配置一次，`Build()` 时若未配置则自动应用默认值。
- FileTarget 路径唯一性：进程级 `_activePaths` 静态字典跨所有 Logger 实例追踪活跃文件路径，在 `FileTarget` 构造时检查并注册，`Dispose` 时自动释放。同一路径不能被两个活跃 `FileTarget` 持有。

---

## 关键文件索引

```
src/Lunarium.Logger/
├── ILogger.cs               # 公共接口（GetContext/GetContextSpan + Debug/Info/Warning/Error/Critical 重载）
├── IDestructurable.cs       # IDestructurable（Destructure(DestructureHelper)）、IDestructured（Destructured() → ReadOnlyMemory<byte>）
├── DestructureHelper.cs     # 高性能自定义解构辅助类（public，包装 Utf8JsonWriter，供 IDestructurable 实现者使用）
├── Logger.cs                # 核心实现，internal sealed（Channel<LogEntry> + 后台任务，预存 _loggerNameBytes）
├── LoggerBuilder.cs         # Fluent Builder，Build() 返回 ILogger，可多次调用
├── LoggerExtensions.cs      # ForContext()/ForContext<T>() 扩展方法（LoggerBuilderExtensions 已抽离）
├── LoggerBuilderExtensions.cs  # LoggerBuilder 扩展方法（AddConsoleSink/AddFileSink/AddChannelSink 等）
├── SinkOutputConfig.cs      # 单个 Sink 配置（级别过滤、上下文过滤、ToJson?、IsColor?）
├── LogSink.cs               # Sink 结构体（internal readonly record struct）
├── LogLevel.cs              # Debug/Info/Warning/Error/Critical 枚举
├── LogUtilsAPI.cs           # LogUtils 工具类（获取时间戳）
├── GlobalConfigurator.cs    # 全局配置入口（Fluent API，Apply() 完成）
├── GlobalUsings.cs          # 全局 using
├── GlobalConfigExtensions.cs# 占位符文件（暂未实现）
├── ISinkConfig.cs           # Sink 配置对象接口（含 SinkOutputConfig 引用）
├── Lunarium.Logger.csproj   # 主项目文件
│
├── Models/
│   └── LogEntry.cs          # 不可变日志事件（sealed，含 ContextBytes/Scope/延迟解析的 MessageTemplate）
│
├── Parser/
│   ├── LogParser.cs         # 消息模板状态机解析器（internal，含 4096 条缓存，Interlocked 并发安全）
│   └── MessageTemplate.cs   # MessageTemplate（含 OriginalMessageBytes）/ TextToken（含 TextBytes）/ PropertyToken
│                            #   PropertyToken 含 FormatString（预构建）、PropertyNameBytes
│                            #   类型均 public，但构造器 internal；Token 属性对外只读 public
│
├── Target/
│   ├── ILogTarget.cs        # ILogTarget（Emit+IDisposable）、IJsonTextTarget、IColorTextTarget、ILogEntryTarget
│   ├── ConsoleTarget.cs     # 控制台输出，sealed（Error/Critical → stderr，缓存底层流，直接写字节）
│   ├── FileTarget.cs        # 文件输出，sealed（按大小 / 按天 / 叠加轮转，直接写 FileStream 无中间 StreamWriter）
│   └── ChannelTarget.cs     # ChannelTarget<T>（public abstract，用户可继承）
│                            #   + StringChannelTarget（string，有 string 堆分配警告）
│                            #   + ByteChannelTarget（byte[]，跳过 UTF-8→string 解码）
│                            #   + LogEntryChannelTarget（透传 LogEntry）
│                            #   + DelegateChannelTarget<T>（internal sealed）
│
├── Writer/
│   ├── LogWriter.cs         # internal abstract 基类（Render 流程 + 对象池管理 + RenderPropertyToken/WriteAligned/AppendPadding 虚方法）
│   ├── LogTextWriter.cs     # internal sealed，纯文本格式（BufferWriter + UTF-8 字面量）
│   ├── LogColorTextWriter.cs# internal sealed，带 ANSI 颜色的文本格式（ANSI 常量为 ReadOnlySpan<byte>）
│   ├── LogJsonWriter.cs     # internal sealed，JSON 格式（Utf8JsonWriter，_scratchWriter 副缓冲区）
│   └── WriterPool.cs        # internal static，对象池（ConcurrentBag，上限 128，容量超 32KB 不回池）
│
├── Filter/
│   └── Filter.cs            # LoggerFilter（internal sealed，上下文前缀过滤 + 2048 条缓存，Interlocked 并发安全）
│
├── Internal/
│   ├── ArrayBufferWriter.cs # BufferWriter（internal sealed，IBufferWriter<byte>，ArrayPool 不使用，自管理 byte[]）
│   └── LogChannelBridge.cs  # LogChannelBridge<T>（internal sealed，Channel 桥接器）
│
├── Wrapper/
│   ├── IContextProvider.cs  # IContextProvider（internal interface，GetContext()/GetContextSpan()，由 LoggerWrapper 实现）
│   └── LoggerWrapper.cs     # LoggerWrapper（internal sealed，扁平化装饰器，Context/ContextBytes 构造时预计算）
│
├── GlobalConfig/
│   ├── GlobalConfigLock.cs  # GlobalConfigLock（internal）
│   ├── TimestampConfig.cs   # 时区配置（Local / UTC / Custom，internal）
│   ├── TimestampFormatConfig.cs  # 时间戳格式（Unix / UnixMs / ISO8601 / Custom，internal）
│   ├── DestructuringConfig.cs    # 集合自动解构开关（internal）
│   └── JsonSerializationConfig.cs# JSON 序列化配置（UnsafeRelaxedJsonEscaping、缩进、自定义 Resolver，internal）
│
├── InternalLoggerUtils/
│   └── InternalLogger.cs    # 库内部错误输出（internal static，不走 Channel，直接到控制台）
│
├── SinkConfig/
│   ├── ConsoleSinkConfig.cs # ConsoleTarget 的完整配置（public sealed record）
│   └── FileSinkConfig.cs    # FileTarget 的完整配置（public sealed record）
│
└── Extensions/
    └── MicrosoftLoggingBridge.cs  # MEL 适配（LunariumLoggerProvider / ILoggingBuilder 扩展）
                                   #   LunariumMsLoggerAdapter（internal sealed）
```

---

## 关键设计约定

### 消息模板语法

```
{PropertyName}          // 默认（ToString）
{@Object}               // 解构（JSON 序列化）
{$Object}               // 字符串化
{Value,10}              // 右对齐 10 位
{Value,-10}             // 左对齐 10 位
{Value:F2}              // 格式化
{Value,10:F2}           // 对齐 + 格式化（顺序固定: 对齐在前）
{{ }} 转义为 { }
```

属性名规则：首字符 Letter 或 `_`，后续可含字母/数字/下划线/`.`（`.` 后不能接数字或连续 `.`）。

### ILogger.Log() 签名

```csharp
void Log(LogLevel level, Exception? ex = null, string message = "", string context = "",
         ReadOnlyMemory<byte> contextBytes = default, string scope = "", params object?[] propertyValues);
```

- `contextBytes`：context 的 UTF-8 预编码字节，供 Writer 层零分配使用；传 `default` 时 Logger/LoggerWrapper 自动填充缓存好的字节
- `scope`：由 MEL 适配器填充的作用域信息，业务代码无需手动提供
- `Exception?`-first 重载：Debug/Info/Warning 新增 `(Exception? ex)` 和 `(Exception? ex, string message, params...)` 两个重载

### IContextProvider（Wrapper/IContextProvider.cs）

```csharp
internal interface IContextProvider
{
    internal string GetContext();
    internal ReadOnlyMemory<byte> GetContextSpan();
}
```

`GetContext()` 和 `GetContextSpan()` 已从 `ILogger` 接口移至 `IContextProvider`（internal interface），仅由 `LoggerWrapper` 实现。`LoggerWrapper` 构造时检查传入的 `ILogger` 是否为 `LoggerWrapper`（`is LoggerWrapper wrapper`），若是则直接调用 `wrapper.GetContext()` 拼接 context 路径；若不是（即根 Logger）则直接用传入的 context 字符串。

### LogEntry 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `LogLevel` | `LogLevel` | 日志级别 |
| `Timestamp` | `DateTimeOffset` | 时间戳 |
| `Message` | `string` | 原始消息模板字符串 |
| `Properties` | `object?[]` | 模板属性值 |
| `Context` | `string` | 上下文（来源模块） |
| `ContextBytes` | `ReadOnlyMemory<byte>` | Context 的 UTF-8 预编码字节 |
| `Scope` | `string` | MEL Scope 信息 |
| `Exception` | `Exception?` | 关联异常 |
| `MessageTemplate` | `MessageTemplate` | 延迟解析的模板（Emit 时填充） |

### Context 约定

Context 应为静态字符串（类名、模块名），不应含动态值（会导致 Filter 缓存爆炸），动态值应位于日志内容本身。

```csharp
// 正确
var log = logger.ForContext("Order.Processor");
log.Info("Processing {Id}", orderId);

// 错误
logger.Info("Processing", $"Order.{orderId}"); // 上下文动态变化
```

多重 `ForContext` 时路径用 `.` 分隔：`"A"` → `ForContext("B")` → Context = `"A.B"`。

**LoggerWrapper 扁平化**：多层 `ForContext` 不再形成链式 wrapper。构造时直接解包到根 Logger，Context 字符串和 UTF-8 bytes 在构造时一次性拼接缓存，后续 Log 调用零分配。

### SinkOutputConfig 过滤逻辑

1. 级别检查：`LogMinLevel ≤ entry.Level ≤ LogMaxLevel`
2. Include 规则：如果配置了，Context 必须以列表中某个前缀开头
3. Exclude 规则：如果 Context 匹配任一排除前缀，则过滤

Include 为空 = 全部通过；Exclude 优先级低于 Include 但在其之后执行。

### 文件 Sink 约定

- 同一文件路径不能被多个 FileTarget 同时持有（FileTarget 构造时检查，进程级跨 Logger 追踪，抛 `InvalidOperationException`；Dispose 时自动释放）
- 按大小轮转：文件名 `app-yyyy-MM-dd-HH-mm-ss.log`
- 按天轮转：文件名 `app-yyyy-MM-dd.log`
- 缓冲刷新：每 10 秒定时 + Error/Critical 立即刷新

### SinkOutputConfig 可空属性语义

`ToJson: bool?` 和 `IsColor: bool?` 均为可空类型，`null` 表示"不覆盖目标的默认值"：

- `null`（默认）：`Sink` 构建时不修改 `IJsonTextTarget.ToJson` / `IColorTextTarget.IsColor`，目标保持自身默认值
- `true` / `false`：`Sink` 构建时通过 `SetJsonSinkProperty()` 强制写入目标属性

### Sink 结构体设计

`Sink`（`LogConfig.cs`，`Lunarium.Logger` 命名空间）是**逻辑单元**而非纯数据容器，封装了单条日志从过滤到输出的完整链路：

```
Sink.Emit(logEntry)
  ├─ LoggerFilter.ShouldEmit()  ← 级别 + 上下文过滤
  ├─ logEntry.ParseMessage()    ← 延迟模板解析
  └─ ILogTarget.Emit(logEntry)  ← 写入目标
```

`ProcessQueueAsync` 只需 `foreach sink → sink.Emit(entry)`，无需关心内部细节。

### 延迟解析

`LogEntry` 在进入 Channel 时携带 `EmptyMessageTemplate`，实际解析在 `Sink.Emit()` 内部调用 `entry.ParseMessage()` 时发生，避免调用方线程执行解析。

### DisposeAsync 顺序

1. 关闭 Channel Writer（不再接受新日志）
2. 取消 CancellationToken
3. 等待后台处理任务结束
4. 逐一 Dispose 所有 Sink

---

## 全局默认配置

未调用 `GlobalConfigurator.Configure()` 时，`Build()` 自动应用：

| 配置项 | 默认值 |
|--------|--------|
| 时区 | 本地时间 |
| JSON 时间戳格式 | ISO 8601（`"O"`） |
| 文本时间戳格式 | `"yyyy-MM-dd HH:mm:ss.fff"` |
| 集合自动解构 | 关闭 |
| JSON 中文转义 | 保留（不转义） |
| JSON 缩进 | 紧凑（单行） |

---

## 测试

### 测试项目

| 项目 | 说明 | 代码索引 |
|------|------|----------|
| `tests/Lunarium.Logger.Tests/` | 单元测试（Parser、Writer、Filter、Core、Extensions 等模块的行为验证） | `tests/Lunarium.Logger.Tests/Index.md` |
| `tests/Lunarium.Logger.IntegrationTests/` | 集成测试（ConsoleTarget / FileTarget 的物理 I/O 验证，含轮转、恢复、多流分发） | `tests/Lunarium.Logger.IntegrationTests/Index.md` |

> **Index.md 用途**：按目标类列出每个测试文件的覆盖场景，修改核心逻辑后可通过搜索 Index.md 快速定位受影响的测试用例，无需全局扫描测试目录。

### 运行测试

```bash
dotnet test                    # 运行所有测试（无覆盖率）
./RunCoverageTest.sh           # 运行测试 + 生成 TextSummary 覆盖率报告（→ CoverageReport/Summary.txt）
./RunCoverageTest.sh --html    # 运行测试 + 生成 HTML 覆盖率报告（→ CoverageReport/html/index.html）
```

### Benchmark 项目

| 项目 | 说明 | 代码索引 |
|------|------|----------|
| `benchmarks/Lunarium.Logger.Benchmarks/` | 性能基准测试（Parser 缓存命中/未命中、Writer 渲染、Filter 过滤、Logger 调用方吞吐量） | `benchmarks/Lunarium.Logger.Benchmarks/Index.md` |

**运行方式**：
```bash
# 交互式菜单选择
dotnet run -c Release --project benchmarks/Lunarium.Logger.Benchmarks

# 按类名过滤运行
dotnet run -c Release --project benchmarks/Lunarium.Logger.Benchmarks -- --filter "*LogParser*"
```

> ⚠️ 必须以 Release 模式运行，Debug 模式结果无意义。结果自动输出至 `BenchmarkDotNet.Artifacts/`。
> ⚠️ AI 助手应当尽量避免执行 Benchmarks 测试，在执行前必须征得同意并确认，该命令执行耗时较长(10min +)，可能会导致 AI Agent 前端认为超时。

### 性能参考数据（2026-03-15，i7-8750H，.NET 10.0）

> 历史记录见 `BenchmarkReports/History/`，最新为 `Dev-2026-03-15-Run.006`。

**LoggerFilter / LogParser（热路径）**

| 场景 | 耗时 | 分配 |
|------|------|------|
| Filter 缓存命中（各规则组合） | ~9 ns | 0 |
| Filter 缓存未命中（超出 2048 条清空） | ~243 ns | 218 B |
| Parser 缓存命中（各模板复杂度） | 11–19 ns | 0 |
| Parser 缓存未命中（超出 4096 条清空） | ~1,305 ns | 1,413 B |

**LogWriter 渲染**

| 场景 | 耗时 | 分配 |
|------|------|------|
| Text / Color（无论属性数） | 320–600 ns | **32 B 固定** |
| JSON（无论属性数） | 470–750 ns | **64 B 固定** |
| JSON `{@Object}` IDestructured 路径 | ~602 ns | 64 B |
| JSON `{@Object}` JsonSerializer 回退路径 | ~953 ns | 656 B |
| WriterPool Get + Return（池化） | ~77 ns | 0 |

**Logger 调用侧吞吐**

| 场景 | 耗时 | 分配 |
|------|------|------|
| 无属性 | ~186 ns | 113 B |
| 单属性 | ~192 ns | 146 B |
| 五属性 | ~212 ns | 227 B |
| ForContext 包装器 | ~199 ns | 146 B |

> 调用侧的分配主要来自 `params object?[]` boxing，是当前 API 的固有成本。

**ChannelTarget**

| Target | 耗时 | 分配 |
|--------|------|------|
| `LogEntryChannelTarget` | ~46 ns | 0 |
| `ByteChannelTarget` | ~915 ns | 128 B |
| `StringChannelTarget` | ~1,130 ns | 192 B |

### 测试现状（2026-03-15）

- **行覆盖率**: 91.2%（2214/2426 行）
- **分支覆盖率**: 87.9%（694/789 分支）
- **方法覆盖率**: 98.8%（340/344 个方法）

覆盖率较低的模块（< 90%）：

| 模块 | 覆盖率 |
|------|--------|
| `FileTarget` | 78.1% |
| `LoggerBuilder` | 81.8% |
| `Logger` | 83.1% |
| `LogWriter` | 83.1% |
| `LogColorTextWriter` | 87.0% |
| `InternalLogger` | 89.3% |
| `LunariumMsLoggerAdapter` | 89.4% |

覆盖率报告生成：`reportgenerator` → `CoverageReport/Summary.txt`

---

## 已实现功能

- [x] 异步日志队列（`Channel<LogEntry>` + 后台任务）
- [x] 结构化消息模板解析（状态机，支持对齐/格式化/解构前缀/转义 `{{ }}`）
- [x] 模板解析缓存（`ConcurrentDictionary`，上限 4096 条清空重用）
- [x] 上下文前缀过滤（Include/Exclude，带缓存，上限 2048 条）
- [x] 多 Sink 支持（每个 Sink 独立配置过滤规则）
- [x] ConsoleTarget（彩色/纯文本/JSON，Error+ → stderr，输出重定向自动降级）
- [x] FileTarget（统一文件 Sink，支持按大小轮转 / 按天轮转 / 两者叠加，可选参数控制，扩展方法：`AddSizedRotatingFileSink` / `AddTimedRotatingFileSink` / `AddRotatingFileSink`）
- [x] ChannelTarget 四变体：`StringChannelTarget`（string，有 string 堆分配）、`ByteChannelTarget`（byte[]，跳过 UTF-8→string 解码）、`LogEntryChannelTarget`（透传 LogEntry）、`DelegateChannelTarget<T>`（自定义转换，internal）
- [x] LogWriter 对象池（`WriterPool`，`ConcurrentBag`，上限 128，容量超 32KB 不回池，拒绝回池时调用 `DisposeAndReturnArrayBuffer()`）
- [x] ForContext 装饰器（`LoggerWrapper`，扁平化，Context/ContextBytes 构造时预计算，多层嵌套零额外分配）
- [x] GlobalConfigurator（一次性全局配置，Fluent API）
- [x] 单例 Logger 保证（构建锁 + 配置锁）
- [x] Microsoft.Extensions.Logging 桥接（`LunariumLoggerProvider`，支持 DI 注册）
- [x] LogUtils 工具 API（获取当前日志系统时间戳）
- [x] InternalLogger（库内部错误隔离输出）
- [x] ISinkConfig 配置对象注册（`ConsoleSinkConfig` / `FileSinkConfig`，通过 `builder.AddSink(ISinkConfig)` 扩展方法注册，与 Fluent 方式可互换；接口含 `CreateTarget()` 工厂方法，第三方实现可直接走统一注册路径）
- [x] 扩展方法：`AddStringChannelSink`、`AddLogEntryChannelSink`、`AddChannelSink<T>`（自定义转换）
- [x] AOT 兼容性支持（`IsAotCompatible=true`；`{@Object}` 解构序列化统一走 `LogWriter.TrySerializeToJson()`，附 `[UnconditionalSuppressMessage]`；`IsCommonCollectionType` 改用 `obj is Array` 消除运行时反射）
- [x] `GlobalConfigurator.UseJsonTypeInfoResolver()`（注册 Source Generated `JsonSerializerContext` / `IJsonTypeInfoResolver`，AOT 环境下 `{@Object}` 解构无需反射；多 Context 可通过 `JsonTypeInfoResolver.Combine()` 在外部合并后传入）
- [x] `IDestructurable` 接口（`Destructure(DestructureHelper helper)`，通过 `DestructureHelper` 包装的 `Utf8JsonWriter` 直接写缓冲区，零中间字符串分配）
- [x] `IDestructured` 接口（`Destructured() → ReadOnlyMemory<byte>`，返回预序列化字节，极致零分配）
- [x] `ILogger` 增加 `GetContext()` / `GetContextSpan()` 方法，支持 LoggerWrapper 构造时读取父级 context
- [x] `LogEntry` 增加 `ContextBytes`（UTF-8 预编码字节）和 `Scope`（MEL 作用域）字段
- [x] `PropertyToken` 预构建 `FormatString`，`TextToken`/`PropertyToken` 预编码 UTF-8 bytes，减少渲染时重复计算

---

## 性能优化架构

### BufferWriter 字节缓冲层

`Writer/LogWriter` 的底层使用自管理 `byte[]` 的 `BufferWriter`（[Internal/ArrayBufferWriter.cs](src/Lunarium.Logger/Internal/ArrayBufferWriter.cs)，类名为 `BufferWriter`），直接操作 UTF-8 字节，避免 UTF-16 → UTF-8 的中间转换开销。

#### BufferWriter API

| 方法 | 说明 |
|------|------|
| `Append(string?)` | UTF-8 编码后追加 |
| `Append(char)` | ASCII 走快速路径（单字节写入），非 ASCII 调用 `AppendSpan(stackalloc char[1])` |
| `Append(object?)` | 调用 `ToString()` 后编码 |
| `AppendSpan(ReadOnlySpan<char>)` | char span → UTF-8 编码 |
| `AppendLine()` | 追加 `Environment.NewLine` |
| `AppendFormat(string, object?)` | `string.Format(InvariantCulture, format, arg)` |
| `AppendFormattable<T>` | **零分配**：`IUtf8SpanFormattable` 直接格式化到 UTF-8 字节（`TryFormat` 直接写缓冲区） |
| `RemoveLast(int)` | 移除末尾 N 字节（`_index -= count`） |
| `Remove(int, int)` | 中间移除（`Buffer.BlockCopy` 前移） |
| `FlushTo(Stream)` | **零拷贝**：`stream.Write(_buffer, 0, _index)` |
| `WrittenSpan` | 只读视图，`new ReadOnlySpan<byte>(_buffer, 0, _index)` |
| `Length` | 等价于 `WrittenCount` |
| `this[int]` | 字节索引器（JSON 尾部逗号检查） |

#### 对象池化

[Writer/WriterPool.cs](src/Lunarium.Logger/Writer/WriterPool.cs) 的健康度检查基于 `_bufferWriter.Capacity`；容量阈值 **32KB**（`Utf8JsonWriter` 贪婪申请缓冲，较小阈值会导致 `LogJsonWriter` 永远无法归池）；池上限 **128**；被拒绝入池的对象调用 `DisposeAndReturnArrayBuffer()` 归还底层数组。

---

### IUtf8SpanFormattable 零分配格式化

文本/JSON Writer 的时间戳、数值、Guid 等类型通过 `AppendFormattable<T>` 直接格式化到 UTF-8 字节，跳过 `ToString()` 分配。

#### LogTextWriter / LogColorTextWriter

`WriteTimestamp` 使用 `IUtf8SpanFormattable` 直接写 UTF-8：

```csharp
_bufferWriter.Append('[');
_bufferWriter.AppendFormattable(timestamp, "O");
_bufferWriter.Append("] ");
```

所有数值类型（`long` Unix/UnixMs）均零字符串分配。

#### LogJsonWriter

使用 **`Utf8JsonWriter`**（.NET BCL）构建 JSON 输出：
- **正确性**：自动处理所有 JSON 转义规则，完美支持 **Surrogate Pairs（Emoji）**。
- **高性能**：主 `_jsonWriter` 通过 `Reset(_bufferWriter)` 绑定 BufferWriter 实现池化复用，零分配；`_scratchWriter`（副 BufferWriter）用于先渲染 RenderedMessage 再作为 JSON string 值写入。
- **解构优化**：`IDestructurable` → 调用 `Destructure(DestructureHelper)` 写 JSON 后通过 `WriteRawValue` 嵌入；`IDestructured` → 直接 `WriteRawValue(bytes)`；普通对象 → `JsonSerializer.Serialize(jsonWriter, value)`，消除中间字符串。
- **JSON Level 字段值**：`Info` 级别对应 `"Information"`（对齐 MEL 约定），其余同名。
- **配置**：使用 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`，中文、Emoji 不被过度转义。
- **零分配格式化**：时间戳通过 `stackalloc byte[64]` + `TryFormat` 直接格式化后由 `Utf8JsonWriter.WriteString` 写入，数值类型走 `WriteNumber*` 零分配。
- **独立 TryReset**：`LogJsonWriter` 重写 `TryReset`，同时检查 `_bufferWriter` 和 `_scratchWriter` 容量，超过 32KB 不归池。

---

### Stream 输出层

`LogWriter.FlushTo(Stream)`（[Writer/LogWriter.cs:123](src/Lunarium.Logger/Writer/LogWriter.cs#L123)）直接将 `BufferWriter` 的已写字节写入 `Stream`，零拷贝零分配。

```csharp
internal void FlushTo(Stream stream) => _bufferWriter.FlushTo(stream);
```

`FlushTo(TextWriter)` 为降级路径（[Writer/LogWriter.cs:134](src/Lunarium.Logger/Writer/LogWriter.cs#L134)）：

```csharp
internal void FlushTo(TextWriter output)
{
    ReadOnlySpan<byte> bytes = _bufferWriter.WrittenSpan;
    if (bytes.IsEmpty) return;
    int charCount = Encoding.UTF8.GetCharCount(bytes);
    if (charCount <= 4096)  // 小内容 stackalloc，零堆分配
    {
        Span<char> chars = stackalloc char[charCount];
        Encoding.UTF8.GetChars(bytes, chars);
        output.Write(chars);
    }
    else  // 大内容退化一次 string 分配
    {
        output.Write(Encoding.UTF8.GetString(bytes));
    }
}
```

#### ConsoleTarget

[Target/ConsoleTarget.cs](src/Lunarium.Logger/Target/ConsoleTarget.cs) 构造时缓存 `Console.OpenStandardOutput()` / `Console.OpenStandardError()` 流：

```csharp
private readonly Stream _stdout;
private readonly Stream _stderr;

public ConsoleTarget()
{
    _stdout = Console.OpenStandardOutput();
    _stderr = Console.OpenStandardError();
}
```

`Emit` 调用 `logWriter.FlushTo(stream)` 直接写入字节流。`Dispose` 不执行任何操作（由运行时管理进程句柄）。
测试支持：提供 `internal ConsoleTarget(Stream, Stream)` 构造函数以支持 `MemoryStream` 注入测试。

#### FileTarget

[Target/FileTarget.cs](src/Lunarium.Logger/Target/FileTarget.cs) 使用 `FileStream? _fileStream` 直接写入，无中间 `StreamWriter` 缓冲层：

```csharp
private FileStream? _fileStream;

private void OpenFile(DateTimeOffset timestamp)
{
    _fileStream = new FileStream(
        currentFilePath,
        FileMode.Append,
        FileAccess.Write,
        FileShare.Read | FileShare.Delete,
        bufferSize: 4096,
        FileOptions.None);
}
```

---

### ByteChannelTarget 字节流 Channel

`ByteChannelTarget`（[ChannelTarget.cs:93](src/Lunarium.Logger/Target/ChannelTarget.cs#L93)）将日志格式化为 UTF-8 字节数组写入 Channel：

```csharp
public sealed class ByteChannelTarget : ChannelTarget<byte[]>, IJsonTextTarget
{
    public bool ToJson { get; set; } = false;

    protected override byte[] Transform(LogEntry entry)
    {
        LogWriter logWriter = ToJson
            ? WriterPool.Get<LogJsonWriter>()
            : _isColor ? WriterPool.Get<LogColorTextWriter>() : WriterPool.Get<LogTextWriter>();
        try
        {
            logWriter.Render(entry);
            return logWriter.GetWrittenBytes();  // byte[] 分配（ToArray()），但跳过 UTF-8→string 解码
        }
        finally
        {
            logWriter.Return();
        }
    }
}
```

`LogWriter.GetWrittenBytes()`（[Writer/LogWriter.cs:143](src/Lunarium.Logger/Writer/LogWriter.cs#L143)）返回 `BufferWriter.WrittenSpan.ToArray()`。

**性能对比**：

| Target | 编码步骤 | 堆分配 |
|--------|----------|--------|
| `StringChannelTarget` | UTF-8 字节 → string 解码（`ToString()`） | UTF-8 字节 + UTF-16 字符串 |
| `ByteChannelTarget` | UTF-8 字节 → byte[] 拷贝（`ToArray()`） | UTF-8 字节数组 |
| `LogEntryChannelTarget` | 无编码 | 无 |

`ByteChannelTarget` 适合消费者直接写网络/文件（例如 `HttpClient.SendAsync(bytes)`），`LogEntryChannelTarget` 适合消费者需要完整对象（例如自定义处理逻辑）。

**StringChannelTarget 性能警告**：

`StringChannelTarget` 每次 Emit 必然产生一次 string 堆分配（`Encoding.UTF8.GetString`）。若消费者直接写网络/文件，建议改用 `ByteChannelTarget`。

---

### LogWriter 基类虚方法

`LogWriter` 基类定义以下虚方法，子类可按需重写：

| 虚方法 | 默认行为 |
|--------|----------|
| `RenderPropertyToken(PropertyToken, object?[], int)` | 完整渲染一个属性 token（含 null/解构/IUtf8SpanFormattable/格式化） |
| `GetPropertyValue(object?[], int, out bool)` | 按索引取属性值 |
| `WriteAligned(int, IUtf8SpanFormattable, string?)` | 带对齐的零分配格式化写入 |
| `AppendPadding(int)` | 写 N 个空格（≤32 走 SpacePool 直接 MemoryCopy） |

`LogColorTextWriter` 重写 `RenderPropertyToken` 以加 ANSI 颜色；`LogJsonWriter` 重写所有四个方法以写入 `_scratchWriter` 副缓冲区。

### 自定义解构接口（IDestructurable / IDestructured）

`{@Object}` 解构支持两个高性能接口（优先级高于 `JsonSerializer.Serialize` 回退路径）：

**`IDestructurable`**（最高性能，支持任意 JSON 结构）：
```csharp
public interface IDestructurable {
    void Destructure(DestructureHelper helper);
}
```
`DestructureHelper` 包装 `Utf8JsonWriter`（与 Writer 层共享缓冲区），提供 `WriteStartObject/WriteEndObject/WriteString/WriteNumber/...` 等 API，直接写到最终输出缓冲区，无额外分配。

**`IDestructured`**（极致零分配，适合预序列化场景）：
```csharp
public interface IDestructured {
    ReadOnlyMemory<byte> Destructured();
}
```
返回已编码的 UTF-8 JSON bytes（静态常量、缓存字节块等），通过 `Utf8JsonWriter.WriteRawValue` / `BufferWriter.Append` 直接嵌入输出，零拷贝零分配。

**优先级顺序**（文本 Writer 和 JSON Writer 一致）：
1. `IDestructurable` → 调用 `Destructure(helper)`
2. `IDestructured` → 调用 `Destructured()`
3. 默认回退 → `JsonSerializer.Serialize()`（AOT 下可能降级为 `ToString()`）

---

## 公开 API 边界与扩展点

### API 层次

| 层 | 类型 | 说明 |
|----|------|------|
| 消费层 | `ILogger`、`LogLevel`、`LogUtils` | 业务代码唯一接触面，`Logger` 本身是 `internal sealed` |
| 构建层 | `LoggerBuilder`、`GlobalConfigurator`、`SinkOutputConfig`、`ISinkConfig` | 启动期一次性配置，均有单次锁保护 |
| 扩展层 | `ILogTarget`、`ChannelTarget<T>`、`IJsonTextTarget`、`IColorTextTarget`、`ILogEntryTarget` | 自定义 Sink 的扩展点 |
| 解构层 | `IDestructurable`、`IDestructured`、`DestructureHelper` | 高性能自定义 `{@Object}` 解构扩展点 |
| 数据层 | `LogEntry`、`MessageTemplate`、`PropertyToken`、`TextToken` | 类型公开供自定义 Sink 读取；构造器 `internal`，禁止外部伪造 |
| 集成层 | `LunariumLoggerProvider`、`LunariumLoggerExtensions`、`LunariumLoggerConversionExtensions` | MEL 桥接，非 sealed 可继承重写 `CreateLogger` |

### 自定义 Sink 的三条路径

**路径 A：实现 `ILogTarget`（同步，最底层）**
- 适合内存缓冲、HTTP 推送、数据库写入
- 通过 `builder.AddSink(new MyTarget(), sinkOutputConfig)` 注册

**路径 B：继承 `ChannelTarget<T>`（异步 Channel 桥接）**
- 只需实现 `Transform(LogEntry) → T`，写入 Channel 的并发安全由基类保证
- `DelegateChannelTarget<T>` 是 `internal`，通过 `AddChannelSink<T>(transform)` 扩展方法暴露，收敛入口

**路径 C：实现能力接口（`IJsonTextTarget` / `IColorTextTarget`）**
- `Sink` 构建时自动探测目标是否实现接口，若 `SinkOutputConfig.ToJson?/IsColor?` 非 null 则强制注入
- 自定义 Target 实现这些接口后，可被 `SinkOutputConfig` 统一控制格式

### 关键设计决策

| 决策 | 意图 |
|------|------|
| `Logger` 是 `internal sealed` | 强制接口依赖，防止继承 |
| `Sink` 是 `internal readonly record struct` | 封装过滤+解析+输出三合一，`ProcessQueueAsync` 不感知内部细节 |
| `MessageTemplate` 构造器 `internal` | 类型公开供读取，禁止外部伪造，保证解析缓存有效性 |
| `LogEntry.ParseMessage()` 是 `internal` | 延迟解析由 `Sink.Emit()` 触发，调用方线程不执行解析 |
| `SinkOutputConfig.ToJson?/IsColor?` 三态可空 | `null`=不覆盖目标默认；`true/false`=强制覆盖 |
| `LunariumLoggerProvider.Dispose()` 不销毁 Logger | 谁创建谁管理生命周期，DI 容器不拥有 Logger |
| Writer 层全 `internal` | 输出格式由库统一管控，不允许外部继承扩展渲染逻辑 |
| `GlobalConfigurator` 不可扩展 | 时区/时间戳格式是跨 Sink 的全局行为，需强一致性 |
| `LoggerWrapper` 扁平化 | 构造时解包至根 Logger，避免多层嵌套触发链式 slow path；Context/ContextBytes 一次性拼接 |
| `ILogger.Log()` 含 `contextBytes` 参数 | Writer 层可直接取 UTF-8 bytes 写入 BufferWriter，无需每次编码 |
| `PropertyToken.FormatString` 预构建 | 消除渲染时每次 `BuildFormatString` 的 stackalloc 或堆分配 |
| `TextToken.TextBytes` / `PropertyToken.PropertyNameBytes` 预编码 | 渲染时直接 `Append(bytes)`，跳过 UTF-8 编码 |

### AOT 兼容性

库标记了 `<IsAotCompatible>true</IsAotCompatible>`，build 时即运行 AOT/Trimming 分析器。

**`{@Object}` 解构在 AOT 下的行为**：
- 已注册 `UseJsonTypeInfoResolver`：走 Source Generated 路径，正常序列化
- 未注册且在 AOT 下：`JsonSerializer.Serialize` 抛出后捕获，静默降级为 `ToString()`
- 所有 `JsonSerializer.Serialize` 调用统一经过 `LogWriter.TrySerializeToJson()`（带 `[UnconditionalSuppressMessage]`），build 时零警告

**用户侧配置**：
```csharp
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(User))]
internal partial class MyLogContext : JsonSerializerContext { }

GlobalConfigurator.Configure()
    .UseJsonTypeInfoResolver(MyLogContext.Default)
    .Apply();
```

### 当前扩展点的已知局限

1. **`GlobalConfigurator` 无自定义钩子**：无法注入自定义时间戳格式化策略，只能在自定义 `ILogTarget` 内部自行处理 `LogEntry.Timestamp`。
2. **Writer 层全封闭**：无法继承扩展输出格式，自定义格式须从 `ILogTarget` 层重新实现渲染逻辑。
3. **AOT 下未注册类型静默降级**：`{@Object}` 解构遇到未在 `JsonSerializerContext` 中注册的类型时，输出退化为 `ToString()`，无运行时报错。（实现了 `IDestructurable`/`IDestructured` 的类型不受此限制）
4. **`ILogger.Log()` 的 `contextBytes` 参数**：慢速路径（传入额外即时 context 时）仍会有一次 `string` 拼接和一次 `UTF8.GetBytes` 分配；正常 `ForContext` 使用走零分配快速路径。
