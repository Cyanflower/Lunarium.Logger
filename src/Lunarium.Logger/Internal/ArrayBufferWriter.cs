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
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lunarium.Logger.Internal;

internal sealed class BufferWriter : IBufferWriter<byte>, IDisposable
{
    private static readonly int MAX_ARRAY_SIZE = Array.MaxLength;
    private byte[] _buffer;
    private int _index;

    internal BufferWriter(int capacity = 4096)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(capacity);
        _index = 0;
    }

    // ===== 低级 IBufferWriter<byte> API =====

    internal int Capacity => _buffer.Length;
    internal int WrittenCount => _index;

    /// <summary>已写入字节数，等价于 WrittenCount。</summary>
    internal int Length => _index;

    /// <summary>已写入内容的只读视图，零拷贝。</summary>
    internal ReadOnlySpan<byte> WrittenSpan => new ReadOnlySpan<byte>(_buffer, 0, _index);

    /// <summary>按字节索引访问已写内容（只读）。用于 JSON 尾部逗号检查等场景。</summary>
    internal byte this[int index] => _buffer[index];

    public void Advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (_index > _buffer.Length - count) throw new InvalidOperationException("Writing beyond the buffer zone boundary");
        _index += count;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint > 0 && _buffer.Length - _index < sizeHint)
            EnsureCapacity(sizeHint);
        else if (_index == _buffer.Length)
            EnsureCapacity(1);
        return _buffer.AsSpan(_index);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint > 0 && _buffer.Length - _index < sizeHint)
            EnsureCapacity(sizeHint);
        else if (_index == _buffer.Length)
            EnsureCapacity(1);
        return _buffer.AsMemory(_index);
    }

    // ===== 高级写入 API =====

    /// <summary>将字符串编码为 UTF-8 追加到缓冲区。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Append(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AppendSpan(value.AsSpan());
    }

    /// <summary>将 char span 编码为 UTF-8 追加到缓冲区。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendSpan(ReadOnlySpan<char> chars)
    {
        if (chars.IsEmpty) return;
        int maxBytes = Encoding.UTF8.GetMaxByteCount(chars.Length);
        EnsureCapacity(maxBytes);
        int written = Encoding.UTF8.GetBytes(chars, _buffer.AsSpan(_index));
        _index += written;
    }

    /// <summary>追加单个字符（ASCII 走快速路径，无需 Encoding 调用）。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Append(char c)
    {
        if (c < 0x80)
        {
            EnsureCapacity(1);
            _buffer[_index++] = (byte)c;
            return;
        }
        Span<char> single = stackalloc char[1];
        single[0] = c;
        AppendSpan(single);
    }

    /// <summary>追加任意对象（调用 ToString() 后编码为 UTF-8）。用于 Exception 等场景。</summary>
    internal void Append(object? value)
    {
        if (value is null) return;
        Append(value.ToString());
    }

    /// <summary>追加换行符（Environment.NewLine）。</summary>
    internal void AppendLine()
    {
        Append(Environment.NewLine);
    }

    /// <summary>
    /// 格式化后追加。等价于 string.Format(InvariantCulture, format, arg)，适用于带对齐/格式的复合格式字符串。
    /// </summary>
    internal void AppendFormat(string format, object? arg)
    {
        Append(string.Format(CultureInfo.InvariantCulture, format, arg));
    }

    /// <summary>
    /// 将 IUtf8SpanFormattable 直接格式化为 UTF-8 字节写入缓冲区，无中间字符串分配。
    /// 适用于 int/long/double/DateTimeOffset 等所有实现该接口的类型（.NET 8+）。
    /// </summary>
    internal void AppendFormattable<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        int sizeHint = 64;
        while (true)
        {
            EnsureCapacity(sizeHint);
            if (value.TryFormat(_buffer.AsSpan(_index), out int written, format, CultureInfo.InvariantCulture))
            {
                _index += written;
                return;
            }
            sizeHint *= 2;
        }
    }

    // ===== 缓冲区修改 =====

    /// <summary>
    /// 移除末尾 count 个字节。用于去除 JSON 尾部逗号（单字节 ASCII）等场景，O(1)。
    /// </summary>
    internal void RemoveLast(int count = 1)
    {
        _index -= count;
    }

    /// <summary>
    /// 移除指定位置的字节段。末尾移除退化为 O(1)；中间移除需内存移动。
    /// </summary>
    internal void Remove(int startIndex, int count)
    {
        if (startIndex + count == _index)
        {
            _index -= count;
            return;
        }
        Buffer.BlockCopy(_buffer, startIndex + count, _buffer, startIndex, _index - startIndex - count);
        _index -= count;
    }

    // ===== 输出 =====

    /// <summary>将已写内容直接写入流，零拷贝，无分配。</summary>
    internal void FlushTo(Stream stream) => stream.Write(_buffer, 0, _index);

    /// <summary>将已写内容解码为字符串（UTF-8 → string），供 StringChannelTarget 使用。</summary>
    public override string ToString() => Encoding.UTF8.GetString(_buffer, 0, _index);

    // ===== 池化 & 生命周期 =====

    internal void Reset()
    {
        if (SafetyClearConfig.SafetyClear)
            _buffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    public void Dispose()
    {
        byte[] buffer;
        if (AtomicOpsConfig.BufferWriterDisposeInterlocked)
            buffer = Interlocked.Exchange(ref _buffer, null!);
        else
        {
            buffer = _buffer;
            _buffer = null!;
        }

        if (buffer != null)
        {
            if (SafetyClearConfig.SafetyClear)
                buffer.AsSpan(0, _index).Clear();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void EnsureCapacity(int sizeHint)
    {
        int request = sizeHint == 0 ? 1 : sizeHint;
        if (_index + request > _buffer.Length)
        {
            int newSize = Math.Max(_buffer.Length * 2, _index + request);
            if ((uint)newSize > MAX_ARRAY_SIZE)
            {
                newSize = Math.Max(_index + request, MAX_ARRAY_SIZE);
                if ((uint)newSize > MAX_ARRAY_SIZE)
                    throw new OutOfMemoryException("Requested buffer size exceeds maximum array length.");
            }
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            _buffer.AsSpan(0, _index).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }
    }
}
