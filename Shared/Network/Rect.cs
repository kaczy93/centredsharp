using System.Buffers;
using System.Xml.Serialization;

namespace CentrED.Network;

public struct Rect
{
    public const int SIZE = 8;
    public Rect() : this(0, 0, 0, 0)
    {
    }

    public Rect(ushort x1, ushort y1, ushort x2, ushort y2)
    {
        X1 = Math.Min(x1, x2);
        X2 = Math.Max(x1, x2);
        Y1 = Math.Min(y1, y2);
        Y2 = Math.Max(y1, y2);
    }

    [XmlAttribute("x1")] public ushort X1;
    [XmlAttribute("x2")] public ushort X2;
    [XmlAttribute("y1")] public ushort Y1;
    [XmlAttribute("y2")] public ushort Y2;

    public bool Contains(uint x, uint y)
    {
        return x >= X1 && x < X2 && y >= Y1 && y < Y2;
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
}

public static class SpanReaderRect
{
    public static Rect ReadRect(this ref SpanReader reader)
    {
        return new Rect(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());   
    }
}