# Plan: LogJsonWriter 性能优化

**目标文件**: `src/Lunarium.Logger/Writer/LogJsonWriter.cs`
**优先级排序**: 优化一 > 优化二 > 优化三
**依赖**: 三项优化互相独立，可按任意顺序分别执行

---

## 优化一：尾部逗号移除 → write-before 模式

### 改什么

将 `WritePropertyValue`（第 197–201 行）和 `WriteException`（第 218–221 行）中的两处 `StringBuilder.Remove()` 调用替换为"非首项时先写逗号"的模式。

**当前代码（WritePropertyValue）**：
```csharp
_stringBuilder.Append(',');
iteration++;
// ... 循环结束后
if (_stringBuilder[_stringBuilder.Length - 1] == ',')
    _stringBuilder.Remove(_stringBuilder.Length - 1, 1); // ← 改掉这里
```

**目标代码**：
```csharp
// 循环内，写 key:value 之前：
if (iteration > 0) _stringBuilder.Append(',');
// ... 写 key:value
iteration++;
// 循环结束后不需要任何 Remove
```

`WriteException` 同理：当有 Exception 时直接写 `"Exception":...`；当无 Exception 时直接 `Remove` 掉 `Propertys` 写完后留下的尾逗号。需要改为：`Propertys` 写完后不主动追加逗号，改由下游（`WriteException` 或 `EndEntry` 之前）决定是否在 `}` 前补逗号。

具体调整：`WritePropertyValue` 末尾的 `_stringBuilder.Append("},")` 改为 `_stringBuilder.Append("}")`（不追加逗号），在 `WriteException` 中统一处理分隔逻辑：有 Exception 时写 `,"Exception":...`，无 Exception 时不写。

### 为什么改

`StringBuilder.Remove(index, count)` 的内部实现是将被删除位置之后的所有字符向前复制一位（`Buffer.BlockCopy` 或等价操作），时间复杂度为 **O(n)**，其中 n 是 Remove 位置之后的剩余字符数。在 JSON 四属性场景下，执行 Remove 时 StringBuilder 中已有 ~300–400 个字符，这意味着每次 Remove 都在做一次百字节级别的内存搬移。

write-before 模式的代价是 O(1)：每次写逗号前只需检查 `iteration > 0`，一个整数比较。

### 原理

JSON 对象的 key:value 对之间需要 `,` 分隔，但最后一项之后不能有逗号。两种处理方式：
- **write-after**（当前）：每项写完后追加 `,`，最后统一删除尾逗号 → 末尾一次 Remove O(n)
- **write-before**（目标）：非首项写入前先追加 `,` → 零 Remove，仅多一次 `int > 0` 判断

write-before 是构建分隔符序列的惯用模式，在任何序列化场景下均优于 write-after。

### 影响

- `WritePropertyValue` 中消除一次 O(n) Remove（约 300–400 字节的内存搬移）
- `WriteException` 中消除一次 O(n) Remove（约 400–500 字节的内存搬移，此时 Propertys 已写完）
- JSON 四属性场景预计可节省 50–150 ns（实际取决于平台和字符串长度）
- JSON 单属性场景收益较小（字符串较短时 Remove 代价本就低）

### 副作用

- 需要同步调整 `WritePropertyValue` 末尾的 `"},"`：改为 `"}"`，让 `WriteException` 负责决定是否在其前加逗号
- `WriteException` 逻辑需要反转：有 Exception → 先写 `","Exception":...`（注意前导逗号）；无 Exception → 什么都不写（不再需要 Remove）
- `EndEntry` 写 `}` 时不受影响（对象末尾不需要逗号）
- 需要检查：`WriteContext` 等其他方法是否也存在类似的 write-after 模式，若有一并处理

---

## 优化二：WriteLevel 预烘焙常量字符串

### 改什么

将 `WriteLevel` 方法（第 89–90 行）中的两条字符串插值替换为每个枚举值对应的预生成静态常量字符串。

**当前代码**：
```csharp
_stringBuilder.Append($"\"Level\":\"{levelStr}\",");   // 运行时分配
_stringBuilder.Append($"\"LogLevel\":{(int)level},"); // 运行时分配
```

**目标代码**：
```csharp
private static readonly string[] _levelJsonFragments =
[
    "\"Level\":\"Debug\",\"LogLevel\":0,",
    "\"Level\":\"Info\",\"LogLevel\":1,",
    "\"Level\":\"Warning\",\"LogLevel\":2,",
    "\"Level\":\"Error\",\"LogLevel\":3,",
    "\"Level\":\"Critical\",\"LogLevel\":4,",
];

// WriteLevel 内：
var idx = (int)level;
_stringBuilder.Append(idx >= 0 && idx < _levelJsonFragments.Length
    ? _levelJsonFragments[idx]
    : $"\"Level\":\"Unknown\",\"LogLevel\":{idx},");
```

或者保持两次 Append，但使用 `switch` 直接返回 string literal，避免插值。

### 为什么改

每次调用 `WriteLevel` 都会执行两次字符串插值（`$"..."`），每次插值分配一个新的 `string` 对象。Level 枚举只有 5 个值，这些字符串在进程生命周期内始终相同，没有任何理由在每次日志写入时重新构造它们。

### 原理

编译器对 `$"..."` 的处理是在运行时调用 `String.Format` 或 `DefaultInterpolatedStringHandler`，始终产生堆分配。静态常量字符串在程序集加载时已存在于堆中，`Append(string)` 时只需传递引用，零额外分配。

### 影响

