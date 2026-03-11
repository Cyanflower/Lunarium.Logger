using Lunarium.Logger.Models;
using Lunarium.Logger.Target;

namespace Lunarium.Logger.Benchmarks;

/// <summary>
/// 不输出任何内容的 NullTarget。
/// 用于隔离测量 Logger 管道开销（Channel 写入、过滤、分发），排除 I/O 噪声。
/// </summary>
internal sealed class NullTarget : ILogTarget
{
    public void Emit(LogEntry entry) { }
    public void Dispose() { }
}

/// <summary>
/// Benchmark 公共辅助工具。
/// </summary>
internal static class BenchmarkHelper
{
    /// <summary>
    /// 确保全局配置已初始化。
    /// GlobalConfigurator 只能配置一次（进程级），所有 Benchmark 类共享同一配置。
    /// 在每个 [GlobalSetup] 方法的第一行调用即可。
    /// </summary>
    public static void EnsureGlobalConfig()
        => GlobalConfigurator.ApplyDefaultIfNotConfigured();
}
