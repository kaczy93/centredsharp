using System.Text.Json;
using CentrED.IO.Models;
using Microsoft.Xna.Framework.Input;

namespace CentrED;

public class ConfigRoot
{
    public string ActiveProfile = "";
    public string ServerConfigPath = "cedserver.xml";
    public bool PreferTexMaps;
    public bool LegacyMouseScroll;
    public bool Viewports;
    public string GraphicsDriver = "D3D11";
    public Dictionary<string, WindowState> Layout = new();
    public Dictionary<string, (Keys[], Keys[])> Keymap = new();
    public int FontSize = 13;
}

public static class Config
{
    private static readonly TimeSpan ConfigSaveRate = TimeSpan.FromSeconds(30);
    private static DateTime LastConfigSave = DateTime.Now;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };
    
    public static ConfigRoot Instance;
    private static string _configFilePath = "settings.json";

    public static void Initialize()
    {
        if (!File.Exists(_configFilePath))
        {
            var newConfig = new ConfigRoot();
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(newConfig));
        }

        var jsonText = File.ReadAllText(_configFilePath);
        Instance = JsonSerializer.Deserialize<ConfigRoot>(jsonText, SerializerOptions);
        Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", Instance.GraphicsDriver);
    }

    public static void AutoSave()
    {
        if (DateTime.Now > LastConfigSave + ConfigSaveRate)
        {
            Save();
        }
    }

    public static void Save()
    {
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(Instance, SerializerOptions));
        LastConfigSave = DateTime.Now;
    }
}