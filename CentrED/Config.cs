using System.Text.Json;
using CentrED.IO.Models;

namespace CentrED;

public class ConfigRoot
{
    public string ActiveProfile = "";
    public string ServerConfigPath = "cedserver.xml";
    public bool PreferTexMaps;
    public Dictionary<string, WindowState> Layout = new();
}

public static class Config
{
    public static ConfigRoot Instance;
    private static string _configFilePath = "settings.json";
    
    static Config()
    {
        if (!File.Exists(_configFilePath))
        {
            var newConfig = new ConfigRoot();
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(newConfig));
        }

        var jsonText = File.ReadAllText(_configFilePath);
        Instance = JsonSerializer.Deserialize<ConfigRoot>(jsonText);
    }

    public static void Save()
    {
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(Instance));
    }
}