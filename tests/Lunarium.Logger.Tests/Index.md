# Lunarium.Logger.Tests 代码索引

此索引专为 AI 和开发者提供，记录了各个测试文件所覆盖的 **目标类 (Target Class)** 和详细的 **测试场景 (Test Cases)**。
AI 在修改核心库的代码时，可通过搜索此文档快速定位受影响的测试类和方法，避免全局扫描。

## Config/(配置相关测试)
### GlobalConfiguratorTests.cs
- **目标类**: `GlobalConfigurator`, `LogTimestampConfig`, `TimestampFormatConfig`, `DestructuringConfig`, `JsonSerializationConfig`
- **主要测试场景**:
  - 重复配置的防御：`Configure_WhenAlreadyConfigured_Throws` 等。
  - 时间与时区：`UseUtcTimeZone`, `UseLocalTimeZone`, `UseCustomTimezone` 设置流生效逻辑。
  - 格式与缩进：`UseJsonUnixTimestamp`, `UseTextISO8601Timestamp` 等枚举映射验证。
  - 特殊选项：`EnableAutoDestructuring` 开启解构，及 JSON 序列化的 `PreserveChineseCharacters`, `UseCompactJson` 生效机制。
  - AOT 支持：`UseJsonTypeInfoResolver_WithNull*_ThrowsArgumentNullException`（两个重载各一条，防御空参数）；`UseJsonTypeInfoResolver_With*_IsAddedToOptionsChain`（验证注册的 Resolver 出现在 `TypeInfoResolverChain` 中）。

### SinkOutputConfigTests.cs
- **目标类**: `SinkOutputConfig`
- **主要测试场景**:
  - 初始化与映射：验证初始化时 `Min/Max Level`、`ToJson` 等控制开关的默认值。
  - 校验开关驱动 `StringComparison` 的逻辑映射关系 (`IgnoreFilterCase` -> `OrdinalIgnoreCase`)。

### GlobalConfigExtensionsTests.cs
- **目标类**: `AtomicOpsGlobalConfigExtensions`, `SafetyClearGlobalConfigExtensions`
- **主要测试场景**:
  - 验证配置扩展方法（`.EnableBufferWriterInterlocked()`, `.EnableSafetyClear()` 等）能否正确修改全局静态配置标志。
  - 验证方法链路的可重复性（Enable 后 Disable 状态正确）。

---

## Core/(核心日志派发与工具)
### LoggerCoreTests.cs
- **目标类**: `Logger`, `LoggerBuilder`
- **主要测试场景**:
  - 分发逻辑：调用 `Log` 时能正确组装并将其派发至各个 `ILogTarget` (基于 Channel)。
  - 生命周期容错：`Logger_AfterDispose_LogIsDropped` 验证 `DisposeAsync` 后的调用能够被安全丢弃而不抛出异常。
  - 资源验证：验证对同一路径创建两个 FileTarget 时，第二次构造即抛出异常 (`FileTarget_DuplicateFilePath_ThrowsInvalidOperation`)；验证 Dispose 后同路径可成功重建 (`FileTarget_SamePath_AfterDispose_CanReuseSuccessfully`)。
  - 多实例支持：验证 `LoggerBuilder.Build()` 可多次调用，返回互相独立、均可正常分发日志的 Logger 实例 (`LoggerBuilder_Build_CanBuildMultipleInstances`)。

### InternalLoggerTests.cs
- **目标类**: `InternalLogger`
- **主要测试场景**:
  - 内部故障隔离：确保内部通过所有重载写入的异常都能无缝安全地沉降记录入根目录下的 `LunariumLogger-internal-yyyyMMdd.log` 中。

### LogUtilsTests.cs
- **目标类**: `LogUtils`
- **主要测试场景**:
  - 时间偏移与降级：针对全局设定生成时间偏移量的回传测试，以及系统遇到不存在的 `TextMode` 枚举强制执行 `ISO8601` 防线回退机制的验证。

### LoggerCoverageGapTests.cs
- **目标类**: `Logger` (异步消费与多重 Try-Catch 防御覆盖)
- **主要测试场景**:
  - 健壮性保障：验证后台挂起的 `ProcessQueueAsync` 死循环中如果某一个 Sink 发生写入抛错，引擎可以吸纳它，不至于中断后续日志队列的循环消耗。
  - 验证即便底层释放资源的 `Sink.Dispose` 抛出严重故障也不会使父级系统的生命周期中止抛错。

---

