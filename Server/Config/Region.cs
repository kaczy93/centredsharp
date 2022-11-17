using System.Xml.Serialization;

namespace Cedserver.Config; 

public class Region {
    [XmlElement]
    public string Name { get; set; }
    [XmlArray("Area")]
    [XmlArrayItem("Rect")]
    public List<Rect> Area { get; set; }
}