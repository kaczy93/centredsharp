/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanWriter.cs                                                   *
 *                                                                       *
 * This file is dual-licensed under the terms of the GNU General Public  *
 * License v3.0, and the MIT License for use with CentredSharp.          *
 * You may use this file under the terms of either license.              *
 *                                                                       *
 * GNU GENERAL PUBLIC LICENSE NOTICE:                                    *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 *                                                                       *
 * MIT LICENSE NOTICE:                                                   *
 * Permission is hereby granted, free of charge, to any person obtaining *
 * a copy of this software and associated documentation files (the       *
 * "Software"), to deal in the Software without restriction, including   *
 * without limitation the rights to use, copy, modify, merge, publish,   *
 * distribute, sublicense, and/or sell copies of the Software, and to    *
 * permit persons to whom the Software is furnished to do so, subject to *
 * the following conditions:                                             *
 *                                                                       *
 * The above copyright notice and this permission notice shall be        *
 * included in all copies or substantial portions of the Software.       *
 *                                                                       *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,       *
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF    *
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.*
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY  *
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,  *
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE     *
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                *
 *************************************************************************/

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Server.Buffers;

namespace System.Buffers;

public ref struct SpanWriter
{
    private readonly bool _resize;
    private byte[] _arrayToReturnToPool;
    private Span<byte> _buffer;
    private int _position;

    public int BytesWritten { get; private set; }

    public int Position
    {
        get => _position;
        private set
        {
            _position = value;

            if (value > BytesWritten)
            {
                BytesWritten = value;
            }
        }
    }

    public int Capacity => _buffer.Length;

    public ReadOnlySpan<byte> Span => _buffer[..Position];

    public Span<byte> RawBuffer => _buffer;

    /**
         * Converts the writer to a Span<byte> using a SpanOwner.
         * If the buffer was stackalloc, it will be copied to a rented buffer.
         * Otherwise the existing rented buffer is used.
         *
         * Note:
         * Do not use the SpanWriter after calling this method.
         * This method will effectively dispose of the SpanWriter and is therefore considered terminal.
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanOwner ToSpan()
    {
        var toReturn = _arrayToReturnToPool;

        SpanOwner apo;
        if (_position == 0)
        {
            apo = new SpanOwner(_position, Array.Empty<byte>());
            if (toReturn != null)
            {
                STArrayPool<byte>.Shared.Return(toReturn);
            }
        }
        else if (toReturn != null)
        {
            apo = new SpanOwner(_position, toReturn);
        }
        else
        {
            var buffer = STArrayPool<byte>.Shared.Rent(_position);
            _buffer.CopyTo(buffer);
            apo = new SpanOwner(_position, buffer);
        }

        this = default; // Don't allow two references to the same buffer
        return apo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriter(Span<byte> initialBuffer, bool resize = false)
    {
        _resize = resize;
        _buffer = initialBuffer;
        _position = 0;
        BytesWritten = 0;
        _arrayToReturnToPool = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriter(int initialCapacity, bool resize = false)
    {
        _resize = resize;
        _arrayToReturnToPool = STArrayPool<byte>.Shared.Rent(initialCapacity);
        _buffer = _arrayToReturnToPool;
        _position = 0;
        BytesWritten = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacity)
    {
        var newSize = Math.Max(BytesWritten + additionalCapacity, _buffer.Length * 2);
        byte[] poolArray = STArrayPool<byte>.Shared.Rent(newSize);

        _buffer[..BytesWritten].CopyTo(poolArray);

        byte[] toReturn = _arrayToReturnToPool;
        _buffer = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfNeeded(int count)
    {
        if (_position + count > _buffer.Length)
        {
            if (!_resize)
            {
                throw new OutOfMemoryException();
            }

            Grow(count);
        }
    }

    public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(_buffer);

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _buffer.Length)
        {
            if (!_resize)
            {
                throw new OutOfMemoryException();
            }

            Grow(capacity - BytesWritten);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(bool value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = *(byte*)&value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer[_position..], value);
        Position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer[_position..], value);
        Position += 2;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer[_position..], value);
        Position += 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer[_position..], value);
        Position += 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        GrowIfNeeded(8);
        BinaryPrimitives.WriteInt64LittleEndian(_buffer[_position..], value);
        Position += 8;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> buffer)
    {
        var count = buffer.Length;
        GrowIfNeeded(count);
        buffer.CopyTo(_buffer[_position..]);
        Position += count;
    }

    public void Write(ReadOnlySpan<char> value, int fixedLength = -1)
    {
        var charLength = Math.Min(fixedLength > -1 ? fixedLength : value.Length, value.Length);
        var src = value[..charLength];

        if (src.Length == 0)
        {
            return;
        }

        GrowIfNeeded(src.Length);

        var bytesWritten = Encoding.ASCII.GetBytes(src, _buffer[_position..]);
        Position += bytesWritten;

        if (fixedLength > -1)
        {
            var extra = fixedLength - bytesWritten;
            if (extra > 0)
            {
                Clear(extra);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStringNull(string value)
    {
        Write(value);
        Write((byte)0); // '\0'
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int count)
    {
        GrowIfNeeded(count);
        _buffer.Slice(_position, count).Clear();
        Position += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin)
    {
        Debug.Assert(
            origin != SeekOrigin.End || _resize || offset <= 0,
            "Attempting to seek to a position beyond capacity using SeekOrigin.End without resize"
        );

        Debug.Assert(
            origin != SeekOrigin.End || offset >= -_buffer.Length,

            "Attempting to seek to a negative position using SeekOrigin.End"
        );

        Debug.Assert(
            origin != SeekOrigin.Begin || offset >= 0,
            "Attempting to seek to a negative position using SeekOrigin.Begin"
        );

        Debug.Assert(
            origin != SeekOrigin.Begin || _resize || offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Begin without resize"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || _position + offset >= 0,
            "Attempting to seek to a negative position using SeekOrigin.Current"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || _resize || _position + offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Current without resize"
        );

        var newPosition = Math.Max(0, origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => BytesWritten + offset,
            _                  => offset // Begin
        });

        if (newPosition > _buffer.Length)
        {
            Grow(newPosition - _buffer.Length + 1);
        }

        return Position = newPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        byte[] toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    public struct SpanOwner : IDisposable
    {
        private readonly int _length;
        private readonly byte[] _arrayToReturnToPool;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SpanOwner(int length, byte[] buffer)
        {
            _length = length;
            _arrayToReturnToPool = buffer;
        }

        public Span<byte> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateSpan(ref _arrayToReturnToPool.DangerousGetReference(), _length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            byte[] toReturn = _arrayToReturnToPool;
            this = default;
            if (_length > 0)
            {
                STArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
