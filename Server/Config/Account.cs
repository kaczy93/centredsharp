using System.Xml.Serialization;

namespace Cedserver.Config; 

public class Account {
    [XmlElement]
    public string Name { get; set; }
    [XmlElement]
    public string PasswordHash { get; set; }
    [XmlElement]
    public int AccessLevel { get; set; }
    [XmlElement]
    public LastPos LastPos { get; set; }
    [XmlArray("Regions")]
    [XmlArrayItem("Region")]
    public List<String> Regions { get; set; }
}