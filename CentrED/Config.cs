using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED; 

public class ConfigRoot {
    [JsonInclude]
    public string clientPath;
    [JsonInclude]
    public string clientVersion;
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

    public static string ClientPath => _configRoot.clientPath;
    public static string ClientVersion => _configRoot.clientVersion;
}