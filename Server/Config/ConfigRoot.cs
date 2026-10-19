using System.Xml;

namespace CentrED.Server.Config;

public class ConfigRoot
{
    private static string DefaultPath =>
        Path.GetFullPath(Path.ChangeExtension(Application.GetCurrentExecutable(), ".xml"));

    public const int CurrentVersion = 5;
    public int Version { get; set; } = CurrentVersion;
    public bool CentrEdPlus { get; set; }
    public int Port { get; set; } = 2597;
    public Map Map { get; set; } = new();
    public string Tiledata { get; set; } = "tiledata.mul";
    public string Radarcol { get; set; } = "radarcol.mul";
    public string Hues { get; set; } = "hues.mul";
    public List<Account> Accounts { get; set; } = new();
    public List<Region> Regions { get; set; } = new();
    public Autobackup AutoBackup { get; set; } = new();
    
    public bool Changed { get; set; }
    public string FilePath { get; set; } = DefaultPath;

    public void Invalidate()
    {
        Changed = true;
    }

    public void Flush()
    {
        if (!Changed)
            return;
        
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = System.Text.Encoding.UTF8
        };
        
        using XmlWriter writer = XmlWriter.Create(FilePath, settings);
        writer.WriteStartDocument();
        Write(writer);
        writer.WriteEndDocument();
        Changed = false;
    }

    public static ConfigRoot Init(string[] args)
    {
        var index = Array.IndexOf(args, "-c");
        var configPath = DefaultPath;
        if (index != -1)
        {
            configPath = args[index + 1];
        }
        else if (args.Length == 1)
        {
            configPath = args[0];
        }
        Console.WriteLine($"Config file: {configPath}");

        if (File.Exists(configPath))
        {
            return Read(configPath);
        }
        else
        {
            return Prompt(configPath);
        }
    }

    public static ConfigRoot Read(string path)
    {
        using var reader = XmlReader.Create(path);
        var result = Read(reader);

        if (result.Version != CurrentVersion)
        {
            result.Version = CurrentVersion;
            result.Invalidate(); // fill in missing entries with default values
            result.Flush();
        }
        
        result.Regions.RemoveAll(r => string.IsNullOrEmpty(r.Name));
        result.Accounts.RemoveAll(a => string.IsNullOrEmpty(a.Name));
        
        result.FilePath = path;
        return result;
    }

    private static ConfigRoot Prompt(string path)
    {
        string? input;
        ConfigRoot result = new()
        {
            FilePath = path
        };
        Console.WriteLine("Configuring Network");
        Console.WriteLine("===================");
        Console.Write($"Port [{result.Port}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && Int32.TryParse(input, out int port))
        {
            result.Port = port;
        }

        Console.WriteLine("Configuring Paths");
        Console.WriteLine("=================");
        Console.Write($"map [{result.Map.MapPath}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
        {
            result.Map.MapPath = input;
        }

        Console.Write($"statics [{result.Map.Statics}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
        {
            result.Map.Statics = input;
        }

        Console.Write($"staidx [{result.Map.StaIdx}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
        {
            result.Map.StaIdx = input;
        }

        Console.Write($"tiledata [{result.Tiledata}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
        {
            result.Tiledata = input;
        }

        Console.Write($"radarcol [{result.Radarcol}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
        {
            result.Radarcol = input;
        }

        Console.WriteLine("Parameters");
        Console.WriteLine("==========");
        Console.Write($"Map width [{result.Map.Width}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort width))
        {
            result.Map.Width = width;
        }

        Console.Write($"Map height [{result.Map.Height}]: ");
        input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input) && UInt16.TryParse(input, out ushort height))
        {
            result.Map.Height = height;
        }

        Console.WriteLine("Admin account");
        Console.WriteLine("=============");
        Console.Write("Account name: ");
        var accountName = Console.ReadLine()!;

        Console.Write("Password [hidden]: ");
        string password = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password = password.Remove(password.Length - 1);
            }
            else
            {
                password += key.KeyChar;
            }
        }

        result.Accounts.Add(new Account(accountName, password, AccessLevel.Administrator, []));
        result.Invalidate();
        result.Flush();

        return result;
    }
    
    internal void Write(XmlWriter writer)
    {
        writer.WriteStartElement("CEDConfig");
        writer.WriteAttributeString("Version", XmlConvert.ToString(CurrentVersion));
            
        writer.WriteElementString("CentrEdPlus", XmlConvert.ToString(CentrEdPlus));
        writer.WriteElementString("Port", XmlConvert.ToString(Port));
        Map.Write(writer);
        writer.WriteElementString("Tiledata", Tiledata);
        writer.WriteElementString("Radarcol", Radarcol);
        writer.WriteElementString("Hues", Hues);
        
        writer.WriteStartElement("Accounts");
        foreach (var account in Accounts)
        {
            account.Write(writer);
        }
        writer.WriteEndElement();
        writer.WriteStartElement("Regions");
        foreach (var region in Regions)
        {
            region.Write(writer);
        }
        writer.WriteEndElement();
        AutoBackup.Write(writer);

        writer.WriteEndElement();
    }

    internal static ConfigRoot Read(XmlReader reader)
    {
        ConfigRoot result = new ConfigRoot();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "CEDConfig":
                        result.Version = XmlConvert.ToInt32(reader.GetAttribute("version") ?? CurrentVersion.ToString());
                        break;

                    case "CentrEdPlus":
                        result.CentrEdPlus = reader.ReadElementContentAsBoolean();
                        break;

                    case "Port":
                        result.Port = reader.ReadElementContentAsInt();
                        break;

                    case "Map":
                        result.Map = Map.Read(reader);
                        break;

                    case "Tiledata":
                        result.Tiledata = reader.ReadElementContentAsString();
                        break;

                    case "Radarcol":
                        result.Radarcol = reader.ReadElementContentAsString();
                        break;

                    case "Hues":
                        result.Hues = reader.ReadElementContentAsString();
                        break;

                    case "Account":
                        result.Accounts.Add(Account.Read(reader));
                        break;

                    case "Region":
                        result.Regions.Add(Region.Read(reader));
                        break;

                    case "AutoBackup":
                        result.AutoBackup = Autobackup.Read(reader);
                        break;
                }
            }
        }
        return result;
    }
}