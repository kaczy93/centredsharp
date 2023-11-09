using System.Xml.Serialization;

namespace CentrED.Server.Config;

public class Autobackup
{
    [XmlElement] public bool Enabled { get; set; } = false;
    [XmlElement] public string Directory { get; set; } = "backups";
    [XmlElement] public uint MaxBackups { get; set; } = 7;
    [XmlElement] public TimeSpan Interval { get; set; } = TimeSpan.FromHours(12);
}