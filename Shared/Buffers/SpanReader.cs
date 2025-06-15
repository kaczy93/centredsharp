/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanReader.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
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