- 消除每条 JSON 日志的 2 次堆分配（两个临时 string 对象，各约 40–60 字节）
- 减少 GC 压力（微小，但零成本可得）
- 代码更清晰：Level → JSON 片段的映射关系显式可见

### 副作用

- 如果 `LogLevel` 枚举值发生变更（增删成员或调整数值），需要同步更新 `_levelJsonFragments` 数组，否则越界或输出错误字符串。需要加一行断言或静态验证（可在静态构造器中 `Debug.Assert`）。
- 数组长度与枚举值数量强耦合，建议加注释标明。

---

## 优化三：AppendJsonStringContent 批量复制

### 改什么

将 `AppendJsonStringContent` 方法（第 251–319 行）的逐字符 `for` 循环改为：先用 `IndexOfAny` 定位第一个需要转义的字符，将安全前缀一次性批量 `Append`，再进入逐字符处理。

**当前结构**：
```csharp
for (int i = 0; i < value.Length; i++)
{
    char c = value[i];
    // 对每个字符：if 代理对 → else switch 转义
}
```

**目标结构**：
```csharp
private static readonly SearchValues<char> _jsonEscapeChars =
    SearchValues.Create("\"\\\n\r\t\b\f");

int start = 0;
int idx;
ReadOnlySpan<char> span = value.AsSpan();

while ((idx = span[start..].IndexOfAny(_jsonEscapeChars)) >= 0)
{
    // 批量追加 [start, start+idx) 的安全前缀
    if (idx > 0) _stringBuilder.Append(span.Slice(start, idx));
    start += idx;

    char c = span[start];
    // 处理单个需要转义的字符（原有 switch 逻辑）
    // ... 对控制字符 c < 0x20 的 \uXXXX 处理仍在此处
    start++;
}
// 追加剩余的安全尾部
if (start < span.Length) _stringBuilder.Append(span[start..]);
```

注：`SearchValues<char>` 是 .NET 8+ 的 API，项目目标框架 net10.0 可以直接使用。代理对的高代理字符（`\uD800–\uDFFF`）不在 JSON 必须转义的字符集内，但仍需保留原有代理对处理逻辑。可将代理对检测改为：若在批量路径中遇到高代理（通过 `char.IsHighSurrogate` 检查），则在 while 循环的 switch 中专门处理。或者简化：`_jsonEscapeChars` 中加入高代理范围，强制进入逐字符路径处理代理对（保守方案，对代理对稍保守但绝对安全）。

### 为什么改

典型日志消息中不含需要转义的字符（无 `"`、`\`、控制字符）。当前逐字符循环对每个字符都执行一次 `switch` 匹配，即使该字符完全不需要转义。对于一条 50 个字符的消息，这是 50 次 switch + 50 次单字符 `Append` 调用。

批量复制后，对于无特殊字符的消息，整个方法只需一次 `IndexOfAny`（SIMD 加速，可在一个 CPU 指令周期内扫描 16–32 个字符）加一次 `Append(ReadOnlySpan<char>)`（内存复制），远快于 50 次单字符 Append。

### 原理

`SearchValues<char>` + `IndexOfAny` 在 .NET 8+ 中使用 SIMD 指令（AVX2/SSE4.2）一次比较 16–32 个字符，扫描速度约为逐字符循环的 8–16 倍。`StringBuilder.Append(ReadOnlySpan<char>)` 内部直接做内存复制，无需逐字符操作。两者合力将"无特殊字符"的普通日志消息的处理路径从 O(n) 慢速循环缩短为接近 O(1) 的快速路径。

### 影响

- 对不含特殊字符的消息（绝大多数日志）：`AppendJsonStringContent` 调用速度显著提升，消息越长收益越明显（50 字符约节省 100–200 ns，100 字符约节省 200–400 ns）
- 对含少量特殊字符的消息（如带引号的字符串值）：批量复制前缀，单独处理特殊字符，综合收益仍正向
- `WriteOriginalMessage`、`WriteRenderedMessage`、`ToJsonValue` 中所有间接调用 `AppendJsonStringContent` 的路径均受益
- 在 `LogJsonWriter` 上体现为每条日志节省约 50–300 ns（视消息长度和特殊字符密度而定）

### 副作用

- 代码复杂度提升（从直观的 for 循环变为 span + IndexOfAny 模式），需要更仔细的审查
- 代理对处理逻辑需要整合进新的 while 循环，需要明确决策：保守方案（将高代理加入 `_jsonEscapeChars`）vs 精确方案（循环内用 `char.IsHighSurrogate` 判断）
- 需要额外的测试用例覆盖：含 Emoji（代理对）的消息、纯无特殊字符消息、仅含特殊字符的消息、混合消息
- `SearchValues<char>` 不包含 `< 0x20` 控制字符范围（只能枚举具体字符，不能指定范围）。当前代码对 `c < 0x20` 的控制字符转义为 `\uXXXX`，在新实现中这类字符需要在 while 循环的 switch default 分支中，通过额外的 `if (c < 0x20)` 检查处理。若控制字符频繁出现，需权衡是否值得为此引入额外逻辑分支。

---

## 执行顺序建议

1. **优化二**（5 分钟，零风险）：先做，热身，顺手
2. **优化一**（30 分钟，需仔细调整 WritePropertyValue / WriteException 的逗号逻辑）：核心改动，完成后跑测试确认 JSON 格式正确
3. **优化三**（1–2 小时，含测试补充）：代码量最大，需处理代理对边界情况，建议独立提交

每项完成后均需运行 `dotnet test` 确认 `LogJsonWriterTests` 全量通过，并在可行时重新运行 `LogWriterBenchmarks` 对比前后数据。
