using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

namespace CentrED.Server; 

[XmlRoot("CEDConfig")]
public class Config {
    private static XmlSerializer _xmlSerializer = new(typeof(Config));
    private static string DefaultPath =>
        Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));
    
    [XmlIgnore] public const int CurrentVersion = 4;
    [XmlAttribute] public int Version { get; set; } = CurrentVersion;
    [XmlElement] public bool CentrEdPlus { get; set; }
    [XmlElement] public int Port { get; set; } = 2597;
    [XmlElement] public Map Map { get; set; } = new();
    [XmlElement] public string Tiledata { get; set; } = "tiledata.mul";
    [XmlElement] public string Radarcol { get; set; } = "radarcol.mul";
    [XmlArray] public List<Account> Accounts { get; set; } = new();
    [XmlArray] public List<Region> Regions { get; set; } = new();
    [XmlElement] public Autobackup AutoBackup { get; set; } = new();
    
    public void Invalidate() {
        Changed = true;
    }

    public void Flush() {
        if (Changed) {
            using var writer = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var writerSettings = new XmlWriterSettings {
                Indent = true,
            };
            using var xmlWriter = XmlWriter.Create(writer, writerSettings);
            _xmlSerializer.Serialize(xmlWriter, FilePath);
            Changed = false;
        }
    }

    [XmlIgnore] public bool Changed { get; set; }
    [XmlIgnore] public string FilePath { get; set; }
    
    public static Config Init(string[] args) {
        var index = Array.IndexOf(args, "-c");
        var configPath = DefaultPath;
        if (index != -1) {
            configPath = args[index + 1];
        }
        else if (args.Length == 1) {
            configPath = args[0];
        }
        Console.WriteLine($"Config file: {configPath}");

        if (File.Exists(configPath)) {
            return Read(configPath);
        }
        else {
            return Prompt(configPath);
        }

    }
    
    public static Config Read(string path) {
        using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = (Config)_xmlSerializer.Deserialize(reader)!;
        result.FilePath = path;

        if (result.Version != CurrentVersion) {
            result.Version = CurrentVersion;
            result.Invalidate(); // fill in missing entries with default values
            result.Flush();
        }

        return result;
    }

    private static Config Prompt(string path) {
        string? input;
        Config result = new() {
            FilePath = path
        };
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
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort width)) {
            result.Map.Width = width;
        }

        Console.Write($"Map height [{result.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort height)) {
            result.Map.Height = height;
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

        result.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
        result.Invalidate();
        result.Flush();
        
        return result;
    }
}