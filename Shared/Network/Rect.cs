using System.Xml.Serialization;

namespace CentrED.Network;

public class Rect
{
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

    public Rect(BinaryReader reader)
    {
        X1 = reader.ReadUInt16();
        Y1 = reader.ReadUInt16();
        X2 = reader.ReadUInt16();
        Y2 = reader.ReadUInt16();
    }

    [XmlAttribute("x1")] public uint X1 { get; set; }
    [XmlAttribute("x2")] public uint X2 { get; set; }
    [XmlAttribute("y1")] public uint Y1 { get; set; }
    [XmlAttribute("y2")] public uint Y2 { get; set; }

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
}