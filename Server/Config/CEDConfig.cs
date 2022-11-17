using System.Xml.Serialization;

namespace Cedserver.Config; 

[XmlRoot]
public class CEDConfig {
    private static XmlSerializer _xmlSerializer = new(typeof(CEDConfig));
    
    [XmlAttribute]
    public int Version { get; set; }
    [XmlElement]
    public int Port { get; set; }
    [XmlElement]
    public Map Map { get; set; }
    [XmlElement]
    public string Tiledata { get; set; }
    [XmlElement]
    public string Radarcol { get; set; }
    [XmlArray("Accounts")]
    [XmlArrayItem("Account")]
    public List<Account> Accounts { get; set; }
    [XmlArray("Regions")]
    [XmlArrayItem("Region")]
    public List<Region> Regions { get; set; }

    public static CEDConfig Read() {
        string path = Path.ChangeExtension(Server.GetCurrentExecutable(), ".xml");
        using var reader = new FileStream(path, FileMode.Open);
        return (CEDConfig)_xmlSerializer.Deserialize(reader);
    }

}