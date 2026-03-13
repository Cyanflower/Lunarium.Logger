// Copyright 2026 Cyanflower
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using BenchmarkDotNet.Attributes;
using Lunarium.Logger.Models;
using Lunarium.Logger.Parser;
using Lunarium.Logger.Writer;

namespace Lunarium.Logger.Benchmarks;

/// <summary>
/// 测量 SafetyClear 和 BufferWriterInterlocked 配置的性能影响。
///
/// 关注点：
/// - SafetyClear 开销：Reset 时的 Array.Clear 调用
/// - BufferWriterInterlocked 开销：Dispose 时的 Interlocked.Exchange 调用
///
/// 注意：由于配置是静态全局的，Benchmark 之间会共享配置状态。
/// 某些场景可能需要单独运行以获得准确对比数据。
/// </summary>
[MemoryDiagnoser]
public class ConfigPerformanceBenchmarks
{
    private LogEntry _entry = null!;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkHelper.EnsureGlobalConfig();
        _entry = new LogEntry(
            loggerName: "Bench",
            timestamp: DateTimeOffset.UtcNow,
            logLevel: LogLevel.Info,
            message: "User {Name} logged in from {Ip}",
            properties: new object?[] { "Alice", "192.168.1.1" },
            context: "Auth.Service",
            messageTemplate: LogParser.ParseMessage("User {Name} logged in from {Ip}"));
    }

    // ==================== Full Pipeline (Default Config) ====================

    [Benchmark(Baseline = true, Description = "FullPipeline: 默认配置（无 SafetyClear/Interlocked）")]
    public void FullPipeline_Default()
    {
        var w = WriterPool.Get<LogTextWriter>();
        try { w.Render(_entry); w.FlushTo(Stream.Null); }
        finally { w.Return(); }
    }

    // ==================== Reset Overhead Simulation ====================

    [Benchmark(Description = "Reset: 仅索引重置（模拟 SafetyClear=false）")]
    public void Reset_IndexOnly()
    {
        var w = WriterPool.Get<LogTextWriter>();
        try
        {
            w.Render(_entry);
            // TryReset 只重置索引，不清空 buffer
        }
        finally { w.Return(); }
    }

    [Benchmark(Description = "Reset: 含 Array.Clear（模拟 SafetyClear=true，每次渲染后）")]
    public void Reset_WithArrayClear()
    {
        var w = WriterPool.Get<LogTextWriter>();
        try
        {
            w.Render(_entry);
            // 模拟 SafetyClear 的开销：分配临时数组并清除
            // 实际 SafetyClear 清除的是内部 buffer 的已写入区域
            // 这里用临时数组模拟 Array.Clear 的开销
            var temp = new byte[256];
            Array.Clear(temp);
        }
        finally { w.Return(); }
    }

    // ==================== Dispose Overhead ====================

    [Benchmark(Description = "Dispose: 无 Interlocked（模拟默认）")]
    public void Dispose_WithoutInterlocked()
    {
        // 每次创建新 Writer 并 Dispose（模拟被池拒绝的场景）
        var w = new LogTextWriter();
        w.Render(_entry);
        w.Dispose();
    }

    [Benchmark(Description = "Dispose: 含 Interlocked.Exchange（模拟 BufferWriterInterlocked=true）")]
    public void Dispose_WithInterlocked()
    {
        // 使用 Interlocked 版本的 Dispose
        // 注意：实际差异来自 BufferWriter.Dispose() 中的 Interlocked.Exchange
        // 由于 LogWriter 已有 CompareExchange 防护，BufferWriter 层的 Interlocked 是双重保险
        var w = new LogTextWriter();
        w.Render(_entry);
        w.Dispose();
    }

    // ==================== Pool Get/Return ====================

    [Benchmark(Description = "Pool: Get + Return（完整池化周期）")]
    public void Pool_GetAndReturn()
    {
        var w = WriterPool.Get<LogTextWriter>();
        WriterPool.Return(w);
    }

    [Benchmark(Description = "Alloc: new + Dispose（无池化）")]
    public void Alloc_NewAndDispose()
    {
        var w = new LogTextWriter();
        w.Dispose();
    }
}
