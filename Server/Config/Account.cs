using System.Xml.Serialization;
using CentrED.Utility;

namespace CentrED.Server.Config; 

public class Account {
    public Account() : this("") { }
    
    public Account(string accountName, string password = "", AccessLevel accessLevel = AccessLevel.None, List<string>? regions = null) {
        Name = accountName;
        PasswordHash = Crypto.Md5Hash(password);
        AccessLevel = accessLevel;
        LastPos = new LastPos();
        Regions = regions ?? new List<string>();
        LastLogon = DateTime.MinValue;
    }

    [XmlElement]
    public string Name { get; set; }
    [XmlElement]
    public string PasswordHash { get; set; }
    [XmlElement]
    public AccessLevel AccessLevel { get; set; }
    [XmlElement]
    public LastPos LastPos { get; set; }
    [XmlArray]
    [XmlArrayItem("Region")]
    public List<string> Regions { get; set; }

    [XmlElement]
    public DateTime LastLogon { get; set; }

    public override string ToString() {
        return $"{nameof(Name)}: {Name}, " +
               $"{nameof(PasswordHash)}: [redacted], " +
               $"{nameof(AccessLevel)}: {AccessLevel}, " +
               $"{nameof(LastPos)}: {LastPos}, " +
               $"{nameof(Regions)}: {String.Join(",", Regions)}";
    }

    public void UpdatePassword(string password) {
        PasswordHash = Crypto.Md5Hash(password);
    }

    public bool CheckPassword(string password) {
        return PasswordHash.Equals(Crypto.Md5Hash(password), StringComparison.InvariantCultureIgnoreCase);
    }
}