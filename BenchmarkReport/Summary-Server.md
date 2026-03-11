
```
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7542, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
BenchmarkAt: 2026-03-12
```

FilterBenchmarks-report

| Method                                          | Mean          | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |--------------:|-----------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| &#39;无规则，级别通过 (缓存命中)&#39;                               |      9.361 ns |  0.0287 ns |  0.0254 ns |     1.00 |    0.00 |      - |         - |          NA |
| &#39;Include 规则，context 匹配通过 (缓存命中)&#39;                |      9.479 ns |  0.0181 ns |  0.0151 ns |     1.01 |    0.00 |      - |         - |          NA |
| &#39;Include 规则，context 不匹配被拒绝 (缓存命中)&#39;              |      9.702 ns |  0.0126 ns |  0.0105 ns |     1.04 |    0.00 |      - |         - |          NA |
| &#39;Exclude 规则，context 不在排除列表通过 (缓存命中)&#39;            |      9.369 ns |  0.0537 ns |  0.0502 ns |     1.00 |    0.01 |      - |         - |          NA |
| &#39;Exclude 规则，context 命中排除列表被拒绝 (缓存命中)&#39;           |      9.390 ns |  0.0280 ns |  0.0249 ns |     1.00 |    0.00 |      - |         - |          NA |
| &#39;无规则，缓存未命中 (近似，3000 个唯一 context，超出 2048 后缓存清空)&#39; | 13,616.814 ns | 18.8979 ns | 17.6771 ns | 1,454.58 |    4.23 | 0.0153 |     194 B |          NA |

LoggerThroughputBenchmarks-report

| Method                                          | Mean        | Error       | StdDev      | Median      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------:|------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| &#39;Log(): 无属性，纯文本消息&#39;                              |    531.4 ns |    12.67 ns |    37.35 ns |    533.9 ns |   1.01 |    0.10 | 0.0105 |      90 B |        1.00 |
| &#39;Log(): 单属性 (params object?[1] 分配)&#39;             |    558.4 ns |    31.42 ns |    89.65 ns |    553.7 ns |   1.06 |    0.19 | 0.0143 |     123 B |        1.37 |
| &#39;Log(): 三属性 (params object?[3] 分配)&#39;             |    803.4 ns |    36.02 ns |   106.20 ns |    815.2 ns |   1.52 |    0.23 | 0.0191 |     165 B |        1.83 |
| &#39;Log(): 五属性 (params object?[5] 分配)&#39;             |    620.3 ns |    13.22 ns |    38.99 ns |    619.0 ns |   1.17 |    0.11 | 0.0243 |     204 B |        2.27 |
| &#39;Log(): 通过 ForContext 包装器 (LoggerWrapper 额外调用)&#39; |    476.1 ns |    22.29 ns |    65.73 ns |    476.5 ns |   0.90 |    0.14 | 0.0143 |     123 B |        1.37 |
| &#39;Log(): 批量 100 条（测量批量分摊后每条的开销）&#39;                 | 75,825.1 ns | 2,749.49 ns | 8,063.79 ns | 73,601.7 ns | 143.41 |   18.41 | 1.7700 |   14855 B |      165.06 |

LogParserBenchmarks-report

| Method                                  | Mean         | Error     | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|---------------------------------------- |-------------:|----------:|----------:|---------:|--------:|-------:|-------:|-------:|----------:|------------:|
| &#39;纯文本，无占位符 (缓存命中)&#39;                       |     13.53 ns |  0.067 ns |  0.063 ns |     1.00 |    0.01 |      - |      - |      - |         - |          NA |
| &#39;单属性模板 (缓存命中)&#39;                          |     10.77 ns |  0.114 ns |  0.107 ns |     0.80 |    0.01 |      - |      - |      - |         - |          NA |
| &#39;三属性模板 (缓存命中)&#39;                          |     16.72 ns |  0.056 ns |  0.050 ns |     1.24 |    0.01 |      - |      - |      - |         - |          NA |
| &#39;复杂模板：对齐 + 格式化 + 解构前缀 (缓存命中)&#39;           |     19.41 ns |  0.149 ns |  0.140 ns |     1.43 |    0.01 |      - |      - |      - |         - |          NA |
| &#39;含转义 {{ }} 的模板 (缓存命中)&#39;                  |     12.78 ns |  0.052 ns |  0.047 ns |     0.94 |    0.01 |      - |      - |      - |         - |          NA |
| &#39;缓存未命中 (近似，6000 个唯一字符串池，超出 4096 后缓存清空)&#39; | 14,142.40 ns | 99.207 ns | 92.798 ns | 1,045.12 |    8.13 | 0.1221 | 0.0458 | 0.0153 |    1080 B |          NA |

LogWriterBenchmarks-report

| Method                                                   | Mean        | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------- |------------:|---------:|---------:|------:|-------:|----------:|------------:|
| &#39;Text: 纯文本消息（无属性）&#39;                                       |   381.02 ns | 1.301 ns | 1.153 ns |  1.00 | 0.0134 |     112 B |        1.00 |
| &#39;Text: 单属性&#39;                                              |   453.26 ns | 1.425 ns | 1.190 ns |  1.19 | 0.0172 |     144 B |        1.29 |
| &#39;Text: 四属性&#39;                                              |   673.61 ns | 3.096 ns | 2.896 ns |  1.77 | 0.0286 |     240 B |        2.14 |
| &#39;Text: 对齐 + 格式化 ({Count,8:D} {Percent:P1})&#39;              |   717.67 ns | 4.451 ns | 4.164 ns |  1.88 | 0.0296 |     248 B |        2.21 |
| &#39;Color: 单属性（含 ANSI 颜色转义代码）&#39;                              |   491.06 ns | 3.237 ns | 3.028 ns |  1.29 | 0.0172 |     144 B |        1.29 |
| &#39;Color: 四属性&#39;                                             |   717.35 ns | 3.654 ns | 3.418 ns |  1.88 | 0.0286 |     240 B |        2.14 |
| &#39;JSON: 单属性（含 RenderedMessage + Propertys 字段）&#39;            |   562.51 ns | 2.015 ns | 1.885 ns |  1.48 | 0.0181 |     152 B |        1.36 |
| &#39;JSON: 四属性&#39;                                              | 1,130.29 ns | 4.667 ns | 4.366 ns |  2.97 | 0.0420 |     360 B |        3.21 |
| &#39;Pool: WriterPool.Get&lt;LogTextWriter&gt;() + Return()（池化路径）&#39; |    55.26 ns | 0.179 ns | 0.167 ns |  0.15 |      - |         - |        0.00 |
| &#39;Alloc: new LogTextWriter()（直接分配，用于对比池化收益）&#39;              |    24.38 ns | 0.539 ns | 0.620 ns |  0.06 | 0.0162 |     136 B |        1.21 |


* Legends *
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  Gen0        : GC Generation 0 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns        : 1 Nanosecond (0.000000001 sec)