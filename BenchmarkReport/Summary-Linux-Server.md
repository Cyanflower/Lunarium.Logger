# Benchmark Environment: Linux Server 

以下数据展示了 `Lunarium.Logger` 在 AMD EPYC 7542 云服务器等级多核处理器以及 Ubuntu 24.04 (Noble Numbat) 环境下的微秒级性能开销与分配明细。<br>The following data demonstrates the microsecond-level performance overhead and allocation details of `Lunarium.Logger` running on a cloud server-grade multi-core processor (AMD EPYC 7542) and Ubuntu 24.04 (Noble Numbat) environment.

---

## 硬件信息 / Hardware Information

```
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7542, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

---

## FilterBenchmarks-report

**测试目标 / Test Objective**: `LoggerFilter.ShouldEmit(LogEntry, SinkOutputConfig)` 的吞吐量 / Throughput
**架构背景 / Architecture Background**: 每个 `LoggerFilter` 实例持有独立的 2048 条上下文缓存；缓存满时全量清空。使用预热好的缓存来测量真实条件下的极致性能字典读取路径。<br>Each `LoggerFilter` instance holds an independent cache of 2048 contexts; the cache is fully cleared when full. Pre-warmed caches are used to measure the extreme performance of dictionary read paths under real conditions.

| Method                                          | Mean          | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|-----------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| 无规则，级别通过 (缓存命中) / No rules, level passed (Cache hit)                               |      9.361 ns |  0.0287 ns |  0.0254 ns |     1.00 |    0.00 |      - |         - |          NA |
| Include 规则，context 匹配通过 (缓存命中) / Include rules, context matched and passed (Cache hit)                |      9.479 ns |  0.0181 ns |  0.0151 ns |     1.01 |    0.00 |      - |         - |          NA |
| Include 规则，context 不匹配被拒绝 (缓存命中) / Include rules, context mismatched and rejected (Cache hit)              |      9.702 ns |  0.0126 ns |  0.0105 ns |     1.04 |    0.00 |      - |         - |          NA |
| Exclude 规则，context 不在排除列表通过 (缓存命中) / Exclude rules, context not in exclude list and passed (Cache hit)            |      9.369 ns |  0.0537 ns |  0.0502 ns |     1.00 |    0.01 |      - |         - |          NA |
| Exclude 规则，context 命中排除列表被拒绝 (缓存命中) / Exclude rules, context hit exclude list and rejected (Cache hit)           |      9.390 ns |  0.0280 ns |  0.0249 ns |     1.00 |    0.00 |      - |         - |          NA |
| 无规则，缓存未命中 (超时清空) / No rules, cache miss (Approx. 3k unique contexts, cleared after 2048) | 13,616.814 ns | 18.8979 ns | 17.6771 ns | 1,454.58 |    4.23 | 0.0153 |     194 B |          NA |

---

## LoggerThroughputBenchmarks-report

**测试目标 / Test Objective**: 调用方线程的 `Log()` 调用吞吐量 (Channel `TryWrite` 性能) / Throughput of `Log()` calls from the caller thread (Channel `TryWrite` performance)
**架构背景 / Architecture Background**: `Logger.Log()` 是同步方法，此测试用 `NullTarget` 隔离真实的后台 I/O 写入消耗，精准测量创建一条日志（包装属性参数、封箱拆箱代价）并成功抛入管道所需的时间。<br>`Logger.Log()` is a synchronous method. This test uses `NullTarget` to isolate actual background I/O writing overhead, accurately measuring the time required to create a log entry (boxing/unboxing costs, wrapping properties) and successfully pushing it into the channel.

| Method                                          | Mean        | Error       | StdDev      | Median      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Log(): 无属性，纯文本消息 / No properties, plain text message                              |    531.4 ns |    12.67 ns |    37.35 ns |    533.9 ns |   1.01 |    0.10 | 0.0105 |      90 B |        1.00 |
| Log(): 单属性 / Single property (params object?[1] alloc)             |    558.4 ns |    31.42 ns |    89.65 ns |    553.7 ns |   1.06 |    0.19 | 0.0143 |     123 B |        1.37 |
| Log(): 三属性 / Three properties (params object?[3] alloc)             |    803.4 ns |    36.02 ns |   106.20 ns |    815.2 ns |   1.52 |    0.23 | 0.0191 |     165 B |        1.83 |
| Log(): 五属性 / Five properties (params object?[5] alloc)             |    620.3 ns |    13.22 ns |    38.99 ns |    619.0 ns |   1.17 |    0.11 | 0.0243 |     204 B |        2.27 |
| Log(): 通过 ForContext / Via ForContext wrapper (LoggerWrapper) |    476.1 ns |    22.29 ns |    65.73 ns |    476.5 ns |   0.90 |    0.14 | 0.0143 |     123 B |        1.37 |
| Log(): 批量 100 条 / Batch of 100 (Amortized cost per item)                 | 75,825.1 ns | 2,749.49 ns | 8,063.79 ns | 73,601.7 ns | 143.41 |   18.41 | 1.7700 |   14855 B |      165.06 |

---

## LogParserBenchmarks-report

**测试目标 / Test Objective**: 模板渲染引擎 `LogParser.ParseMessage(string)` 的吞吐量 / Throughput of the template rendering engine `LogParser.ParseMessage(string)`
**架构背景 / Architecture Background**: Parser 内置 4096 条 `ConcurrentDictionary` 缓存。在正常基准线测试中均发生完美缓存命中，主要评估对特殊结构如对齐、解构修饰符 `{@}`等词法标记的极限解析与组合性能。<br>The parser has a built-in 4096-item `ConcurrentDictionary` cache. Perfect cache hits occur in standard baseline tests, primarily evaluating the extreme parsing and composition performance for special structures like alignment, destructuring modifiers `{@}`, and other lexical tokens.

| Method                                  | Mean         | Error     | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|----------:|----------:|---------:|--------:|-------:|-------:|-------:|----------:|------------:|
| 纯文本，无占位符 (命中) / Plain text, no placeholders (Cache hit)                       |     13.53 ns |  0.067 ns |  0.063 ns |     1.00 |    0.01 |      - |      - |      - |         - |          NA |
| 单属性模板 (命中) / Single property template (Cache hit)                          |     10.77 ns |  0.114 ns |  0.107 ns |     0.80 |    0.01 |      - |      - |      - |         - |          NA |
| 三属性模板 (命中) / Three properties template (Cache hit)                          |     16.72 ns |  0.056 ns |  0.050 ns |     1.24 |    0.01 |      - |      - |      - |         - |          NA |
| 复杂模板 (命中) / Complex template: Align+Format+Destructure (Hit)           |     19.41 ns |  0.149 ns |  0.140 ns |     1.43 |    0.01 |      - |      - |      - |         - |          NA |
| 含转义 {{ }} 的模板 (命中) / Template with escaped {{ }} (Hit)                  |     12.78 ns |  0.052 ns |  0.047 ns |     0.94 |    0.01 |      - |      - |      - |         - |          NA |
| 缓存未命中 / Cache miss (Approx. 6k strings, cleared after 4096) | 14,142.40 ns | 99.207 ns | 92.798 ns | 1,045.12 |    8.13 | 0.1221 | 0.0458 | 0.0153 |    1080 B |          NA |

---

## LogWriterBenchmarks-report

**测试目标 / Test Objective**: 格式化输出引擎的性能 (`LogWriter.Render`) 以及 `WriterPool` 对象池收益 / Performance of the formatted output engine (`LogWriter.Render`) and `WriterPool` object pool benefits
**架构背景 / Architecture Background**: 针对纯文本 (Text)、带 ANSI 颜色控制码 (Color) 以及 JSON 格式化的渲染损耗对比。对比直接分配 `new LogTextWriter()` 与使用池化 `WriterPool.Get/Return` 的性能与内存分配 (GC) 收益。<br>Compares rendering overhead across plain text (Text), text with ANSI color codes (Color), and JSON formatting. It also compares the performance and memory allocation (GC) benefits of directly allocating `new LogTextWriter()` versus using the pooled `WriterPool.Get/Return`.

| Method                                                   | Mean        | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|---------:|---------:|------:|-------:|----------:|------------:|
| Text: 纯文本消息 / Plain text message (No properties)                                       |   381.02 ns | 1.301 ns | 1.153 ns |  1.00 | 0.0134 |     112 B |        1.00 |
| Text: 单属性 / Single property                                              |   453.26 ns | 1.425 ns | 1.190 ns |  1.19 | 0.0172 |     144 B |        1.29 |
| Text: 四属性 / Four properties                                              |   673.61 ns | 3.096 ns | 2.896 ns |  1.77 | 0.0286 |     240 B |        2.14 |
| Text: 对齐 + 格式化 / Alignment + Formatting ({Count,8:D} {Percent:P1})              |   717.67 ns | 4.451 ns | 4.164 ns |  1.88 | 0.0296 |     248 B |        2.21 |
| Color: 单属性 / Single property (With ANSI color escape codes)                              |   491.06 ns | 3.237 ns | 3.028 ns |  1.29 | 0.0172 |     144 B |        1.29 |
| Color: 四属性 / Four properties                                             |   717.35 ns | 3.654 ns | 3.418 ns |  1.88 | 0.0286 |     240 B |        2.14 |
| JSON: 单属性 / Single property (With RenderedMessage + Properties fields)            |   562.51 ns | 2.015 ns | 1.885 ns |  1.48 | 0.0181 |     152 B |        1.36 |
| JSON: 四属性 / Four properties                                              | 1,130.29 ns | 4.667 ns | 4.366 ns |  2.97 | 0.0420 |     360 B |        3.21 |
| Pool: 池化路径 / Pool path (WriterPool.Get&lt;LogTextWriter&gt;() + Return()) |    55.26 ns | 0.179 ns | 0.167 ns |  0.15 |      - |         - |        0.00 |
| Alloc: 直接分配 / Direct allocation (new LogTextWriter())              |    24.38 ns | 0.539 ns | 0.620 ns |  0.06 | 0.0162 |     136 B |        1.21 |

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