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
