using System.Xml;
using System.Xml.Serialization;
using Server;
using Shared;

namespace Cedserver;

[XmlRoot]
public static class Config {
    private static XmlSerializer _xmlSerializer = new(typeof(CEDConfig));

    private static string _configPath = DefaultPath;

    private static CEDConfig _cedConfig = new();
    public static int Version => _cedConfig.Version;
    public static bool CentrEdPlus => _cedConfig.CentrEdPlus;
    public static int Port => _cedConfig.Port;
    public static Map Map => _cedConfig.Map;
    public static string Tiledata => _cedConfig.Tiledata;
    public static string Radarcol => _cedConfig.Radarcol;
    public static Autobackup Autobackup => _cedConfig.AutoBackup;
    public static List<Account> Accounts => _cedConfig.Accounts;
    public static List<Region> Regions => _cedConfig.Regions;

    public static string DefaultPath =>
        Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));

    public static void Read() {
        using var reader = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _cedConfig = (CEDConfig)_xmlSerializer.Deserialize(reader)!;

        if (_cedConfig.Version != CEDConfig.CurrentVersion) {
            _cedConfig.Version = CEDConfig.CurrentVersion;
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
            _xmlSerializer.Serialize(xmlWriter, _cedConfig);
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
        _cedConfig = new CEDConfig();
        Console.WriteLine("Configuring Network");
        Console.WriteLine("===================");
        Console.Write($"Port [{_cedConfig.Port}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int port)) {
            _cedConfig.Port = port;
        }

        Console.WriteLine("Configuring Paths");
        Console.WriteLine("=================");
        Console.Write($"map [{_cedConfig.Map.MapPath}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _cedConfig.Map.MapPath = input;
        }

        Console.Write($"statics [{_cedConfig.Map.Statics}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _cedConfig.Map.Statics = input;
        }

        Console.Write($"staidx [{_cedConfig.Map.StaIdx}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _cedConfig.Map.StaIdx = input;
        }

        Console.Write($"tiledata [{_cedConfig.Tiledata}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _cedConfig.Tiledata = input;
        }

        Console.Write($"radarcol [{_cedConfig.Radarcol}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input)) {
            _cedConfig.Radarcol = input;
        }

        Console.WriteLine("Parameters");
        Console.WriteLine("==========");
        Console.Write($"Map width [{_cedConfig.Map.Width}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort width)) {
            _cedConfig.Map.Width = width;
        }

        Console.Write($"Map height [{_cedConfig.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort height)) {
            _cedConfig.Map.Height = height;
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

        _cedConfig.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
        Invalidate();
        Flush();
    }
}