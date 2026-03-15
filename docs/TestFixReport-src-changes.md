# 测试修复报告：src-changes.diff 引发的编译错误

> 生成日期：2026-03-15
> 关联 diff：`src-changes.diff`
> 错误列表：`errorlist.txt`

---

## 错误概览

| 类别 | 根本原因 | 受影响文件数 | 修复性质 |
|------|----------|-------------|---------|
| A | `LogEntry` 构造函数新增 `contextBytes`/`scope` 参数 | 13 个调用位置 | 机械补参数 |
| B | `ILogger.Log()` 参数顺序变更（`ex` 与 `message` 对调） | 5 个测试文件 | 改为命名参数；部分需核查断言值 |
| C | `ILogger` 新增 `GetContext()`/`GetContextSpan()` 接口成员 | 1 个文件 | 更新 `CapturingLogger` 实现 |
| D | `AtomicOpsConfig`/`SafetyClearConfig`/`BufferWriterDiagnostics` 不存在 | 3 个文件 | 直接删除相关测试 |

---

## Error Category A：`LogEntry` 构造函数缺少 `contextBytes` 和 `scope` 参数

### 原因

`LogEntry` 在 `context` 和 `messageTemplate` 之间新插入了两个必须参数：

```csharp
// 新构造函数签名
public LogEntry(
    string loggerName,
    DateTimeOffset timestamp,
    LogLevel logLevel,
    string message,
    object?[] properties,
    string context,
    ReadOnlyMemory<byte> contextBytes,   // ← 新增
    string scope,                        // ← 新增
    MessageTemplate messageTemplate,
    Exception? exception = null)
```

- `contextBytes`：`Context` 字符串的 UTF-8 预编码缓存，供 Writer 层零分配写入
- `scope`：MEL 适配器的日志作用域信息（EventId/CategoryName）

所有直接 `new LogEntry(...)` 的测试均报"未提供所需参数 contextBytes/scope 对应的参数"。

### 修复方式

在所有 `new LogEntry(...)` 调用处补两个默认值：

```csharp
contextBytes: default,   // 等价于 ReadOnlyMemory<byte>.Empty
scope: "",
```

### 受影响位置（13 处）

| 文件 | 行号 |
|------|------|
| `tests/Lunarium.Logger.IntegrationTests/ConsoleSink/ConsoleSinkTests.cs` | 55 |
| `tests/Lunarium.Logger.Tests/FileSink/FileSinkTests.cs` | 62, 139, 312 |
| `tests/Lunarium.Logger.Tests/Core/LoggerCoverageGapTests.cs` | 33 |
| `tests/Lunarium.Logger.Tests/Filter/LoggerFilterTests.cs` | 33 |
| `tests/Lunarium.Logger.Tests/Integration/LoggerIntegrationTests.cs` | 251, 287 |
| `tests/Lunarium.Logger.Tests/Models/LogConfigTests.cs` | 227 |
| `tests/Lunarium.Logger.Tests/Target/ChannelTargetTests.cs` | 28 |
| `tests/Lunarium.Logger.Tests/Writer/LogColorTextWriterTests.cs` | 44 |
| `tests/Lunarium.Logger.Tests/Writer/LogJsonWriterTests.cs` | 43 |
| `tests/Lunarium.Logger.Tests/Writer/LogTextWriterTests.cs` | 41 |
| `tests/Lunarium.Logger.Tests/Writer/LogWriterTests.cs` | 48 |
| `tests/Lunarium.Logger.Tests/Writer/WriterCoverageGapTests.cs` | 40 |

> **注**：`LoggerIntegrationTests.cs:287` 还有位置参数错乱的额外问题（第 7 个位置参数传入了 `MessageTemplate` 但应为 `ReadOnlyMemory<byte>`），建议改为全命名参数形式。

### 对原测试的影响

纯机械补参数，原有测试逻辑和断言完全不变。

---

## Error Category B：`ILogger.Log()` 参数顺序变更导致位置参数错乱

### 原因

`Log()` 签名发生了**破坏性变更**：`ex` 和 `message` 位置对调，同时新增了 `contextBytes` 和 `scope`：

```csharp
// 旧签名
void Log(LogLevel level, string message = "", string context = "", Exception? ex = null, params object?[] propertyValues);

// 新签名
void Log(LogLevel level, Exception? ex = null, string message = "", string context = "",
         ReadOnlyMemory<byte> contextBytes = default, string scope = "", params object?[] propertyValues);
```

所有旧代码中按**位置**传参的调用均受影响：

