
```
BenchmarkDotNet v0.14.0, Windows 10 (10.0.20348.4171)
AMD EPYC 7402, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

FilterBenchmarks-report

| Method                                          | Mean          | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|-----------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| &#39;无规则，级别通过 (缓存命中)&#39;                               |      9.186 ns |  0.0346 ns |  0.0270 ns |     1.00 |    0.00 |      - |         - |          NA |
| &#39;Include 规则，context 匹配通过 (缓存命中)&#39;                |      9.078 ns |  0.1355 ns |  0.1201 ns |     0.99 |    0.01 |      - |         - |          NA |
| &#39;Include 规则，context 不匹配被拒绝 (缓存命中)&#39;              |      9.085 ns |  0.1554 ns |  0.1298 ns |     0.99 |    0.01 |      - |         - |          NA |
| &#39;Exclude 规则，context 不在排除列表通过 (缓存命中)&#39;            |      9.162 ns |  0.1739 ns |  0.1626 ns |     1.00 |    0.02 |      - |         - |          NA |
| &#39;Exclude 规则，context 命中排除列表被拒绝 (缓存命中)&#39;           |      9.089 ns |  0.1404 ns |  0.1173 ns |     0.99 |    0.01 |      - |         - |          NA |
| &#39;无规则，缓存未命中 (近似，3000 个唯一 context，超出 2048 后缓存清空)&#39; | 12,121.558 ns | 50.3954 ns | 44.6742 ns | 1,319.64 |    6.00 | 0.0153 |     194 B |          NA |

LoggerThroughputBenchmarks-report

| Method                                          | Mean        | Error     | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Log(): 无属性，纯文本消息&#39;                              |    202.2 ns |   5.31 ns |    15.31 ns |  1.01 |    0.11 | 0.0105 |      - |      89 B |        1.00 |
| &#39;Log(): 单属性 (params object?[1] 分配)&#39;             |    171.6 ns |   4.34 ns |    12.60 ns |  0.85 |    0.09 | 0.0143 | 0.0033 |     121 B |        1.36 |
| &#39;Log(): 三属性 (params object?[3] 分配)&#39;             |    413.2 ns |  19.32 ns |    56.65 ns |  2.06 |    0.32 | 0.0191 |      - |     163 B |        1.83 |
| &#39;Log(): 五属性 (params object?[5] 分配)&#39;             |    306.4 ns |  12.74 ns |    37.38 ns |  1.52 |    0.22 | 0.0241 | 0.0007 |     201 B |        2.26 |
| &#39;Log(): 通过 ForContext 包装器 (LoggerWrapper 额外调用)&#39; |    176.6 ns |   5.22 ns |    15.22 ns |  0.88 |    0.10 | 0.0143 | 0.0017 |     120 B |        1.35 |
| &#39;Log(): 批量 100 条（测量批量分摊后每条的开销）&#39;                 | 19,162.5 ns | 756.01 ns | 2,217.24 ns | 95.33 |   13.15 | 1.7090 | 0.0305 |   14449 B |      162.35 |

LogParserBenchmarks-report

| Method                                  | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|-------:|-------:|----------:|------------:|
| &#39;纯文本，无占位符 (缓存命中)&#39;                       |     13.86 ns |   0.295 ns |  0.276 ns |   1.00 |    0.03 |      - |      - |      - |         - |          NA |
| &#39;单属性模板 (缓存命中)&#39;                          |     10.71 ns |   0.128 ns |  0.120 ns |   0.77 |    0.02 |      - |      - |      - |         - |          NA |
| &#39;三属性模板 (缓存命中)&#39;                          |     16.73 ns |   0.211 ns |  0.197 ns |   1.21 |    0.03 |      - |      - |      - |         - |          NA |
| &#39;复杂模板：对齐 + 格式化 + 解构前缀 (缓存命中)&#39;           |     19.55 ns |   0.272 ns |  0.241 ns |   1.41 |    0.03 |      - |      - |      - |         - |          NA |
| &#39;含转义 {{ }} 的模板 (缓存命中)&#39;                  |     13.13 ns |   0.275 ns |  0.257 ns |   0.95 |    0.03 |      - |      - |      - |         - |          NA |
| &#39;缓存未命中 (近似，6000 个唯一字符串池，超出 4096 后缓存清空)&#39; | 13,096.81 ns | 103.631 ns | 96.937 ns | 945.43 |   19.20 | 0.1221 | 0.0458 | 0.0153 |    1079 B |          NA |

LogWriterBenchmarks-report

| Method                                                   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Text: 纯文本消息（无属性）&#39;                                       |   364.02 ns |  3.667 ns |  3.251 ns |  1.00 |    0.01 | 0.0134 |     112 B |        1.00 |
| &#39;Text: 单属性&#39;                                              |   451.82 ns |  3.996 ns |  3.738 ns |  1.24 |    0.01 | 0.0172 |     144 B |        1.29 |
| &#39;Text: 四属性&#39;                                              |   602.26 ns |  8.354 ns |  7.814 ns |  1.65 |    0.03 | 0.0286 |     240 B |        2.14 |
| &#39;Text: 对齐 + 格式化 ({Count,8:D} {Percent:P1})&#39;              |   679.50 ns |  8.096 ns |  7.177 ns |  1.87 |    0.02 | 0.0296 |     248 B |        2.21 |
| &#39;Color: 单属性（含 ANSI 颜色转义代码）&#39;                              |   477.80 ns |  2.547 ns |  2.382 ns |  1.31 |    0.01 | 0.0172 |     144 B |        1.29 |
| &#39;Color: 四属性&#39;                                             |   668.48 ns |  8.632 ns |  8.074 ns |  1.84 |    0.03 | 0.0286 |     240 B |        2.14 |
| &#39;JSON: 单属性（含 RenderedMessage + Propertys 字段）&#39;            |   539.17 ns |  4.138 ns |  3.668 ns |  1.48 |    0.02 | 0.0181 |     152 B |        1.36 |
| &#39;JSON: 四属性&#39;                                              | 1,107.56 ns | 14.937 ns | 13.972 ns |  3.04 |    0.05 | 0.0420 |     360 B |        3.21 |
| &#39;Pool: WriterPool.Get&lt;LogTextWriter&gt;() + Return()（池化路径）&#39; |    52.84 ns |  0.242 ns |  0.226 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| &#39;Alloc: new LogTextWriter()（直接分配，用于对比池化收益）&#39;              |    14.15 ns |  0.306 ns |  0.287 ns |  0.04 |    0.00 | 0.0162 |     136 B |        1.21 |

Legends
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  Gen0        : GC Generation 0 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns        : 1 Nanosecond (0.000000001 sec)