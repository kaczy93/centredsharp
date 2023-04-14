﻿using System.Xml;
using System.Xml.Serialization;
using Server;
using Shared;

namespace Cedserver;

[XmlRoot]
public static class Config {
    private static XmlSerializer _xmlSerializer = new(typeof(CEDConfig));

    private static string _Path;

    private static CEDConfig _CedConfig;
    public static int Version => _CedConfig.Version;
    public static bool CentrEdPlus => _CedConfig.CentrEdPlus;
    public static int Port => _CedConfig.Port;
    public static Map Map => _CedConfig.Map;
    public static string Tiledata => _CedConfig.Tiledata;
    public static string Radarcol => _CedConfig.Radarcol;
    public static Autobackup Autobackup => _CedConfig.AutoBackup;
    public static List<Account> Accounts => _CedConfig.Accounts;
    public static List<Region> Regions => _CedConfig.Regions;

    public static string DefaultPath =>
        Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));

    public static void Read() {
        using var reader = new FileStream(_Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _CedConfig = (CEDConfig)_xmlSerializer.Deserialize(reader);

        if (_CedConfig.Version != CEDConfig.CurrentVersion) {
            _CedConfig.Version = CEDConfig.CurrentVersion;
            Invalidate(); // fill in missing entries with default values
            Flush();
        }
    }

    public static void Invalidate() {
        Changed = true;
    }

    public static void Flush() {
        if (Changed) {
            using var writer = new FileStream(_Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var writerSettings = new XmlWriterSettings {
                Indent = true,
            };
            using var xmlWriter = XmlWriter.Create(writer, writerSettings);
            _xmlSerializer.Serialize(xmlWriter, _CedConfig);
            Changed = false;
        }
    }

    public static bool Changed { get; set; }

    public static void Init(string[] args) {
        var index = Array.IndexOf(args, "-c");
        if (index != -1) {
            _Path = args[index + 1];
        }
        else if (args.Length == 1) {
            _Path = args[0];
        }
        else {
            _Path = DefaultPath;
        }
        Console.WriteLine($"Config file: {_Path}");

        if (File.Exists(_Path)) {
            Read();
        }
        else {
            Prompt();
        }
    }

    private static void Prompt() {
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
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort width)) {
            _CedConfig.Map.Width = width;
        }

        Console.Write($"Map height [{_CedConfig.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort height)) {
            _CedConfig.Map.Height = height;
        }

        Console.WriteLine("Admin account");
        Console.WriteLine("=============");
        Console.Write($"Account name: ");
        var accountName = Console.ReadLine();

        Console.Write($"Password [hidden]: ");
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

        _CedConfig.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator));
        Invalidate();
        Flush();
    }
}