| 错误模式 | 示例 | 表现 |
|----------|------|------|
| `string` → `Exception?` | `Log(level, "msg")` | 第2位置参数 `"msg"` 传给了 `ex` |
| `object?[]` → `ReadOnlyMemory<byte>` | NSubstitute验证中 `Arg.Any<object?[]>()` 在第5位置 | 第5位置现在是 `contextBytes` |
| `Exception` → `string` | `Log(level, "msg", "ctx", ex, args)` | `ex` 传到了 `context`（`string`）位置 |

### 修复方式

所有 `Log()` 调用（包括 NSubstitute 验证中的 `mock.Received().Log(...)`）均改为**命名参数**：

```csharp
// 旧
logger.Log(LogLevel.Info, $"Hello {id}");
// 新
logger.Log(LogLevel.Info, message: $"Hello {id}");

// 旧（带 context 和 exception）
wrapper.Log(LogLevel.Error, "msg", "", ex);
// 新
wrapper.Log(LogLevel.Error, ex: ex, message: "msg");

// 旧（NSubstitute 验证）
mock.Received().Log(LmLogLevel.Info, "Hello world", Arg.Any<string>(), null, Arg.Any<object?[]>());
// 新
mock.Received().Log(LmLogLevel.Info, ex: null, message: "Hello world",
                    context: Arg.Any<string>(), propertyValues: Arg.Any<object?[]>());
```

### 受影响文件

| 文件 | 行号 | 错误类型 |
|------|------|----------|
| `tests/Lunarium.Logger.Tests/Core/LoggerCoreTests.cs` | 71, 92, 107, 150, 201, 202 | `string` → `Exception?` |
| `tests/Lunarium.Logger.Tests/Core/LoggerCoverageGapTests.cs` | 66, 67, 119 | `string` → `Exception?` |
| `tests/Lunarium.Logger.Tests/LoggerExtensionsTests.cs` | 55, 72 | `string` → `Exception?` |
| `tests/Lunarium.Logger.Tests/Extensions/MicrosoftLoggingBridgeTests.cs` | 74,77,95,98,117,119,120,158,161,175,178,193,196 | 混合（NSubstitute验证） |
| `tests/Lunarium.Logger.Tests/Wrapper/LoggerWrapperTests.cs` | 46,50,52,53,61,65,67,68,79,83,85,86,100,106,108,109,120,124,126,127,139,143,145,146,157,161,164 | 混合（直接调用+NSubstitute验证） |

### ⚠️ LoggerWrapperTests.cs 存在超出机械修复的语义变化

`LoggerWrapper` 的 context 拼接时机发生了变化：

```csharp
// 旧：Log() 调用时动态拼接
var finalContext = string.IsNullOrEmpty(context) ? _context : $"{_context}.{context}";

// 新：构造时就拼接到 _context
internal LoggerWrapper(ILogger logger, string context)
{
    _context = $"{logger.GetContext()}.{context}";  // ← 构造时调用 GetContext()
    _contextBytes = Encoding.UTF8.GetBytes(_context);
}
```

`Substitute.For<ILogger>()` 的 `GetContext()` 默认返回 `null`，因此：
- `new LoggerWrapper(innerMock, "A")` → `_context = ".A"`（而非 `"A"`）
- 嵌套两层：`_context = "..A.B"`（而非 `"A.B"`）

测试中所有对 context 拼接结果（`"A.B"`、`"A.B.C"` 等）的期望值需要重新核实。**这不是纯机械修复**，需要根据实际业务需求决定：
- 如果 `NSubstitute` mock 的 `GetContext()` 应该被 setup 为返回合理值（如 `""`），则需要在测试中添加 `innerMock.GetContext().Returns("")`
- 如果 wrapper 的测试场景应该从空 context 开始，则期望值需要从 `"A.B"` 改为 `"A.B"`（去掉前导 `.`）

### 对原测试的影响

参数重映射后测试意图不变；但 `LoggerWrapperTests.cs` 中关于 Context 拼接结果的断言值需要额外审查与修正。

---

## Error Category C：`ILoggerDefaultMethodTests.CapturingLogger` 未实现新接口成员

### 原因

`ILogger` 新增了两个**没有默认实现**的接口方法，所有实现类必须提供：

```csharp
string GetContext();
ReadOnlyMemory<byte> GetContextSpan();
```

同时 `Log()` 签名也已变更。`CapturingLogger` 实现了旧接口，现在三处均不满足编译要求。

### 修复方式

更新 `CapturingLogger` 及其 `LogCall` record：

