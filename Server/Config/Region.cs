using System.Xml.Serialization;

namespace Cedserver.Config; 

public class Region {
    [XmlElement]
    public string Name { get; set; }
    [XmlArray]
    public List<Rect> Area { get; set; }

    public override string ToString() {
        return $"{nameof(Name)}: {Name}, {nameof(Area)}: [{String.Join(",", Area)}]";
    }
}