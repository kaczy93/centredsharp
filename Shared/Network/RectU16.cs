using System.Buffers;
using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct RectU16
{
    public const int SIZE = 8;

    public RectU16(ushort x1, ushort y1, ushort x2, ushort y2)
    {
        X1 = Math.Min(x1, x2);
        X2 = Math.Max(x1, x2);
        Y1 = Math.Min(y1, y2);
        Y2 = Math.Max(y1, y2);
    }

    public ushort X1;
    public ushort X2;
    public ushort Y1;
    public ushort Y2;

    public int Width => Math.Max(X1,X2) - Math.Min(X1,X2) + 1;
    public int Height => Math.Max(Y1,Y2) - Math.Min(Y1,Y2) + 1;

    public bool Contains(uint x, uint y)
    {
        return x >= X1 && x <= X2 && y >= Y1 && y <= Y2;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(X1);
        writer.Write(Y1);
        writer.Write(X2);
        writer.Write(Y2);
    }

    public override string ToString()
    {
        return $"({X1}, {Y1})/({X2}, {Y2})";
    }

    public static RectU16 operator /(RectU16 a, int value)
    {
        return new RectU16
            ((ushort)(a.X1 / value), (ushort)(a.Y1 / value), (ushort)(a.X2 / value), (ushort)(a.Y2 / value));
    }

    public IEnumerable<(ushort x, ushort y)> Iterate()
    {
        for (ushort x = X1; x <= X2; x++)
        {
            for (ushort y = Y1; y <= Y2; y++)
            {
                yield return (x, y);
            }
        }
    }
}

public static class SpanReaderRect
{
    public static RectU16 ReadRectU16(this ref SpanReader reader)
    {
        return new RectU16(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());   
    }
}