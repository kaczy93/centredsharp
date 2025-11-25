using System.Xml;

namespace CentrED.Server.Config;

public class Autobackup
{
    public bool Enabled { get; set; } = false;
    public string Directory { get; set; } = "backups";
    public uint MaxBackups { get; set; } = 7;
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(12);
    
    internal void Write(XmlWriter writer)
    {
        writer.WriteStartElement("AutoBackup");
        writer.WriteElementString("Enabled", XmlConvert.ToString(Enabled));
        writer.WriteElementString("Directory", Directory);
        writer.WriteElementString("MaxBackups", XmlConvert.ToString(MaxBackups));
        writer.WriteElementString("Interval", XmlConvert.ToString(Interval));
        writer.WriteEndElement();
    }

    internal static Autobackup Read(XmlReader reader)
    {
        var result = new Autobackup();
        using XmlReader sub = reader.ReadSubtree();
        sub.Read();
        while (sub.Read())
        {
            if (sub.NodeType == XmlNodeType.Element)
            {
                switch (sub.Name)
                {
                    case "Enabled":
                        result.Enabled = sub.ReadElementContentAsBoolean();
                        break;
                    case "Directory":
                        result.Directory = sub.ReadElementContentAsString();
                        break;
                    case "MaxBackups":
                        result.MaxBackups = XmlConvert.ToUInt32(sub.ReadElementContentAsString());
                        break;
                    case "Interval":
                        result.Interval = XmlConvert.ToTimeSpan(sub.ReadElementContentAsString());
                        break;
                }
            }
        }
        return result;
    }
}