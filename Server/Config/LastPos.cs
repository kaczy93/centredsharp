using System.Xml.Serialization;

namespace Cedserver.Config; 

public class LastPos {
    [XmlAttribute("x")]
    public int X { get; set; }
    [XmlAttribute("y")]
    public int Y { get; set; }
}