## Extensions/(微软原生扩展包接管适配)
### MicrosoftLoggingBridgeTests.cs
- **目标类**: `LunariumLoggerProvider`, `LunariumMsLoggerAdapter`, `LunariumLoggerConversionExtensions`
- **主要测试场景**:
  - 级别平译：测试 `.NET` 专属的 `Trace/Warning` 无损平移为本库自身的级别映射。
  - Context & Scope 支撑：对接 MEL 系统中下发的 `EventId` 及子系统名至我们的 `ForContext` 范畴并完成桥接。
  - 抛出及扩展：验证从微软库传入 `Exception` 的完整度转换、使用扩展的生成性校验、和 `BeginScope` 生命期流转均得到正常接应。

---

## Filter/(投递黑白名单校验)
### LoggerFilterTests.cs
- **目标类**: `LoggerFilter`
- **主要测试场景**:
  - 级别校验：检验低于最低阈值或高于最高阈值的门槛控制。
  - 黑白名单重合校验：检验基于前缀匹配 (如 `App.*`) 的 `Include` 与 `Exclude` 的集合交锋 (以拦截排除优先)。
  - 高并发与内部 LRU 高速缓存挤出重建：验证超过 2048 名单时 `LRU Clear` 执行后新的判断仍正确运作且表现一致。

---

## Models/(配置结构参数模型)
### LogConfigTests.cs
- **目标类**: `Sink`, `ConsoleSinkConfig`, `FileSinkConfig`
- **主要测试场景**:
  - 透传校验：当 `ConsoleSinkConfig` 含有显式 `SinkOutputConfig` 下 `ToJson` 或 `IsColor` 参数时，确认装箱过程中对应的 `IJsonTextTarget` 或 `IColorTextTarget` 实例特性能否被同步向下生效和重写。
  - 工厂方法：`ConsoleSinkConfig.CreateTarget()` 返回 `ConsoleTarget`；`FileSinkConfig.CreateTarget()` 返回正确参数的 `FileTarget`。

---

## Parser/(核心字符串插值解析模板器)
### LogParserTests.cs
- **目标类**: `LogParser`, `MessageTemplate`, `TextToken`, `PropertyToken`
- **注意**: `Tokens()` 辅助方法返回 `IReadOnlyList<MessageTemplateTokens>`，对应 `MessageTemplate.MessageTemplateTokens` 字段（`internal readonly IReadOnlyList<MessageTemplateTokens>`）。
- **主要测试场景**:
  - 特殊符转义：`{{` -> `{` 及 `{{}}` 并无属性解包意义被视作纯文本。
  - 取值回退防御：解析失误如 `{A.}`、`{V,abc:}` 被自动降层认定并切片为普通文本 (`TextToken`)，而非报错。
  - 限定修饰词控制与分隔：测试前置运算符（`@`：解构输出 / `$`：强转字符串）以及长度格式化器包含对齐支持及 `Format` 透传。
  - 同 `Filter` 一致测试内部 `ConcurrentDictionary` 高速模式匹配映射与相同的结构内存互通复用校验。

---

## Wrapper/(装饰器叠加级联树)
### LoggerWrapperTests.cs
- **目标类**: `LoggerWrapper`
- **主要测试场景**:
  - 全局子系统隔离合并：证明多次调用 `.ForContext("Service").ForContext("Auth")` 能正确以 `Service.Auth` 合并传递。
  - 参数装满与无损透传：确保底部的原始消息、数组甚至抛出的 `Exception` 完整下发至实际执行器。

---

## Writer/(输出流直接渲染器引擎)
### LogColorTextWriterTests.cs
- **目标类**: `LogColorTextWriter`
- **主要测试场景**:
  - 格式及转义码拼接：验证能否插入针对等级的颜色区别（如 Info 绿, Error 红）及对数值的显色识别策略。
  - 极端过长处理：检验遇到超出预构建 `span` (长度>96) 的自定义过长格式器被抛至堆申请内存。

### LogJsonWriterTests.cs
- **目标类**: `LogJsonWriter`
- **主要测试场景**:
  - 契约执行完整性：使用 `Utf8JsonWriter` 重新实现，验证生成的 JSON 文档结构的准确性。
  - 特殊类型序列化：转换双精度 NaN/Infinity 以避免 JSON 抛错；验证基于 `JsonSerializer` 的 `@Obj` 属性级联解构。


### LogTextWriterTests.cs
- **目标类**: `LogTextWriter`
- **主要测试场景**:
  - 验证纯白板状态的基础文字插槽。验证含有对齐规则输出长度空位补偿的表现效果。

### LogWriterTests.cs
- **目标类**: `LogTextWriter`, `LogColorTextWriter`, `LogJsonWriter`, `LogWriter` (基类), `StringChannelTarget`
- **主要测试场景**:
  - 综合渲染管线：针对时间戳（Unix、ISO8601、Custom）在多种 Writer 下的格式化输出校验。
  - 基础缓冲池逻辑校验：验证 `TryReset` 对容量大小超限(`GetBufferCapacity` > 4KB)对象的拦截与归还控制机制。
  - 目标器分发校验：验证 `StringChannelTarget.Emit` 在不同开关 (`ToJson`, `IsColor`) 下正确选择对应格式分发链路。

