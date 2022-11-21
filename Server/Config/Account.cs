using System.Xml.Serialization;
using Shared;

namespace Cedserver; 

public class Account {

    public Account() : this("", "", Shared.AccessLevel.None) {
        
    }
    public Account(string accountName, string password, AccessLevel accessLevel) {
        Name = accountName;
        PasswordHash = Crypto.Md5Hash(password);
        AccessLevel = accessLevel;
        LastPos = new LastPos();
        Regions = new List<string>();
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
    public List<String> Regions { get; set; }

    public override string ToString() {
        return $"{nameof(Name)}: {Name}, " +
               $"{nameof(PasswordHash)}: [redacted], " +
               $"{nameof(AccessLevel)}: {AccessLevel}, " +
               $"{nameof(LastPos)}: {LastPos}, " +
               $"{nameof(Regions)}: {String.Join(",", Regions)}";
    }

    public void UpdatePassword(string password) {
        throw new NotImplementedException();
    }

    public void Invalidate() {
        throw new NotImplementedException();
    }

    public bool CheckPassword(string password) {
        throw new NotImplementedException();
    }
}