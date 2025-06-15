/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanReader.cs                                                   *
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
using System.Text;

namespace System.Buffers;

public ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _buffer;

    public int Length { get; }
    public int Position { get; private set; }
    public int Remaining => Length - Position;

    public ReadOnlySpan<byte> Buffer => _buffer;

    public SpanReader(ReadOnlySpan<byte> span)
    {
        _buffer = span;
        Position = 0;
        Length = span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (Position >= Length)
        {
            throw new OutOfMemoryException();
        }

        return _buffer[Position++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean() => ReadByte() > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        if (!BinaryPrimitives.TryReadInt16LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 2;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 2;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        if (!BinaryPrimitives.TryReadInt32LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        if (!BinaryPrimitives.TryReadUInt32LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 4;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        if (!BinaryPrimitives.TryReadInt64LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        if (!BinaryPrimitives.TryReadUInt64LittleEndian(_buffer[Position..], out var value))
        {
            throw new OutOfMemoryException();
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        bool isFixedLength = fixedLength > -1;

        var remaining = Remaining;
        int size = remaining;
        if (isFixedLength)
        {
            size = fixedLength;
            if (size > Remaining)
            {
                throw new OutOfMemoryException();
            }
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOf((byte)0);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + 1;

        // The string is either as long as the first terminator character, remaining buffer size, or fixed length.
        return Encoding.ASCII.GetString(span);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadStringFixed(int fixedLength) => ReadString(fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin)
    {
        Debug.Assert(
            origin != SeekOrigin.End || offset <= 0,
            "Attempting to seek to a position beyond capacity using SeekOrigin.End"
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
            origin != SeekOrigin.Begin || offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Begin"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || Position + offset >= 0,
            "Attempting to seek to a negative position using SeekOrigin.Current"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || Position + offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Current"
        );

        return Position = Math.Max(0, origin switch
        {
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End     => _buffer.Length + offset,
            _                  => offset // Begin
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Read(Span<byte> bytes)
    {
        if (bytes.Length < Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        return _buffer.TryCopyTo(bytes);
    }
}