```csharp
private sealed class CapturingLogger : ILogger
{
    // 调整字段顺序以匹配新的 Log() 参数顺序
    public record LogCall(LogLevel Level, Exception? Ex, string Message, string Context, object?[] Props);
    public List<LogCall> Calls { get; } = [];

    public string GetContext() => "";
    public ReadOnlyMemory<byte> GetContextSpan() => ReadOnlyMemory<byte>.Empty;

    public void Log(LogLevel level, Exception? ex = null, string message = "", string context = "",
                    ReadOnlyMemory<byte> contextBytes = default, string scope = "", params object?[] propertyValues)
        => Calls.Add(new(level, ex, message, context, propertyValues));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

同时需要逐一检查同文件中各 `Fact` 内访问 `LogCall` 字段的代码（`.Ex`、`.Message`、`.Context` 字段名因 record 定义顺序调整而需核对）。

### 对原测试的影响

非纯机械修复：需要逐一核对各 Fact 的字段访问与新 `LogCall` record 对齐。测试逻辑本身（验证 DIM 是否正确 forward 到 `Log()`）不变。

---

## Error Category D：`AtomicOpsConfig` / `SafetyClearConfig` / `BufferWriterDiagnostics` 不存在

### 原因

这三个类型是**期间添加又移除的设计**，由于加了又删了导致 diff 中完全没有记录，但测试代码中已经引用了它们：

- `AtomicOpsConfig`：控制 `BufferWriter` 原子 Dispose 保护（防止双重 `ArrayPool.Return`）
- `SafetyClearConfig`：控制返回 `ArrayPool` 前是否清零内存
- `BufferWriterDiagnostics`：诊断工具，用于检测双重 Return 事件

这些测试是为未实现（且已放弃）的功能写的，**直接删除**。

### 受影响测试（全部删除）

**`GlobalConfiguratorTests.cs`** — 删除 Section 12 的全部 5 个 Fact（第 464-523 行）：
- `EnableBufferWriterInterlocked_SetsFlagTrue`
- `DisableBufferWriterInterlocked_SetsFlagFalse`
- `EnableSafetyClear_SetsFlagTrue`
- `DisableSafetyClear_SetsFlagFalse`
- `Config_SafetyAndInterlocked_ChainedCorrectly`

同时删除 `ResetAll()` 中引用这两个类型的两行：
```csharp
_bufferWriterInterlockedField?.SetValue(null, false);  // 删除
_safetyClearField?.SetValue(null, false);               // 删除
```
以及文件顶部对应的两个 `FieldInfo` 字段声明。

**`LoggerConcurrencyTests.cs`** — 删除 `WithSafetyEnabled_NoDoubleReturn` Fact（第 94-127 行），同时修复 `ResetAll()` 方法（删除第 38-39 行的 `AtomicOpsConfig`/`SafetyClearConfig` 调用）。

**`BufferWriterTests.cs`** — 删除 `Dispose_WithInterlockedEnabled_ShouldWork` Fact（第 115-144 行），同时删除文件顶部的 `AtomicOpsConfig` 类型引用声明（第 122 行）。

### 对原测试的影响

删除这些测试后，其余测试逻辑不受任何影响。

---

## 新增字段/接口的补充测试建议

| 新增内容 | 是否需要新测试 | 说明 |
|----------|--------------|------|
| `LogEntry.ContextBytes` | 不急需 | 纯内部优化字段，值来自 `Context` UTF-8 编码，无独立逻辑 |
| `LogEntry.Scope` | **建议添加** | `MicrosoftLoggingBridgeTests.cs` 中应验证 MEL 适配器填充的 `scope` 值能正确传递到 `LogEntry` |
| `ILogger.GetContext()` / `GetContextSpan()` | **应添加** | `LoggerCoreTests.cs` 验证 `GetContext()` == `loggerName`；`LoggerWrapperTests.cs` 验证返回拼接后的 context |
| `ByteChannelTarget`（全新 Target） | **必须添加** | `ChannelTargetTests.cs` 中完全没有覆盖，功能全新 |
| `IDestructurable` / `IDestructured` / `DestructureHelper`（全新接口） | **建议添加** | 全新自定义解构路径，无任何测试覆盖 |
| `WriterPool.MarkAsActive()` / `DisposeAndReturnArrayBuffer()` | 建议添加 | 新的安全回收路径，影响对象池内存安全正确性 |
| `Filter.cs` TryAdd+Interlocked 计数优化 | 低优先级 | 现有测试覆盖功能行为，并发边界场景可选 |
| 各 `TextBytes`、`FormatString`、`OriginalMessageBytes` 等内部 byte 字段 | 不需要 | 内部优化字段，已被 Writer 渲染结果的现有测试间接验证 |
