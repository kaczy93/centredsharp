using System.Xml;
using System.Xml.Serialization;
using Server;
using Shared;

namespace Cedserver.Config; 

[XmlRoot]
public static class Config {
    private static XmlSerializer _xmlSerializer = new(typeof(CEDConfig));

    private static string _Path;

    private static CEDConfig _CedConfig;
    public static int Version => _CedConfig.Version;
    public static int Port => _CedConfig.Port;
    public static Map Map => _CedConfig.Map;
    public static string Tiledata => _CedConfig.Tiledata;
    public static string Radarcol => _CedConfig.Radarcol;
    public static List<Account> Accounts => _CedConfig.Accounts;
    public static List<Region> Regions => _CedConfig.Regions;
    
    public static string DefaultPath => Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));
    public static void Read(string path = "") {
        if (string.IsNullOrEmpty(path))
            path = DefaultPath;
        using var reader = new FileStream(path, FileMode.Open);
        _CedConfig = (CEDConfig)_xmlSerializer.Deserialize(reader);
    }

    public static void Write() {
        using var writer = new FileStream(_Path, FileMode.Create);
        var writerSettings = new XmlWriterSettings {
            Indent = true,
        };
        using var xmlWriter = XmlWriter.Create(writer, writerSettings);
        _xmlSerializer.Serialize(xmlWriter, _CedConfig);
    }
    
    public static void Init() {
        string? input;
        _CedConfig = new CEDConfig();
        Console.WriteLine("Configuring Network");
        Console.WriteLine("===================");
        Console.Write($"Port [{_CedConfig.Port}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int port)) {
            _CedConfig.Port = port;
        }
        
        Console.WriteLine("Configuring Paths");
        Console.WriteLine("=================");
        Console.Write($"map [{_CedConfig.Map.MapPath}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _CedConfig.Map.MapPath = input;
        }
        Console.Write($"statics [{_CedConfig.Map.Statics}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _CedConfig.Map.Statics = input;
        }
        Console.Write($"staidx [{_CedConfig.Map.StaIdx}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _CedConfig.Map.StaIdx = input;
        }
        Console.Write($"tiledata [{_CedConfig.Tiledata}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _CedConfig.Tiledata = input;
        }
        Console.Write($"radarcol [{_CedConfig.Radarcol}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _CedConfig.Radarcol = input;
        }
        
        Console.WriteLine("Parameters");
        Console.WriteLine("==========");
        Console.Write($"Map width [{_CedConfig.Map.Width}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int width)) {
            _CedConfig.Map.Width = width;
        }
        Console.Write($"Map height [{_CedConfig.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int height)) {
            _CedConfig.Map.Height = height;
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
        
        _CedConfig.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
    }
}