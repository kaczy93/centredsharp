using System.Xml.Serialization;
using CentrED.Network;

namespace CentrED.Server.Config;

public class Region
{
    public Region() : this("")
    {
    }

    public Region(string name, List<RectU16>? area = null)
    {
        Name = name;
        Area = area ?? new List<RectU16>();
    }

    [XmlElement] public string Name { get; set; }
    [XmlArray] public List<RectU16> Area { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Area)}: [{String.Join(",", Area)}]";
    }
}