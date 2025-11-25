using System.Xml;
using CentrED.Utility;

namespace CentrED.Server.Config;

public class Account(string name, string password, AccessLevel accessLevel, List<String> regions)
{
    public Account() : this("","", AccessLevel.None, [])
    {
    }
    
    public string Name { get; set; } = name;
    public string PasswordHash { get; set; } = Crypto.Md5Hash(password);
    public AccessLevel AccessLevel { get; set; } = accessLevel;
    public LastPos LastPos { get; set; } = new();
    public List<string> Regions { get; set; } = regions;
    public DateTime LastLogon { get; set; } = DateTime.MinValue;

    public void UpdatePassword(string password)
    {
        PasswordHash = Crypto.Md5Hash(password);
    }

    public bool CheckPassword(string password)
    {
        return PasswordHash.Equals(Crypto.Md5Hash(password), StringComparison.InvariantCultureIgnoreCase);
    }
    
    internal void Write(XmlWriter writer)
    {
        writer.WriteStartElement("Account");
        writer.WriteElementString("Name", Name);
        writer.WriteElementString("PasswordHash", PasswordHash);
        writer.WriteElementString("AccessLevel", XmlConvert.ToString((int)AccessLevel));
        
        writer.WriteStartElement("LastPos");
        writer.WriteAttributeString("x", XmlConvert.ToString(LastPos.X));
        writer.WriteAttributeString("y", XmlConvert.ToString(LastPos.Y));
        writer.WriteEndElement(); //LastPos
        
        writer.WriteStartElement("Regions");
        foreach (var region in Regions)
            writer.WriteElementString("Region", region);
        writer.WriteEndElement(); //Regions
        
        writer.WriteElementString("LastLogon", XmlConvert.ToString(LastLogon, XmlDateTimeSerializationMode.Local));
        
        writer.WriteEndElement(); //Account
    }

    internal static Account Read(XmlReader reader)
    {
        var result = new Account();
        using XmlReader sub = reader.ReadSubtree();
        sub.Read();
        while (sub.Read())
        {
            if (sub.NodeType == XmlNodeType.Element)
            {
                switch (sub.Name)
                {
                    case "Name":
                        result.Name = sub.ReadElementContentAsString();
                        break;
                    case "PasswordHash":
                        result.PasswordHash = sub.ReadElementContentAsString();
                        break;
                    case "AccessLevel":
                        result.AccessLevel = (AccessLevel)sub.ReadElementContentAsInt();
                        break;
                    case "LastPos":
                        var x = XmlConvert.ToUInt16(sub.GetAttribute("x") ?? "0");
                        var y = XmlConvert.ToUInt16(sub.GetAttribute("y") ?? "0");
                        result.LastPos = new LastPos(x, y);
                        break;
                    case "Region":
                        result.Regions.Add(sub.ReadElementContentAsString());
                        break;
                    case "LastLogon":
                        result.LastLogon = sub.ReadElementContentAsDateTime();
                        break;
                }
            }
        }
        return result;
    }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, " + $"{nameof(PasswordHash)}: [redacted], " +
               $"{nameof(AccessLevel)}: {AccessLevel}, " + $"{nameof(LastPos)}: {LastPos}, " +
               $"{nameof(Regions)}: {String.Join(",", Regions)}";
    }
}