using System.Text.Json;

namespace CentrED; 

public class ConfigRoot {
    public string ActiveProfile { get; set; } = "";
    public string ServerConfigPath { get; set; } = "cedserver.xml";
}

public static class Config {

    private static ConfigRoot _configRoot;
    private static string _configFilePath = "settings.json";
    
    static Config() {
        if (!File.Exists(_configFilePath)) {
            var newConfig = new ConfigRoot();
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(newConfig));
        }

        var jsonText = File.ReadAllText(_configFilePath);
            _configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonText);
    }

    public static void Save() {
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(_configRoot));
    }

    public static string ActiveProfile {
        get => _configRoot.ActiveProfile;
        set => _configRoot.ActiveProfile = value;
    }
    
    public static string ServerConfigPath {
        get => _configRoot.ServerConfigPath;
        set => _configRoot.ServerConfigPath = value;
    }
}