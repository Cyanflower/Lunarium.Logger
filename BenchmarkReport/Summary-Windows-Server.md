# Benchmark Environment: Windows Server 

以下数据展示了 `Lunarium.Logger` 在 AMD EPYC 7402 云服务器等级多核处理器以及 Windows 10/Server-2022 环境下的微秒级性能开销与分配明细。<br>The following data demonstrates the microsecond-level performance overhead and allocation details of `Lunarium.Logger` running on a cloud server-grade multi-core processor (AMD EPYC 7402) and Windows 10/Server-2022 environment.

---

## 硬件信息 / Hardware Information

```
BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.4171)
AMD EPYC 7402, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.200
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
| 无规则，级别通过 (缓存命中) / No rules, level passed (Cache hit)                               |      9.186 ns |  0.0346 ns |  0.0270 ns |     1.00 |    0.00 |      - |         - |          NA |
| Include 规则，context 匹配通过 (缓存命中) / Include rules, context matched and passed (Cache hit)                |      9.078 ns |  0.1355 ns |  0.1201 ns |     0.99 |    0.01 |      - |         - |          NA |
| Include 规则，context 不匹配被拒绝 (缓存命中) / Include rules, context mismatched and rejected (Cache hit)              |      9.085 ns |  0.1554 ns |  0.1298 ns |     0.99 |    0.01 |      - |         - |          NA |
| Exclude 规则，context 不在排除列表通过 (缓存命中) / Exclude rules, context not in exclude list and passed (Cache hit)            |      9.162 ns |  0.1739 ns |  0.1626 ns |     1.00 |    0.02 |      - |         - |          NA |
| Exclude 规则，context 命中排除列表被拒绝 (缓存命中) / Exclude rules, context hit exclude list and rejected (Cache hit)           |      9.089 ns |  0.1404 ns |  0.1173 ns |     0.99 |    0.01 |      - |         - |          NA |
| 无规则，缓存未命中 (超时清空) / No rules, cache miss (Approx. 3k unique contexts, cleared after 2048) | 12,121.558 ns | 50.3954 ns | 44.6742 ns | 1,319.64 |    6.00 | 0.0153 |     194 B |          NA |

---

## LoggerThroughputBenchmarks-report

**测试目标 / Test Objective**: 调用方线程的 `Log()` 调用吞吐量 (Channel `TryWrite` 性能) / Throughput of `Log()` calls from the caller thread (Channel `TryWrite` performance)
**架构背景 / Architecture Background**: `Logger.Log()` 是同步方法，此测试用 `NullTarget` 隔离真实的后台 I/O 写入消耗，精准测量创建一条日志（包装属性参数、封箱拆箱代价）并成功抛入管道所需的时间。<br>`Logger.Log()` is a synchronous method. This test uses `NullTarget` to isolate actual background I/O writing overhead, accurately measuring the time required to create a log entry (boxing/unboxing costs, wrapping properties) and successfully pushing it into the channel.

| Method                                          | Mean        | Error     | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| Log(): 无属性，纯文本消息 / No properties, plain text message                              |    202.2 ns |   5.31 ns |    15.31 ns |  1.01 |    0.11 | 0.0105 |      - |      89 B |        1.00 |
| Log(): 单属性 / Single property (params object?[1] alloc)             |    171.6 ns |   4.34 ns |    12.60 ns |  0.85 |    0.09 | 0.0143 | 0.0033 |     121 B |        1.36 |
| Log(): 三属性 / Three properties (params object?[3] alloc)             |    413.2 ns |  19.32 ns |    56.65 ns |  2.06 |    0.32 | 0.0191 |      - |     163 B |        1.83 |
| Log(): 五属性 / Five properties (params object?[5] alloc)             |    306.4 ns |  12.74 ns |    37.38 ns |  1.52 |    0.22 | 0.0241 | 0.0007 |     201 B |        2.26 |
| Log(): 通过 ForContext / Via ForContext wrapper (LoggerWrapper) |    176.6 ns |   5.22 ns |    15.22 ns |  0.88 |    0.10 | 0.0143 | 0.0017 |     120 B |        1.35 |
| Log(): 批量 100 条 / Batch of 100 (Amortized cost per item)                 | 19,162.5 ns | 756.01 ns | 2,217.24 ns | 95.33 |   13.15 | 1.7090 | 0.0305 |   14449 B |      162.35 |

---

## LogParserBenchmarks-report

**测试目标 / Test Objective**: 模板渲染引擎 `LogParser.ParseMessage(string)` 的吞吐量 / Throughput of the template rendering engine `LogParser.ParseMessage(string)`
**架构背景 / Architecture Background**: Parser 内置 4096 条 `ConcurrentDictionary` 缓存。在正常基准线测试中均发生完美缓存命中，主要评估对特殊结构如对齐、解构修饰符 `{@}`等词法标记的极限解析与组合性能。<br>The parser has a built-in 4096-item `ConcurrentDictionary` cache. Perfect cache hits occur in standard baseline tests, primarily evaluating the extreme parsing and composition performance for special structures like alignment, destructuring modifiers `{@}`, and other lexical tokens.

| Method                                  | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|-------:|-------:|----------:|------------:|
| 纯文本，无占位符 (命中) / Plain text, no placeholders (Cache hit)                       |     13.86 ns |   0.295 ns |  0.276 ns |   1.00 |    0.03 |      - |      - |      - |         - |          NA |
| 单属性模板 (命中) / Single property template (Cache hit)                          |     10.71 ns |   0.128 ns |  0.120 ns |   0.77 |    0.02 |      - |      - |      - |         - |          NA |
| 三属性模板 (命中) / Three properties template (Cache hit)                          |     16.73 ns |   0.211 ns |  0.197 ns |   1.21 |    0.03 |      - |      - |      - |         - |          NA |
| 复杂模板 (命中) / Complex template: Align+Format+Destructure (Hit)           |     19.55 ns |   0.272 ns |  0.241 ns |   1.41 |    0.03 |      - |      - |      - |         - |          NA |
| 含转义 {{ }} 的模板 (命中) / Template with escaped {{ }} (Hit)                  |     13.13 ns |   0.275 ns |  0.257 ns |   0.95 |    0.03 |      - |      - |      - |         - |          NA |
| 缓存未命中 / Cache miss (Approx. 6k strings, cleared after 4096) | 13,096.81 ns | 103.631 ns | 96.937 ns | 945.43 |   19.20 | 0.1221 | 0.0458 | 0.0153 |    1079 B |          NA |

---

## LogWriterBenchmarks-report

**测试目标 / Test Objective**: 格式化输出引擎的性能 (`LogWriter.Render`) 以及 `WriterPool` 对象池收益 / Performance of the formatted output engine (`LogWriter.Render`) and `WriterPool` object pool benefits
**架构背景 / Architecture Background**: 针对纯文本 (Text)、带 ANSI 颜色控制码 (Color) 以及 JSON 格式化的渲染损耗对比。对比直接分配 `new LogTextWriter()` 与使用池化 `WriterPool.Get/Return` 的性能与内存分配 (GC) 收益。<br>Compares rendering overhead across plain text (Text), text with ANSI color codes (Color), and JSON formatting. It also compares the performance and memory allocation (GC) benefits of directly allocating `new LogTextWriter()` versus using the pooled `WriterPool.Get/Return`.

| Method                                                   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Text: 纯文本消息 / Plain text message (No properties)                                       |   364.02 ns |  3.667 ns |  3.251 ns |  1.00 |    0.01 | 0.0134 |     112 B |        1.00 |
| Text: 单属性 / Single property                                              |   451.82 ns |  3.996 ns |  3.738 ns |  1.24 |    0.01 | 0.0172 |     144 B |        1.29 |
| Text: 四属性 / Four properties                                              |   602.26 ns |  8.354 ns |  7.814 ns |  1.65 |    0.03 | 0.0286 |     240 B |        2.14 |
| Text: 对齐 + 格式化 / Alignment + Formatting ({Count,8:D} {Percent:P1})              |   679.50 ns |  8.096 ns |  7.177 ns |  1.87 |    0.02 | 0.0296 |     248 B |        2.21 |
| Color: 单属性 / Single property (With ANSI color escape codes)                              |   477.80 ns |  2.547 ns |  2.382 ns |  1.31 |    0.01 | 0.0172 |     144 B |        1.29 |
| Color: 四属性 / Four properties                                             |   668.48 ns |  8.632 ns |  8.074 ns |  1.84 |    0.03 | 0.0286 |     240 B |        2.14 |
| JSON: 单属性 / Single property (With RenderedMessage + Properties fields)            |   539.17 ns |  4.138 ns |  3.668 ns |  1.48 |    0.02 | 0.0181 |     152 B |        1.36 |
| JSON: 四属性 / Four properties                                              | 1,107.56 ns | 14.937 ns | 13.972 ns |  3.04 |    0.05 | 0.0420 |     360 B |        3.21 |
| Pool: 池化路径 / Pool path (WriterPool.Get&lt;LogTextWriter&gt;() + Return()) |    52.84 ns |  0.242 ns |  0.226 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| Alloc: 直接分配 / Direct allocation (new LogTextWriter())              |    14.15 ns |  0.306 ns |  0.287 ns |  0.04 |    0.00 | 0.0162 |     136 B |        1.21 |

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