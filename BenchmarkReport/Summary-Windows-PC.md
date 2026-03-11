# Benchmark Environment: Windows 11 PC (i7-7700)

以下数据展示了 `Lunarium.Logger` 在桌面级处理器（Intel Core i7-7700）以及 Windows 11 环境下的微秒级性能开销与分配明细。<br>The following data demonstrates the microsecond-level performance overhead and allocation details of `Lunarium.Logger` running on a desktop-class processor (Intel Core i7-7700) and Windows 11 environment.

---

## 硬件信息 / Hardware Information

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.8037)
Intel(R) Core(TM) i7-7700 CPU @ 3.60GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

---

## FilterBenchmarks-report

**测试目标 / Test Objective**: `LoggerFilter.ShouldEmit(LogEntry, SinkOutputConfig)` 的吞吐量 / Throughput
**架构背景 / Architecture Background**: 每个 `LoggerFilter` 实例持有独立的 2048 条上下文缓存；缓存满时全量清空。使用预热好的缓存来测量真实条件下的极致性能字典读取路径。<br>Each `LoggerFilter` instance holds an independent cache of 2048 contexts; the cache is fully cleared when full. Pre-warmed caches are used to measure the extreme performance of dictionary read paths under real conditions.

| Method                                          | Mean          | Error       | StdDev      | Median        | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|------------:|------------:|--------------:|---------:|--------:|-------:|----------:|------------:|
| 无规则，级别通过 (缓存命中) / No rules, level passed (Cache hit)                               |      9.301 ns |   0.2207 ns |   0.5246 ns |      9.228 ns |     1.00 |    0.08 |      - |         - |          NA |
| Include 规则，context 匹配通过 (缓存命中) / Include rules, context matched and passed (Cache hit)                |      8.650 ns |   0.2088 ns |   0.3311 ns |      8.488 ns |     0.93 |    0.06 |      - |         - |          NA |
| Include 规则，context 不匹配被拒绝 (缓存命中) / Include rules, context mismatched and rejected (Cache hit)              |      8.908 ns |   0.2070 ns |   0.3223 ns |      8.844 ns |     0.96 |    0.06 |      - |         - |          NA |
| Exclude 规则，context 不在排除列表通过 (缓存命中) / Exclude rules, context not in exclude list and passed (Cache hit)            |      8.642 ns |   0.1989 ns |   0.2977 ns |      8.484 ns |     0.93 |    0.06 |      - |         - |          NA |
| Exclude 规则，context 命中排除列表被拒绝 (缓存命中) / Exclude rules, context hit exclude list and rejected (Cache hit)           |      8.459 ns |   0.1953 ns |   0.1827 ns |      8.413 ns |     0.91 |    0.05 |      - |         - |          NA |
| 无规则，缓存未命中 (超时清空) / No rules, cache miss (Approx. 3k unique contexts, cleared after 2048) | 14,453.402 ns | 282.6941 ns | 325.5510 ns | 14,402.010 ns | 1,558.71 |   91.78 | 0.0305 |     189 B |          NA |

---

## LoggerThroughputBenchmarks-report

**测试目标 / Test Objective**: 调用方线程的 `Log()` 调用吞吐量 (Channel `TryWrite` 性能) / Throughput of `Log()` calls from the caller thread (Channel `TryWrite` performance)
**架构背景 / Architecture Background**: `Logger.Log()` 是同步方法，此测试用 `NullTarget` 隔离真实的后台 I/O 写入消耗，精准测量创建一条日志（包装属性参数、封箱拆箱代价）并成功抛入管道所需的时间。<br>`Logger.Log()` is a synchronous method. This test uses `NullTarget` to isolate actual background I/O writing overhead, accurately measuring the time required to create a log entry (boxing/unboxing costs, wrapping properties) and successfully pushing it into the channel.

| Method                                          | Mean        | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|----------:|----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| Log(): 无属性，纯文本消息 / No properties, plain text message                              |    125.2 ns |   2.15 ns |   2.01 ns |   1.00 |    0.02 | 0.0212 | 0.0002 |      89 B |        1.00 |
| Log(): 单属性 / Single property (params object?[1] alloc)             |    133.2 ns |   1.55 ns |   1.37 ns |   1.06 |    0.02 | 0.0288 |      - |     121 B |        1.36 |
| Log(): 三属性 / Three properties (params object?[3] alloc)             |    139.2 ns |   2.27 ns |   2.23 ns |   1.11 |    0.02 | 0.0384 | 0.0007 |     162 B |        1.82 |
| Log(): 五属性 / Five properties (params object?[5] alloc)             |    148.0 ns |   2.96 ns |   3.40 ns |   1.18 |    0.03 | 0.0482 | 0.0005 |     202 B |        2.27 |
| Log(): 通过 ForContext / Via ForContext wrapper (LoggerWrapper) |    125.7 ns |   2.49 ns |   2.20 ns |   1.00 |    0.02 | 0.0288 | 0.0002 |     121 B |        1.36 |
| Log(): 批量 100 条 / Batch of 100 (Amortized cost per item)                 | 13,854.6 ns | 141.09 ns | 131.98 ns | 110.73 |    2.00 | 3.4027 | 0.1984 |   14522 B |      163.17 |

---

## LogParserBenchmarks-report

