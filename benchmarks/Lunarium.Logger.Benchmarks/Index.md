# Lunarium.Logger.Benchmarks 代码索引

此索引专为 AI 和开发者提供，记录了各个 Benchmark 文件所覆盖的 **目标类 (Target Class)** 和详细的 **测试场景 (Benchmark Cases)**。
修改核心库热路径时，可通过搜索此文档快速定位受影响的 Benchmark，评估性能回归风险。

**运行方式**：
```bash
# 交互式菜单选择
dotnet run -c Release --project benchmarks/Lunarium.Logger.Benchmarks

# 按类名过滤运行
dotnet run -c Release --project benchmarks/Lunarium.Logger.Benchmarks -- --filter "*LogParser*"
```

> ⚠️ 必须以 Release 模式运行，Debug 模式结果无意义。

---

## 公共基础设施

### BenchmarkHelper.cs
- **目标类**: `NullTarget`（`ILogTarget` 空实现）、`GlobalConfigurator`
- **用途**:
  - `NullTarget.Emit()` 为空方法，用于在所有吞吐量测试中隔离 I/O 噪声，只测量 Channel 写入与管道分发开销。
  - `BenchmarkHelper.EnsureGlobalConfig()` 内部调用 `GlobalConfigurator.ApplyDefaultIfNotConfigured()`，确保全局时区、时间戳格式等静态配置在进程首次 Benchmark 前完成初始化（进程级单次执行，所有 Benchmark 类共享）。

---

## LogParserBenchmarks.cs

- **目标类**: `LogParser`（`Lunarium.Logger.Parser`，internal）
- **测量目标**: `LogParser.ParseMessage(string)` 的吞吐量
- **架构背景**: 解析器内置 4096 条 `ConcurrentDictionary` 缓存；缓存满时全量清空（非 LRU 淘汰）。
- **主要 Benchmark 场景**:
  - `PlainText_CacheHit`：纯文本模板（无占位符），走 `IndexOfAny` 快速路径后命中缓存，代表最快情形的基准线。
  - `SingleProperty_CacheHit`：单属性 `"User {Name} logged in"`，最常见模板形态的缓存命中开销。
  - `ThreeProperty_CacheHit`：三属性模板，验证 Token 数量增加后缓存命中的影响（理论上命中后开销与属性数量无关）。
  - `ComplexTemplate_CacheHit`：含对齐 `{Count,8:D}`、格式化 `{Percent:P1}`、解构前缀 `{@Worker}` 的复合模板缓存命中，验证复杂性不影响缓存命中路径。
  - `EscapedBraces_CacheHit`：含 `{{ }}` 转义的模板缓存命中，验证转义分支不增加命中后的开销。
  - `CacheMiss_Approx`：使用 6000 个预生成唯一字符串池循环调用（超出缓存上限 4096 后触发清空重建），近似模拟持续缓存未命中场景，测量状态机解析 + 字典写入的完整开销。

---

## LogWriterBenchmarks.cs

- **目标类**: `LogTextWriter`、`LogColorTextWriter`、`LogJsonWriter`（均为 `Lunarium.Logger.Writer`，internal）、`WriterPool`（internal static）
- **测量目标**: `LogWriter.Render(LogEntry)` 渲染管线吞吐量 + `WriterPool` 对象池收益
- **架构背景**: 三种 Writer 均通过 `WriterPool.Get<T>()` 取出、`writer.Return()` 归还；内部使用 `BufferWriter`（基于 `ArrayPool<byte>`）构建 UTF-8 输出，写入 `Stream.Null` 排除 I/O 干扰。`GlobalSetup` 中预构建含解析完毕模板的 `LogEntry`，排除解析器开销干扰。
- **主要 Benchmark 场景**:

  **LogTextWriter（纯文本格式）**
  - `Text_PlainText`（基准线）：无属性消息渲染，代表最小开销路径。
  - `Text_SingleProperty`：单属性渲染，测量 `AppendFormat` 与属性值查找的基础开销。
  - `Text_MultiProperty`：四属性渲染，观察属性数量线性增长对渲染时间的影响。
  - `Text_AlignmentAndFormat`：含对齐 `{Count,8:D}` 与格式化 `{Percent:P1}`，验证 `BuildFormatString` 的 `stackalloc` 路径开销。

  **LogColorTextWriter（带 ANSI 颜色的文本格式）**
  - `Color_SingleProperty`：单属性渲染，相比 `Text_SingleProperty` 测量 ANSI 转义码插入的额外开销。
  - `Color_MultiProperty`：四属性渲染，多属性场景下颜色代码拼接的综合开销。

  **LogJsonWriter（JSON 格式）**
  - `Json_SingleProperty`：单属性 JSON 渲染，基于 `Utf8JsonWriter` 测量核心序列化开销。
  - `Json_MultiProperty`：四属性 JSON 渲染，验证 `Utf8JsonWriter` 的多属性拼接效率。

  **WriterPool 对象池收益**
  - `Pool_GetAndReturn`：`WriterPool.Get<LogTextWriter>()` + `WriterPool.Return()` 的往返开销（池化路径），测量 `ConcurrentBag.TryTake` 与 `Add` 的代价。
  - `Alloc_NewWriter`：`new LogTextWriter()` 直接分配（不归还），通过 `[MemoryDiagnoser]` 的 `Allocated` 列量化池化节省的堆分配。

