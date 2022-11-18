using System.Xml;
using System.Xml.Serialization;
using Shared;

namespace Cedserver.Config; 

[XmlRoot]
public class CEDConfig {
    private static XmlSerializer _xmlSerializer = new(typeof(CEDConfig));

    [XmlAttribute] public int Version { get; set; } = 3;
    [XmlElement] public int Port { get; set; } = 2597;

    [XmlElement]
    public Map Map { get; set; } = new();

    [XmlElement] public string Tiledata { get; set; } = "tiledata.mul";
    [XmlElement] public string Radarcol { get; set; } = "radarcol.mul";
    [XmlArray]
    public List<Account> Accounts { get; set; } = new();

    [XmlArray]
    public List<Region> Regions { get; set; } = new();

    private string _Path;
    
    public CEDConfig() : this(DefaultPath){}
    
    public CEDConfig(string path) {
        _Path = path;
    }

    public static string DefaultPath => Path.GetFullPath(Path.ChangeExtension(Server.GetCurrentExecutable(), ".xml"));
    public static CEDConfig Read(string path = "") {
        if (string.IsNullOrEmpty(path))
            path = DefaultPath;
        using var reader = new FileStream(path, FileMode.Open);
        var result = (CEDConfig)_xmlSerializer.Deserialize(reader);
        result._Path = path;
        return result;
    }

    public void Write() {
        using var writer = new FileStream(_Path, FileMode.Create);
        var writerSettings = new XmlWriterSettings {
            Indent = true,
        };
        using var xmlWriter = XmlWriter.Create(writer, writerSettings);
        _xmlSerializer.Serialize(xmlWriter, this);
    }
    
    public static CEDConfig Init() {
        string? input;
        CEDConfig result = new CEDConfig(DefaultPath);
        Console.WriteLine("Configuring Network");
        Console.WriteLine("===================");
        Console.Write($"Port [{result.Port}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int port)) {
            result.Port = port;
        }
        
        Console.WriteLine("Configuring Paths");
        Console.WriteLine("=================");
        Console.Write($"map [{result.Map.MapPath}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            result.Map.MapPath = input;
        }
        Console.Write($"statics [{result.Map.Statics}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            result.Map.Statics = input;
        }
        Console.Write($"staidx [{result.Map.StaIdx}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            result.Map.StaIdx = input;
        }
        Console.Write($"tiledata [{result.Tiledata}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            result.Tiledata = input;
        }
        Console.Write($"radarcol [{result.Radarcol}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            result.Radarcol = input;
        }
        
        Console.WriteLine("Parameters");
        Console.WriteLine("==========");
        Console.Write($"Map width [{result.Map.Width}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int width)) {
            result.Map.Width = width;
        }
        Console.Write($"Map height [{result.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int height)) {
            result.Map.Height = height;
        }
        
        Console.WriteLine("Admin account");
        Console.WriteLine("=============");
        Console.Write($"Account name: ");
        var accountName = Console.ReadLine();

        Console.Write($"Password [hidden]: ");
        string password = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace)
                password = password.Remove(password.Length - 1);
            else {
                password += key.KeyChar;
            }
        }
        
        result.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
        result.Write();
        return result;
    }

    public override string ToString() {
        return $"{nameof(Version)}: {Version}, " +
               $"{nameof(Port)}: {Port}, " +
               $"{nameof(Map)}: {Map}, " +
               $"{nameof(Tiledata)}: {Tiledata}, " +
               $"{nameof(Radarcol)}: {Radarcol}, " +
               $"{nameof(Accounts)}: [{String.Join(", ", Accounts)}] " +
               $"{nameof(Regions)}: [{String.Join(",", Regions)}]";
    }
}