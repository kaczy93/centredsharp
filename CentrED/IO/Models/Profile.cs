using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED.IO.Models;

public class Profile
{
    
    private const string PROFILE_FILE = "profile.json";
    private const string LOCATIONS_FILE = "locations.json";
    private const string LAND_TILE_SETS_FILE = "landtilesets.json";
    private const string STATIC_TILE_SETS_FILE = "statictilesets.json";
    private const string HUE_SETS_FILE = "huesets.json";
    
    [JsonIgnore] public string Name { get; set; }
    public string Hostname { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 2597;
    public string Username { get; set; } = "";
    public string ClientPath { get; set; } = "";
    public string ClientVersion { get; set; } = "";
    [JsonIgnore]
    public Dictionary<string, RadarFavorite> RadarFavorites { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> LandTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> StaticTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> HueSets { get; set; } = new();
    

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
        File.WriteAllText(Path.Join(profileDir, PROFILE_FILE), JsonSerializer.Serialize(this, options));
        File.WriteAllText(Path.Join(profileDir, LOCATIONS_FILE), JsonSerializer.Serialize(RadarFavorites, options));
        File.WriteAllText(Path.Join(profileDir, LAND_TILE_SETS_FILE), JsonSerializer.Serialize(LandTileSets, options));
        File.WriteAllText(Path.Join(profileDir, STATIC_TILE_SETS_FILE), JsonSerializer.Serialize(StaticTileSets, options));
        File.WriteAllText(Path.Join(profileDir, HUE_SETS_FILE), JsonSerializer.Serialize(HueSets, options));
    }

    public static Profile? Deserialize(string profileDir)
    {
        DirectoryInfo dir = new DirectoryInfo(profileDir);
        if (!dir.Exists)
            return null;

        var profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(Path.Join(profileDir, PROFILE_FILE)));
        if (profile == null)
            return null;
        profile.Name = dir.Name;
        
        var favorites  = Deserialize<Dictionary<string, RadarFavorite>>(Path.Join(profileDir, LOCATIONS_FILE));
        if (favorites != null)
            profile.RadarFavorites = favorites;
        
        var landTileSets  = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, LAND_TILE_SETS_FILE));
        if (landTileSets != null)
            profile.LandTileSets = landTileSets;
        
        var staticTileSets  = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, STATIC_TILE_SETS_FILE));
        if (staticTileSets != null)
            profile.StaticTileSets = staticTileSets;
        
        var huesets  = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, HUE_SETS_FILE));
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