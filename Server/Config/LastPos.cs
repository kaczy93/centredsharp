using System.Xml.Serialization;

namespace Cedserver; 

public class LastPos {
    public LastPos() : this(0, 0) {
        
    }
    
    public LastPos(int x, int y) {
        X = x;
        Y = y;
    }

    [XmlAttribute("x")] public int X { get; set; }
    [XmlAttribute("y")] public int Y { get; set; }

    public override string ToString() {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }
}