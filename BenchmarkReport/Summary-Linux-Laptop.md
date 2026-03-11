
```
BenchmarkDotNet v0.14.0, Fedora Linux 42 (Workstation Edition)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.226.5608), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.2 (10.0.226.5608), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

FilterBenchmarks-report

| Method                                          | Mean          | Error       | StdDev      | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|------------:|------------:|---------:|--------:|-------:|----------:|------------:|
| &#39;无规则，级别通过 (缓存命中)&#39;                               |      8.917 ns |   0.2037 ns |   0.2346 ns |     1.00 |    0.04 |      - |         - |          NA |
| &#39;Include 规则，context 匹配通过 (缓存命中)&#39;                |      8.790 ns |   0.0863 ns |   0.0720 ns |     0.99 |    0.03 |      - |         - |          NA |
| &#39;Include 规则，context 不匹配被拒绝 (缓存命中)&#39;              |      9.523 ns |   0.1237 ns |   0.1158 ns |     1.07 |    0.03 |      - |         - |          NA |
| &#39;Exclude 规则，context 不在排除列表通过 (缓存命中)&#39;            |      8.888 ns |   0.2043 ns |   0.1911 ns |     1.00 |    0.03 |      - |         - |          NA |
| &#39;Exclude 规则，context 命中排除列表被拒绝 (缓存命中)&#39;           |      8.811 ns |   0.1450 ns |   0.1210 ns |     0.99 |    0.03 |      - |         - |          NA |
| &#39;无规则，缓存未命中 (近似，3000 个唯一 context，超出 2048 后缓存清空)&#39; | 26,014.008 ns | 419.6278 ns | 392.5201 ns | 2,919.23 |   84.74 | 0.0305 |     216 B |          NA |


LoggerThroughputBenchmarks-report

| Method                                          | Mean        | Error     | StdDev      | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|----------:|------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Log(): 无属性，纯文本消息&#39;                              |    149.4 ns |   4.80 ns |    13.69 ns |   1.01 |    0.13 | 0.0188 | 0.0005 |      90 B |        1.00 |
| &#39;Log(): 单属性 (params object?[1] 分配)&#39;             |    185.9 ns |   5.17 ns |    15.24 ns |   1.25 |    0.15 | 0.0260 | 0.0002 |     123 B |        1.37 |
| &#39;Log(): 三属性 (params object?[3] 分配)&#39;             |    189.3 ns |   4.67 ns |    13.78 ns |   1.28 |    0.15 | 0.0339 | 0.0010 |     161 B |        1.79 |
| &#39;Log(): 五属性 (params object?[5] 分配)&#39;             |    204.4 ns |   5.35 ns |    15.53 ns |   1.38 |    0.16 | 0.0420 | 0.0026 |     201 B |        2.23 |
| &#39;Log(): 通过 ForContext 包装器 (LoggerWrapper 额外调用)&#39; |    183.1 ns |   4.48 ns |    13.20 ns |   1.24 |    0.14 | 0.0257 | 0.0007 |     122 B |        1.36 |
| &#39;Log(): 批量 100 条（测量批量分摊后每条的开销）&#39;                 | 19,773.6 ns | 637.78 ns | 1,870.49 ns | 133.45 |   17.58 | 3.0212 | 0.1526 |   14477 B |      160.86 |

LogParserBenchmarks-report

| Method                                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| &#39;纯文本，无占位符 (缓存命中)&#39;                       |     13.54 ns |   0.177 ns |   0.148 ns |     1.00 |    0.01 |      - |      - |         - |          NA |
| &#39;单属性模板 (缓存命中)&#39;                          |     11.52 ns |   0.229 ns |   0.191 ns |     0.85 |    0.02 |      - |      - |         - |          NA |
| &#39;三属性模板 (缓存命中)&#39;                          |     17.60 ns |   0.412 ns |   0.405 ns |     1.30 |    0.03 |      - |      - |         - |          NA |
| &#39;复杂模板：对齐 + 格式化 + 解构前缀 (缓存命中)&#39;           |     18.74 ns |   0.384 ns |   0.340 ns |     1.38 |    0.03 |      - |      - |         - |          NA |
| &#39;含转义 {{ }} 的模板 (缓存命中)&#39;                  |     14.05 ns |   0.265 ns |   0.221 ns |     1.04 |    0.02 |      - |      - |         - |          NA |
| &#39;缓存未命中 (近似，6000 个唯一字符串池，超出 4096 后缓存清空)&#39; | 26,437.30 ns | 407.861 ns | 361.558 ns | 1,952.35 |   32.74 | 0.1526 | 0.0610 |    1085 B |          NA |

LogWriterBenchmarks-report

| Method                                                   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Text: 纯文本消息（无属性）&#39;                                       |   347.95 ns |  5.346 ns |  5.000 ns |  1.00 |    0.02 | 0.0234 |     112 B |        1.00 |
| &#39;Text: 单属性&#39;                                              |   425.43 ns |  8.352 ns |  8.937 ns |  1.22 |    0.03 | 0.0305 |     144 B |        1.29 |
| &#39;Text: 四属性&#39;                                              |   582.14 ns | 11.430 ns | 11.737 ns |  1.67 |    0.04 | 0.0505 |     240 B |        2.14 |
| &#39;Text: 对齐 + 格式化 ({Count,8:D} {Percent:P1})&#39;              |   666.54 ns | 12.731 ns | 16.101 ns |  1.92 |    0.05 | 0.0525 |     248 B |        2.21 |
| &#39;Color: 单属性（含 ANSI 颜色转义代码）&#39;                              |   455.94 ns |  7.754 ns |  7.253 ns |  1.31 |    0.03 | 0.0305 |     144 B |        1.29 |
| &#39;Color: 四属性&#39;                                             |   649.25 ns |  8.767 ns |  8.201 ns |  1.87 |    0.03 | 0.0505 |     240 B |        2.14 |
| &#39;JSON: 单属性（含 RenderedMessage + Propertys 字段）&#39;            |   577.74 ns |  9.754 ns |  8.647 ns |  1.66 |    0.03 | 0.0315 |     152 B |        1.36 |
| &#39;JSON: 四属性&#39;                                              | 1,148.39 ns | 10.269 ns |  9.103 ns |  3.30 |    0.05 | 0.0763 |     360 B |        3.21 |
| &#39;Pool: WriterPool.Get&lt;LogTextWriter&gt;() + Return()（池化路径）&#39; |    81.72 ns |  1.227 ns |  1.148 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| &#39;Alloc: new LogTextWriter()（直接分配，用于对比池化收益）&#39;              |    20.17 ns |  0.429 ns |  0.495 ns |  0.06 |    0.00 | 0.0289 |     136 B |        1.21 |

* Legends *
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  Gen0        : GC Generation 0 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns        : 1 Nanosecond (0.000000001 sec)