using System.Xml;

namespace CentrED.Server.Config;

public class Map
{
    public string MapPath { get; set; } = "map0.mul";
    public string StaIdx { get; set; } = "staidx0.mul";
    public string Statics { get; set; } = "statics0.mul";
    public ushort Width { get; set; } = 896;
    public ushort Height { get; set; } = 512;

    internal void Write(XmlWriter writer)
    {
        writer.WriteStartElement("Map");
        writer.WriteElementString("Map", MapPath);
        writer.WriteElementString("StaIdx", StaIdx);
        writer.WriteElementString("Statics", Statics);
        writer.WriteElementString("Width", XmlConvert.ToString(Width));
        writer.WriteElementString("Height", XmlConvert.ToString(Height));
        writer.WriteEndElement();
    }

    internal static Map Read(XmlReader reader)
    {
        var result = new Map();
        using XmlReader sub = reader.ReadSubtree();
        sub.Read();
        while (sub.Read())
        {
            if (sub.NodeType == XmlNodeType.Element)
            {
                switch (sub.Name)
                {
                    case "Map":
                        result.MapPath = sub.ReadElementContentAsString();
                        break;
                    case "StaIdx":
                        result.StaIdx = sub.ReadElementContentAsString();
                        break;
                    case "Statics":
                        result.Statics = sub.ReadElementContentAsString();
                        break;
                    case "Width":
                        result.Width = (ushort)sub.ReadElementContentAsInt();
                        break;
                    case "Height":
                        result.Height = (ushort)sub.ReadElementContentAsInt();
                        break;
                }
            }
        }
        return result;
    }

    public override string ToString()
    {
        return
            $"{nameof(MapPath)}: {MapPath}, {nameof(StaIdx)}: {StaIdx}, {nameof(Statics)}: {Statics}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
    }
}