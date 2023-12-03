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
        var favoritesPath = Path.Join(profileDir, "favorites.json");
        if (File.Exists(favoritesPath))
        {
            var favorites = JsonSerializer.Deserialize<Dictionary<string, RadarFavorite>>(File.ReadAllText(favoritesPath));
            if (favorites != null)
            {
                profile.RadarFavorites = favorites;
            }
        }
        return profile;
    }
}