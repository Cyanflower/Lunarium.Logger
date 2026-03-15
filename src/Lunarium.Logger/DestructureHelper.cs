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
using System.Text.Json;
using Lunarium.Logger.Internal;

namespace Lunarium.Logger;

public class DestructureHelper
{
    private BufferWriter _bufferWriter;

    private Utf8JsonWriter _serializerWriter;

    internal DestructureHelper(
        BufferWriter bufferWriter,
        bool bufferWriterIsMainWriter,
        Utf8JsonWriter jsonWriter,
        bool jsonWriterIsMainWriter)
    {
        _bufferWriter = bufferWriter;
        _serializerWriter = jsonWriter;
        if (!bufferWriterIsMainWriter)
        {
            _bufferWriter.Reset();
        }
        if (!jsonWriterIsMainWriter)
        {
            _serializerWriter.Reset(bufferWriter);
        }
    }

    public void WriteStartObject()
    {
        _serializerWriter.WriteStartObject();
    }

    public void WriteEndObject()
    {
        _serializerWriter.WriteEndObject();
    }

    public void WriteStartArray()
    {
        _serializerWriter.WriteStartArray();
    }

    public void WriteEndArray()
    {
        _serializerWriter.WriteEndArray();
    }

    public void WritePropertyName(string name)
    {
        _serializerWriter.WritePropertyName(name);
    }

    public void WritePropertyName(ReadOnlySpan<char> name)
    {
        _serializerWriter.WritePropertyName(name);
    }

    public void WritePropertyName(JsonEncodedText name)
    {
        _serializerWriter.WritePropertyName(name);
    }

    #region Write Value Methods

    public void WriteStringValue(string value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteStringValue(ReadOnlySpan<char> value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteStringValue(JsonEncodedText value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteStringValue(DateTime value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteStringValue(DateTimeOffset value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteStringValue(Guid value)
    {
        _serializerWriter.WriteStringValue(value);
    }

    public void WriteNumberValue(int value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(long value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(uint value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(ulong value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(float value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(double value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(decimal value)
    {
        _serializerWriter.WriteNumberValue(value);
    }

    public void WriteBooleanValue(bool value)
    {
        _serializerWriter.WriteBooleanValue(value);
    }

    public void WriteNullValue()
    {
        _serializerWriter.WriteNullValue();
    }

    #endregion

    #region Write Field Methods (Property Name + Value)

    public void WriteString(string name, string value)
    {
        _serializerWriter.WriteString(name, value);
    }

    public void WriteString(JsonEncodedText name, string value)
    {
        _serializerWriter.WriteString(name, value);
    }

    public void WriteString(string name, DateTime value)
    {
        _serializerWriter.WriteString(name, value);
    }

    public void WriteString(string name, DateTimeOffset value)
    {
        _serializerWriter.WriteString(name, value);
    }

    public void WriteString(string name, Guid value)
    {
        _serializerWriter.WriteString(name, value);
    }

    public void WriteNumber(string name, int value)
    {
        _serializerWriter.WriteNumber(name, value);
    }

    public void WriteNumber(string name, long value)
    {
        _serializerWriter.WriteNumber(name, value);
    }

    public void WriteNumber(string name, double value)
    {
        _serializerWriter.WriteNumber(name, value);
    }

    public void WriteBoolean(string name, bool value)
    {
        _serializerWriter.WriteBoolean(name, value);
    }

    public void WriteNull(string name)
    {
        _serializerWriter.WriteNull(name);
    }

    #endregion

    public void WriteRawValue(ReadOnlySpan<byte> utf8Json)
    {
        _serializerWriter.WriteRawValue(utf8Json);
    }

    internal ReadOnlySpan<byte> WrittenSpan => _bufferWriter.WrittenSpan;

    internal bool TryFlush()
    {
        try
        {
            if (_serializerWriter.CurrentDepth == 0)
            {
                _serializerWriter.Flush();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            InternalLogger.Error(ex);
            return false;
        }
    }

    internal void Dispose(bool resetBufferWriter, bool resetJsonWriter)
    {
        if (resetBufferWriter)
        {
            _bufferWriter.Reset();
        }
        if (resetJsonWriter)
        {
            _serializerWriter.Reset(Stream.Null);
        }
        _bufferWriter = null!;
        _serializerWriter = null!;
    }
}