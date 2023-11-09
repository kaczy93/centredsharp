using System.Xml.Serialization;

namespace CentrED.Server.Config;

public class Map
{
    [XmlElement("Map")] public string MapPath { get; set; } = "map0.mul";
    [XmlElement] public string StaIdx { get; set; } = "staidx0.mul";
    [XmlElement] public string Statics { get; set; } = "statics0.mul";
    [XmlElement] public ushort Width { get; set; } = 896;
    [XmlElement] public ushort Height { get; set; } = 512;

    public override string ToString()
    {
        return
            $"{nameof(MapPath)}: {MapPath}, {nameof(StaIdx)}: {StaIdx}, {nameof(Statics)}: {Statics}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
    }
}