using System.Text.Json;

namespace CentrED; 

public class ConfigRoot {
    public string? ActiveProfile { get; set; }
}

public static class Config {

    private static ConfigRoot _configRoot;
    private static string _filePath = "settings.json";
    
    static Config() {
        if (!File.Exists(_filePath)) {
            var newConfig = new ConfigRoot();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(newConfig));
        }

        var jsonText = File.ReadAllText(_filePath);
            _configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonText);
    }

    public static string ActiveProfile => _configRoot.ActiveProfile ?? "";
}