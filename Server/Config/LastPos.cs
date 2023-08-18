using System.Xml.Serialization;

namespace CentrED.Server.Config; 

public class LastPos {
    public LastPos() : this(0, 0) {
        
    }
    
    public LastPos(ushort x, ushort y) {
        X = x;
        Y = y;
    }

    [XmlAttribute("x")] public ushort X { get; set; }
    [XmlAttribute("y")] public ushort Y { get; set; }

    public override string ToString() {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }
}