### WriterCoverageGapTests.cs
- **主要测试场景**:
  - 异常强吃：验证调用内置格式器遭遇未知极端故障引发抛错（如非标的格式化类型）时依然进行文本保底，防止队列直接崩坏丢失。
  - 编码方案验证：验证 **Surrogate Pairs (Emoji)** 经过 `Utf8JsonWriter` 后的 JSON 转义表现，以及中文字符在 `UnsafeRelaxedJsonEscaping` 模式下的不转义表现。


### WriterPoolTests.cs
- **目标类**: `WriterPool`
- **主要测试场景**:
  - 池化循环与遗忘：验证从并发囊获取并归还后，是否确保每次使用都被重置抛除历史写入。
  - 资源过大主动清退：检验当内载缓冲数组极大超出定义后，强制进入垃圾回收机制而杜绝送还引发的内存泄露。

## Internal/(底层实现与缓冲区)
### BufferWriterTests.cs
- **目标类**: `BufferWriter`
- **主要测试场景**:
  - 缓冲区操作：`Remove`、`RemoveLast` 处理逻辑验证。
  - 边界与扩容：`GetSpan` 触发的数组自动扩容。
  - 编码路径：`Append(char)` 处理 Emoji 或中文等非 ASCII 字符的正确性。
  - 属性盖全：`Length`, `WrittenSpan`, `this[index]` 的正确性检查。
  - 生命周期：`Dispose` 在开启 `Interlocked` 模式下的行为及重复 Dispose 的安全性。

## Target/(数据转发目标)
### ChannelTargetTests.cs
- **目标类**: `ByteChannelTarget`, `StringChannelTarget`
- **主要测试场景**:
  - 字节流转换：验证 `ByteChannelTarget` 将日志转换为 `byte[]` 并正确送入 Channel。
  - 链路完整性：验证 ChannelTarget 在 JSON 模式下的整体输出行为。

---

## Integration/ 及 Root级 (集成环境与组件连接)
### LoggerIntegrationTests.cs
- **目标类**: 包含多 Channel 并发和 `IntegrationCollection`。
- **注意**: fixture 中 `ConcreteLogger` 字段类型为 `ILogger`（非 `Logger` 具体类）。
- **主要测试场景**:
  - 使用 xUnit 集成集合特性共享单例锁验证复合限定逻辑。多个具有等级、名称排他的通道并走验证。
  - `LogEntryChannelTarget` 传递自身；`DelegateChannelTarget<T>` 单一函数包装转发效果的确认。

### ILoggerDefaultMethodTests.cs
- **主要测试场景**: 接口维度上所有短命名如 `Info()`, `Warning()`, `Error()` 及其重载均能正常跳转通向底层默认 `Log` 方法签名的测定。

### LoggerExtensionsTests.cs
- **主要测试场景**: 构建流水线上流式链条连接检验。例如：`AddRotatingFileSink()` 成功把对应参数组配化为具体的底层 `FileTarget`，并插入流表不至断接。
  - `AddSink_CustomISinkConfig_UsesCreateTarget`：验证第三方 `ISinkConfig` 实现通过 `CreateTarget()` 可直接走统一注册路径，不再抛出异常。

### LogUtilsTests.cs (根目录)
- **目标类**: `LogUtils`
- **主要测试场景**: 
  - 检验公共时间戳 API 在不同的 `TextTimestampMode` (Unix, UnixMs, ISO8601, Custom) 设置下返回特定格式的文本转换逻辑与正确性。

### LoggerConcurrencyTests.cs
- **目标类**: `Logger`, `LogWriter`, `WriterPool`, `BufferWriter`（多线程安全性）
- **主要测试场景**:
  - 并发调用：多线程并发 `.Log()` 无异常抛出。
  - 数据完整性：并发调用后验证日志条目数量正确，无丢失/重复。
  - ArrayPool 安全性（DEBUG 模式）：验证 `BufferWriter` 的 `ArrayPool.Return` 没有被调用超过一次。

### MicrosoftLoggingBridgeTests.cs (根目录)
- **目标类**: `LunariumLoggerProvider`, `LunariumMsLoggerAdapter`, `LunariumLoggerExtensions`
- **主要测试场景**:
  - 核心桥接验证：测试适配器生命周期、原生的 `BeginScope` 生命期流转及带有 `EventId` 的日志如何融入本库上下文。
  - 扩展层注入校验：重点测定 `AddLunariumLogger` 扩展在标准 `ILoggingBuilder` 托管管线中的成功挂载与运行。