using System.Xml.Serialization;

namespace CentrED.IO.Models.Centredplus;

[XmlRoot("TilesBrush")]
public class TilesBrush
{
    [XmlElement] public List<Brush> Brush;
}

public class Brush
{
    [XmlAttribute] public String Id;
    [XmlAttribute] public String Name;
    [XmlElement] public List<Land> Land;
    [XmlElement] public List<Edge> Edge;
}

public class Land
{
    [XmlAttribute] public String ID;
    [XmlAttribute] public String Chance;
}

public class Edge
{
    [XmlAttribute] public String To;
    [XmlElement] public List<EdgeLand> Land;
}

public class EdgeLand
{
    [XmlAttribute] public String Type;
    [XmlAttribute] public String ID;
}