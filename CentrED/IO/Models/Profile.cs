using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED.IO.Models;

public class Profile
{
    [JsonIgnore] public string Name { get; set; }
    public string Hostname { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 2597;
    public string Username { get; set; } = "";
    public string ClientPath { get; set; } = "";
    public string ClientVersion { get; set; } = "";
    [JsonIgnore]
    public Dictionary<string, RadarFavorite> RadarFavorites { get; set; } = new();
    [JsonIgnore] public Dictionary<string, HashSet<ushort>> TileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, HashSet<ushort>> HueSets { get; set; } = new();
    

    public void Serialize(String path)
    {
        var profileDir = Path.Join(path, Name);
        if (!Directory.Exists(profileDir))
        {
            Directory.CreateDirectory(profileDir);
        }
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(Path.Join(profileDir, "profile.json"), JsonSerializer.Serialize(this, options));
        File.WriteAllText(Path.Join(profileDir, "favorites.json"), JsonSerializer.Serialize(RadarFavorites, options));
        File.WriteAllText(Path.Join(profileDir, "tilesets.json"), JsonSerializer.Serialize(TileSets, options));
        File.WriteAllText(Path.Join(profileDir, "huesets.json"), JsonSerializer.Serialize(HueSets, options));
    }

    public static Profile? Deserialize(string profileDir)
    {
        DirectoryInfo dir = new DirectoryInfo(profileDir);
        if (!dir.Exists)
            return null;

        var profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(Path.Join(profileDir, "profile.json")));
        if (profile == null)
            return null;
        profile.Name = dir.Name;
        
        var favorites  = Deserialize<Dictionary<string, RadarFavorite>>(Path.Join(profileDir, "favorites.json"));
        if (favorites != null)
            profile.RadarFavorites = favorites;
        
        var tilesets  = Deserialize<Dictionary<string, HashSet<ushort>>>(Path.Join(profileDir, "tilesets.json"));
        if (tilesets != null)
            profile.TileSets = tilesets;
        
        var huesets  = Deserialize<Dictionary<string, HashSet<ushort>>>(Path.Join(profileDir, "huesets.json"));
        if (huesets != null)
            profile.HueSets = huesets;
        
        return profile;
    }

    private static T? Deserialize<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
    }

}