---

## FilterBenchmarks.cs

- **目标类**: `LoggerFilter`（`Lunarium.Logger.Filter`，internal）
- **测量目标**: `LoggerFilter.ShouldEmit(LogEntry, SinkOutputConfig)` 的吞吐量
- **架构背景**: 每个 `LoggerFilter` 实例持有独立的 2048 条 `ConcurrentDictionary` 上下文缓存（实例级，非静态）；缓存满时全量清空。每个 Benchmark 方法使用独立的 `LoggerFilter` 实例，缓存状态互不干扰。`GlobalSetup` 中预热缓存，确保缓存命中测试直接走字典读路径。
- **主要 Benchmark 场景**:

  **缓存命中路径**
  - `NoRules_Pass_CacheHit`（基准线）：无 Include/Exclude 规则，仅做级别检查后命中缓存，代表最快过滤路径。
  - `WithIncludes_Pass_CacheHit`：配置 5 条 Include 前缀规则，context 匹配通过的缓存命中开销。
  - `WithIncludes_Reject_CacheHit`：context 不匹配 Include 列表被拒绝，缓存命中路径（命中结果为 false）。
  - `WithExcludes_Pass_CacheHit`：配置 3 条 Exclude 前缀规则，context 不在排除列表通过的缓存命中。
  - `WithExcludes_Reject_CacheHit`：context 命中 Exclude 列表被拒绝，缓存命中路径。

  **缓存未命中（近似）**
  - `NoRules_CacheMiss_Approx`：使用 3000 个预生成唯一 context 字符串池（超出缓存上限 2048 后触发清空），近似模拟缓存未命中场景，测量前缀匹配计算 + 字典写入开销。`LogEntry` 已在 `static` 构造器中预生成，排除构造开销干扰。

---

## ConfigPerformanceBenchmarks.cs

- **目标类**: `SafetyClearConfig`, `AtomicOpsConfig`, `BufferWriter`, `WriterPool`
- **测量目标**: 配置项对性能的影响
- **架构背景**: `SafetyClear` 影响 Reset/Dispose 时的 `Array.Clear` 调用；`BufferWriterInterlocked` 影响 Dispose 时的 `Interlocked.Exchange` 调用。由于这些是静态全局配置，Benchmark 之间共享配置状态。
- **主要 Benchmark 场景**:
  - `FullPipeline_Default`（基准线）：完整渲染管道，使用默认配置。
  - `Reset_IndexOnly`：仅重置索引，模拟 `SafetyClear=false`。
  - `Reset_WithArrayClear`：含 `Array.Clear` 调用，模拟 `SafetyClear=true` 的开销。
  - `Dispose_WithoutInterlocked`：直接 Dispose，模拟 `BufferWriterInterlocked=false`。
  - `Dispose_WithInterlocked`：含 `Interlocked.Exchange`，模拟 `BufferWriterInterlocked=true`（双重保险）。
  - `Pool_GetAndReturn`：池化周期（Get + Return），对比无池化场景。
  - `Alloc_NewAndDispose`：无池化（new + Dispose），量化池化收益。

---

## LoggerThroughputBenchmarks.cs

- **目标类**: `Logger`（internal，通过 `ILogger` 接口操作）、`LoggerBuilder`、`LoggerWrapper`（`ForContext` 返回值）
- **测量目标**: 调用方线程的 `Log()` 调用开销（Channel TryWrite 吞吐量）
- **架构背景**: `Logger.Log()` 是同步方法，内部构造 `LogEntry` 后调用 `Channel<LogEntry>.Writer.TryWrite()` 即返回；实际的过滤/解析/渲染/输出在后台 `ProcessQueueAsync` 中异步执行。本 Benchmark 测量的是调用方线程感知到的开销，与后台处理速度无关。所有测试使用 `NullTarget`，`GlobalCleanup` 中通过 `IAsyncDisposable.DisposeAsync()` 等待队列排空后释放资源。
- **主要 Benchmark 场景**:
  - `Log_PlainMessage`（基准线）：无属性纯文本消息，`params object?[]` 传入空数组，测量 `LogEntry` 构造 + `TryWrite` 的最小开销。
  - `Log_OneProperty`：单属性调用，`params object?[1]` 隐式数组分配，通过 `[MemoryDiagnoser]` 观察该分配量。
  - `Log_ThreeProperties`：三属性调用，`params object?[3]` 数组分配，与单属性对比验证参数数量对 `Allocated` 的影响。
  - `Log_FiveProperties`：五属性调用，参数数组分配量最大的常见场景。
  - `Log_ViaForContext`：通过 `ForContext` 返回的 `LoggerWrapper` 调用，测量 `LoggerWrapper` 相比直接调用 `Logger` 的额外间接层开销（预期极小）。
  - `Log_Batch100`：循环调用 100 次，分摊开销后反映批量写入的平均每条成本，更接近高吞吐生产场景。
