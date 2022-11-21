using System.Xml.Serialization;

namespace Cedserver; 

public class Rect {
    public Rect() : this(0,0,0,0) {
        
    }

    public Rect(ushort x1, ushort y1, ushort x2, ushort y2) {
        X1 = Math.Min(x1, x2);
        X2 = Math.Max(x1, x2);
        Y1 = Math.Min(y1, y2);
        Y2 = Math.Max(y1, y2);
    }

    [XmlAttribute("x1")]
    public uint X1 { get; set; }
    [XmlAttribute("x2")]
    public uint X2 { get; set; }
    [XmlAttribute("y1")]
    public uint Y1 { get; set; }
    [XmlAttribute("y2")]
    public uint Y2 { get; set; }

    public override string ToString() {
        return $"{nameof(X1)}: {X1}, {nameof(X2)}: {X2}, {nameof(Y1)}: {Y1}, {nameof(Y2)}: {Y2}";
    }

    public bool Contains(uint x, uint y) {
        return x >= X1 && x < X2 && y >= Y1 && y < Y2;
    }
}