**测试目标 / Test Objective**: 模板渲染引擎 `LogParser.ParseMessage(string)` 的吞吐量 / Throughput of the template rendering engine `LogParser.ParseMessage(string)`
**架构背景 / Architecture Background**: Parser 内置 4096 条 `ConcurrentDictionary` 缓存。在正常基准线测试中均发生完美缓存命中，主要评估对特殊结构如对齐、解构修饰符 `{@}`等词法标记的极限解析与组合性能。<br>The parser has a built-in 4096-item `ConcurrentDictionary` cache. Perfect cache hits occur in standard baseline tests, primarily evaluating the extreme parsing and composition performance for special structures like alignment, destructuring modifiers `{@}`, and other lexical tokens.

| Method                                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|-------:|----------:|------------:|
| 纯文本，无占位符 (命中) / Plain text, no placeholders (Cache hit)                       |     14.99 ns |   0.359 ns |   0.580 ns |     1.00 |    0.05 |      - |      - |      - |         - |          NA |
| 单属性模板 (命中) / Single property template (Cache hit)                          |     11.40 ns |   0.293 ns |   0.371 ns |     0.76 |    0.04 |      - |      - |      - |         - |          NA |
| 三属性模板 (命中) / Three properties template (Cache hit)                          |     18.42 ns |   0.427 ns |   0.927 ns |     1.23 |    0.08 |      - |      - |      - |         - |          NA |
| 复杂模板 (命中) / Complex template: Align+Format+Destructure (Hit)           |     20.56 ns |   0.472 ns |   0.838 ns |     1.37 |    0.08 |      - |      - |      - |         - |          NA |
| 含转义 {{ }} 的模板 (命中) / Template with escaped {{ }} (Hit)                  |     14.05 ns |   0.342 ns |   0.320 ns |     0.94 |    0.04 |      - |      - |      - |         - |          NA |
| 缓存未命中 / Cache miss (Approx. 6k strings, cleared after 4096) | 15,322.49 ns | 299.344 ns | 367.622 ns | 1,023.64 |   45.53 | 0.1678 | 0.0763 | 0.0305 |    1074 B |          NA |

---

## LogWriterBenchmarks-report

**测试目标 / Test Objective**: 格式化输出引擎的性能 (`LogWriter.Render`) 以及 `WriterPool` 对象池收益 / Performance of the formatted output engine (`LogWriter.Render`) and `WriterPool` object pool benefits
**架构背景 / Architecture Background**: 针对纯文本 (Text)、带 ANSI 颜色控制码 (Color) 以及 JSON 格式化的渲染损耗对比。对比直接分配 `new LogTextWriter()` 与使用池化 `WriterPool.Get/Return` 的性能与内存分配 (GC) 收益。<br>Compares rendering overhead across plain text (Text), text with ANSI color codes (Color), and JSON formatting. It also compares the performance and memory allocation (GC) benefits of directly allocating `new LogTextWriter()` versus using the pooled `WriterPool.Get/Return`.

| Method                                                   | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Text: 纯文本消息 / Plain text message (No properties)                                       | 312.83 ns |  5.786 ns |  6.191 ns | 311.01 ns |  1.00 |    0.03 | 0.0267 |     112 B |        1.00 |
| Text: 单属性 / Single property                                              | 379.28 ns |  7.602 ns | 11.609 ns | 372.93 ns |  1.21 |    0.04 | 0.0343 |     144 B |        1.29 |
| Text: 四属性 / Four properties                                              | 528.72 ns | 10.603 ns | 19.389 ns | 517.78 ns |  1.69 |    0.07 | 0.0572 |     240 B |        2.14 |
| Text: 对齐 + 格式化 / Alignment + Formatting ({Count,8:D} {Percent:P1})              | 600.71 ns | 11.913 ns | 19.904 ns | 591.67 ns |  1.92 |    0.07 | 0.0591 |     248 B |        2.21 |
| Color: 单属性 / Single property (With ANSI color escape codes)                              | 435.53 ns |  8.727 ns | 12.234 ns | 431.92 ns |  1.39 |    0.05 | 0.0343 |     144 B |        1.29 |
| Color: 四属性 / Four properties                                             | 628.98 ns | 12.530 ns | 19.508 ns | 627.70 ns |  2.01 |    0.07 | 0.0572 |     240 B |        2.14 |
| JSON: 单属性 / Single property (With RenderedMessage + Properties fields)            | 510.44 ns | 10.211 ns | 15.897 ns | 507.24 ns |  1.63 |    0.06 | 0.0362 |     152 B |        1.36 |
| JSON: 四属性 / Four properties                                              | 975.17 ns | 19.241 ns | 36.608 ns | 966.14 ns |  3.12 |    0.13 | 0.0858 |     360 B |        3.21 |
| Pool: 池化路径 / Pool path (WriterPool.Get&lt;LogTextWriter&gt;() + Return()) |  77.15 ns |  1.565 ns |  1.675 ns |  76.61 ns |  0.25 |    0.01 |      - |         - |        0.00 |
| Alloc: 直接分配 / Direct allocation (new LogTextWriter())              |  16.11 ns |  0.488 ns |  1.431 ns |  15.40 ns |  0.05 |    0.00 | 0.0325 |     136 B |        1.21 |

---

# Legends

- **Mean**        : Arithmetic mean of all measurements
- **Error**       : Half of 99.9% confidence interval
- **StdDev**      : Standard deviation of all measurements
- **Ratio**       : Mean of the ratio distribution ([Current]/[Baseline])
- **Gen0**        : GC Generation 0 collects per 1000 operations
- **Allocated**   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
- **Alloc Ratio** : Allocated memory ratio distribution ([Current]/[Baseline])
- **1 ns**        : 1 Nanosecond (0.000000001 sec)