# Benchmark Environment: Linux Laptop 

以下数据展示了 `Lunarium.Logger` 在笔记本电脑（Intel Core i7-8750H）以及 Fedora Linux 42 环境下的微秒级性能开销与分配明细。<br>The following data demonstrates the microsecond-level performance overhead and allocation details of `Lunarium.Logger` running on a laptop (Intel Core i7-8750H) and Fedora Linux 42 environment.

---

## 硬件信息 / Hardware Information

```
BenchmarkDotNet v0.14.0, Fedora Linux 42 (Workstation Edition)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.226.5608), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.2 (10.0.226.5608), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

---

## FilterBenchmarks-report

**测试目标 / Test Objective**: `LoggerFilter.ShouldEmit(LogEntry, SinkOutputConfig)` 的吞吐量 / Throughput
**架构背景 / Architecture Background**: 每个 `LoggerFilter` 实例持有独立的 2048 条上下文缓存；缓存满时全量清空。使用预热好的缓存来测量真实条件下的极致性能字典读取路径。<br>Each `LoggerFilter` instance holds an independent cache of 2048 contexts; the cache is fully cleared when full. Pre-warmed caches are used to measure the extreme performance of dictionary read paths under real conditions.

| Method                                          | Mean          | Error       | StdDev      | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|------------:|------------:|---------:|--------:|-------:|----------:|------------:|
| 无规则，级别通过 (缓存命中) / No rules, level passed (Cache hit)                               |      8.917 ns |   0.2037 ns |   0.2346 ns |     1.00 |    0.04 |      - |         - |          NA |
| Include 规则，context 匹配通过 (缓存命中) / Include rules, context matched and passed (Cache hit)                |      8.790 ns |   0.0863 ns |   0.0720 ns |     0.99 |    0.03 |      - |         - |          NA |
| Include 规则，context 不匹配被拒绝 (缓存命中) / Include rules, context mismatched and rejected (Cache hit)              |      9.523 ns |   0.1237 ns |   0.1158 ns |     1.07 |    0.03 |      - |         - |          NA |
| Exclude 规则，context 不在排除列表通过 (缓存命中) / Exclude rules, context not in exclude list and passed (Cache hit)            |      8.888 ns |   0.2043 ns |   0.1911 ns |     1.00 |    0.03 |      - |         - |          NA |
| Exclude 规则，context 命中排除列表被拒绝 (缓存命中) / Exclude rules, context hit exclude list and rejected (Cache hit)           |      8.811 ns |   0.1450 ns |   0.1210 ns |     0.99 |    0.03 |      - |         - |          NA |
| 无规则，缓存未命中 (超时清空) / No rules, cache miss (Approx. 3k unique contexts, cleared after 2048) | 26,014.008 ns | 419.6278 ns | 392.5201 ns | 2,919.23 |   84.74 | 0.0305 |     216 B |          NA |

---

## LoggerThroughputBenchmarks-report

**测试目标 / Test Objective**: 调用方线程的 `Log()` 调用吞吐量 (Channel `TryWrite` 性能) / Throughput of `Log()` calls from the caller thread (Channel `TryWrite` performance)
**架构背景 / Architecture Background**: `Logger.Log()` 是同步方法，此测试用 `NullTarget` 隔离真实的后台 I/O 写入消耗，精准测量创建一条日志（包装属性参数、封箱拆箱代价）并成功抛入管道所需的时间。<br>`Logger.Log()` is a synchronous method. This test uses `NullTarget` to isolate actual background I/O writing overhead, accurately measuring the time required to create a log entry (boxing/unboxing costs, wrapping properties) and successfully pushing it into the channel.

| Method                                          | Mean        | Error     | StdDev      | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|----------:|------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| Log(): 无属性，纯文本消息 / No properties, plain text message                              |    149.4 ns |   4.80 ns |    13.69 ns |   1.01 |    0.13 | 0.0188 | 0.0005 |      90 B |        1.00 |
| Log(): 单属性 / Single property (params object?[1] alloc)             |    185.9 ns |   5.17 ns |    15.24 ns |   1.25 |    0.15 | 0.0260 | 0.0002 |     123 B |        1.37 |
| Log(): 三属性 / Three properties (params object?[3] alloc)             |    189.3 ns |   4.67 ns |    13.78 ns |   1.28 |    0.15 | 0.0339 | 0.0010 |     161 B |        1.79 |
| Log(): 五属性 / Five properties (params object?[5] alloc)             |    204.4 ns |   5.35 ns |    15.53 ns |   1.38 |    0.16 | 0.0420 | 0.0026 |     201 B |        2.23 |
| Log(): 通过 ForContext / Via ForContext wrapper (LoggerWrapper) |    183.1 ns |   4.48 ns |    13.20 ns |   1.24 |    0.14 | 0.0257 | 0.0007 |     122 B |        1.36 |
| Log(): 批量 100 条 / Batch of 100 (Amortized cost per item)                 | 19,773.6 ns | 637.78 ns | 1,870.49 ns | 133.45 |   17.58 | 3.0212 | 0.1526 |   14477 B |      160.86 |

---

## LogParserBenchmarks-report

**测试目标 / Test Objective**: 模板渲染引擎 `LogParser.ParseMessage(string)` 的吞吐量 / Throughput of the template rendering engine `LogParser.ParseMessage(string)`
**架构背景 / Architecture Background**: Parser 内置 4096 条 `ConcurrentDictionary` 缓存。在正常基准线测试中均发生完美缓存命中，主要评估对特殊结构如对齐、解构修饰符 `{@}`等词法标记的极限解析与组合性能。<br>The parser has a built-in 4096-item `ConcurrentDictionary` cache. Perfect cache hits occur in standard baseline tests, primarily evaluating the extreme parsing and composition performance for special structures like alignment, destructuring modifiers `{@}`, and other lexical tokens.

| Method                                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| 纯文本，无占位符 (命中) / Plain text, no placeholders (Cache hit)                       |     13.54 ns |   0.177 ns |   0.148 ns |     1.00 |    0.01 |      - |      - |         - |          NA |
| 单属性模板 (命中) / Single property template (Cache hit)                          |     11.52 ns |   0.229 ns |   0.191 ns |     0.85 |    0.02 |      - |      - |         - |          NA |
| 三属性模板 (命中) / Three properties template (Cache hit)                          |     17.60 ns |   0.412 ns |   0.405 ns |     1.30 |    0.03 |      - |      - |         - |          NA |
| 复杂模板 (命中) / Complex template: Align+Format+Destructure (Hit)           |     18.74 ns |   0.384 ns |   0.340 ns |     1.38 |    0.03 |      - |      - |         - |          NA |
| 含转义 {{ }} 的模板 (命中) / Template with escaped {{ }} (Hit)                  |     14.05 ns |   0.265 ns |   0.221 ns |     1.04 |    0.02 |      - |      - |         - |          NA |
| 缓存未命中 / Cache miss (Approx. 6k strings, cleared after 4096) | 26,437.30 ns | 407.861 ns | 361.558 ns | 1,952.35 |   32.74 | 0.1526 | 0.0610 |    1085 B |          NA |

---

## LogWriterBenchmarks-report

**测试目标 / Test Objective**: 格式化输出引擎的性能 (`LogWriter.Render`) 以及 `WriterPool` 对象池收益 / Performance of the formatted output engine (`LogWriter.Render`) and `WriterPool` object pool benefits
**架构背景 / Architecture Background**: 针对纯文本 (Text)、带 ANSI 颜色控制码 (Color) 以及 JSON 格式化的渲染损耗对比。对比直接分配 `new LogTextWriter()` 与使用池化 `WriterPool.Get/Return` 的性能与内存分配 (GC) 收益。<br>Compares rendering overhead across plain text (Text), text with ANSI color codes (Color), and JSON formatting. It also compares the performance and memory allocation (GC) benefits of directly allocating `new LogTextWriter()` versus using the pooled `WriterPool.Get/Return`.

| Method                                                   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Text: 纯文本消息 / Plain text message (No properties)                                       |   347.95 ns |  5.346 ns |  5.000 ns |  1.00 |    0.02 | 0.0234 |     112 B |        1.00 |
| Text: 单属性 / Single property                                              |   425.43 ns |  8.352 ns |  8.937 ns |  1.22 |    0.03 | 0.0305 |     144 B |        1.29 |
| Text: 四属性 / Four properties                                              |   582.14 ns | 11.430 ns | 11.737 ns |  1.67 |    0.04 | 0.0505 |     240 B |        2.14 |
| Text: 对齐 + 格式化 / Alignment + Formatting ({Count,8:D} {Percent:P1})              |   666.54 ns | 12.731 ns | 16.101 ns |  1.92 |    0.05 | 0.0525 |     248 B |        2.21 |
| Color: 单属性 / Single property (With ANSI color escape codes)                              |   455.94 ns |  7.754 ns |  7.253 ns |  1.31 |    0.03 | 0.0305 |     144 B |        1.29 |
| Color: 四属性 / Four properties                                             |   649.25 ns |  8.767 ns |  8.201 ns |  1.87 |    0.03 | 0.0505 |     240 B |        2.14 |
| JSON: 单属性 / Single property (With RenderedMessage + Properties fields)            |   577.74 ns |  9.754 ns |  8.647 ns |  1.66 |    0.03 | 0.0315 |     152 B |        1.36 |
| JSON: 四属性 / Four properties                                              | 1,148.39 ns | 10.269 ns |  9.103 ns |  3.30 |    0.05 | 0.0763 |     360 B |        3.21 |
| Pool: 池化路径 / Pool path (WriterPool.Get&lt;LogTextWriter&gt;() + Return()) |    81.72 ns |  1.227 ns |  1.148 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| Alloc: 直接分配 / Direct allocation (new LogTextWriter())              |    20.17 ns |  0.429 ns |  0.495 ns |  0.06 |    0.00 | 0.0289 |     136 B |        1.21 |

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