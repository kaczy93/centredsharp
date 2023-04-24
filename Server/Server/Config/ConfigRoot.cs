using System.Xml.Serialization;

namespace CentrED.Server; 

[XmlRoot("CEDConfig")]
public class ConfigRoot {

    [XmlIgnore] public const int CurrentVersion = 4;
    [XmlAttribute] public int Version { get; set; } = CurrentVersion;
    [XmlElement] public bool CentrEdPlus { get; set; }
    [XmlElement] public int Port { get; set; } = 2597;

    [XmlElement]
    public Map Map { get; set; } = new();

    [XmlElement] public string Tiledata { get; set; } = "tiledata.mul";
    [XmlElement] public string Radarcol { get; set; } = "radarcol.mul";
    [XmlArray]
    public List<Account> Accounts { get; set; } = new();

    [XmlArray]
    public List<Region> Regions { get; set; } = new();
    
    [XmlElement] public Autobackup AutoBackup { get; set; } = new();
    
    
    public override string ToString() {
        return $"{nameof(Version)}: {Version}, " +
               $"{nameof(Port)}: {Port}, " +
               $"{nameof(Map)}: {Map}, " +
               $"{nameof(Tiledata)}: {Tiledata}, " +
               $"{nameof(Radarcol)}: {Radarcol}, " +
               $"{nameof(Accounts)}: [{String.Join(", ", Accounts)}] " +
               $"{nameof(Regions)}: [{String.Join(",", Regions)}]" +
               $"{nameof(AutoBackup)}: {AutoBackup}";
    }
}