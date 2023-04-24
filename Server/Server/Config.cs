using System.Xml;
using System.Xml.Serialization;

namespace CentrED.Server;

[XmlRoot]
public static class Config {
    private static XmlSerializer _xmlSerializer = new(typeof(ConfigRoot));

    private static string _configPath = DefaultPath;

    private static ConfigRoot _configRoot = new();
    public static int Version => _configRoot.Version;
    public static bool CentrEdPlus => _configRoot.CentrEdPlus;
    public static int Port => _configRoot.Port;
    public static Map Map => _configRoot.Map;
    public static string Tiledata => _configRoot.Tiledata;
    public static string Radarcol => _configRoot.Radarcol;
    public static Autobackup Autobackup => _configRoot.AutoBackup;
    public static List<Account> Accounts => _configRoot.Accounts;
    public static List<Region> Regions => _configRoot.Regions;

    public static Account? GetAccount(string name) {
        return Accounts.Find(a => a.Name == name);
    }
    
    public static Region? GetRegion(string name) {
        return Regions.Find(a => a.Name == name);
    }

    public static string DefaultPath =>
        Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));

    public static void Read() {
        using var reader = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _configRoot = (ConfigRoot)_xmlSerializer.Deserialize(reader)!;

        if (_configRoot.Version != ConfigRoot.CurrentVersion) {
            _configRoot.Version = ConfigRoot.CurrentVersion;
            Invalidate(); // fill in missing entries with default values
            Flush();
        }
    }

    public static void Invalidate() {
        Changed = true;
    }

    public static void Flush() {
        if (Changed) {
            using var writer = new FileStream(_configPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var writerSettings = new XmlWriterSettings {
                Indent = true,
            };
            using var xmlWriter = XmlWriter.Create(writer, writerSettings);
            _xmlSerializer.Serialize(xmlWriter, _configRoot);
            Changed = false;
        }
    }

    public static bool Changed { get; set; }

    public static void Init(string[] args) {
        var index = Array.IndexOf(args, "-c");
        if (index != -1) {
            _configPath = args[index + 1];
        }
        else if (args.Length == 1) {
            _configPath = args[0];
        }
        Console.WriteLine($"Config file: {_configPath}");

        if (File.Exists(_configPath)) {
            Read();
        }
        else {
            Prompt();
        }
    }

    private static void Prompt() {
        string? input;
        _configRoot = new ConfigRoot();
        Console.WriteLine("Configuring Network");
        Console.WriteLine("===================");
        Console.Write($"Port [{_configRoot.Port}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int port)) {
            _configRoot.Port = port;
        }

        Console.WriteLine("Configuring Paths");
        Console.WriteLine("=================");
        Console.Write($"map [{_configRoot.Map.MapPath}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _configRoot.Map.MapPath = input;
        }

        Console.Write($"statics [{_configRoot.Map.Statics}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _configRoot.Map.Statics = input;
        }

        Console.Write($"staidx [{_configRoot.Map.StaIdx}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _configRoot.Map.StaIdx = input;
        }

        Console.Write($"tiledata [{_configRoot.Tiledata}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _configRoot.Tiledata = input;
        }

        Console.Write($"radarcol [{_configRoot.Radarcol}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _configRoot.Radarcol = input;
        }

        Console.WriteLine("Parameters");
        Console.WriteLine("==========");
        Console.Write($"Map width [{_configRoot.Map.Width}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort width)) {
            _configRoot.Map.Width = width;
        }

        Console.Write($"Map height [{_configRoot.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort height)) {
            _configRoot.Map.Height = height;
        }

        Console.WriteLine("Admin account");
        Console.WriteLine("=============");
        Console.Write("Account name: ");
        var accountName = Console.ReadLine()!;

        Console.Write("Password [hidden]: ");
        string password = "";
        while (true) {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace)
                password = password.Remove(password.Length - 1);
            else {
                password += key.KeyChar;
            }
        }

        _configRoot.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
        Invalidate();
        Flush();
    }
}