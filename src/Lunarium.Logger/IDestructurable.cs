using System.Buffers;

namespace Lunarium.Logger;

/// <summary>
/// 标识该对象支持高性能解构方案。
/// 通过实现此接口，对象可以直接提供其 UTF-8 编码的字节表示（通常为 JSON 块），
/// 从而绕过反射和 JsonSerializer 的开销。
/// </summary>
public interface IDestructurable
{
    /// <summary>
    /// 返回对象的字节表示。对于 JSON 写入器，这应是一个标准的 UTF-8 JSON 对象/片段。
    /// 建议实现：返回静态数组、缓存的字节块或常量，以实现 0 分配。
    /// </summary>
    /// <returns>包含对象预序列化内容的只读内存区域。</returns>
    void Destructure(DestructureHelper helper);

}

public interface IDestructured
{
    /// <summary>
    /// 返回对象的字节表示。对于 JSON 写入器，这应是一个标准的 UTF-8 JSON 对象/片段。
    /// 建议实现：返回静态数组、缓存的字节块或常量，以实现 0 分配。
    /// </summary>
    /// <returns>包含对象预序列化内容的只读内存区域。</returns>
    ReadOnlyMemory<byte> Destructured();

}
