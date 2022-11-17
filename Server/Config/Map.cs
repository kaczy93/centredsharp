using System.Xml.Serialization;

namespace Cedserver.Config; 

public class Map {
    [XmlElement("Map")]
    public string MapPath { get; set; }
    [XmlElement]
    public string StaIdx { get; set; }
    [XmlElement]
    public string Statics { get; set; }
    [XmlElement]
    public int Width { get; set; }
    [XmlElement]
    public int Height { get; set; }
}