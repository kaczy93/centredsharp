using System.Xml.Serialization;

namespace Cedserver; 

public class Region {
    public Region() : this("") {
        
    }
    
    public Region(string name, List<Rect>? area = null) {
        Name = name;
        Area = area ?? new List<Rect>();
    }

    [XmlElement]
    public string Name { get; set; }
    [XmlArray]
    public List<Rect> Area { get; set; }

    public override string ToString() {
        return $"{nameof(Name)}: {Name}, {nameof(Area)}: [{String.Join(",", Area)}]";
    }
}