using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED.IO.Models;

public class Profile
{
    private const string PROFILE_FILE = "profile.json";
    private const string LOCATIONS_FILE = "locations.json";
    private const string LAND_TILE_SETS_FILE = "landtilesets.json";
    private const string STATIC_TILE_SETS_FILE = "statictilesets.json";
    private const string SEQUENTIAL_LAND_TILE_SETS_FILE = "sequential_landtilesets.json";
    private const string SEQUENTIAL_STATIC_TILE_SETS_FILE = "sequential_statictilesets.json";
    private const string HUE_SETS_FILE = "huesets.json";
    private const string LAND_BRUSH_FILE = "landbrush.json";
    
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
    [JsonIgnore] public Dictionary<string, List<ushort>> SequentialLandTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, List<ushort>> SequentialStaticTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> HueSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, LandBrush> LandBrush { get; set; } = new();
    

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
        File.WriteAllText(Path.Join(profileDir, SEQUENTIAL_LAND_TILE_SETS_FILE), JsonSerializer.Serialize(SequentialLandTileSets, options));
        File.WriteAllText(Path.Join(profileDir, SEQUENTIAL_STATIC_TILE_SETS_FILE), JsonSerializer.Serialize(SequentialStaticTileSets, options));
        File.WriteAllText(Path.Join(profileDir, HUE_SETS_FILE), JsonSerializer.Serialize(HueSets, options));
        File.WriteAllText(Path.Join(profileDir, LAND_BRUSH_FILE), JsonSerializer.Serialize(LandBrush, Models.LandBrush.JsonOptions));
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
        
        var favorites = Deserialize<Dictionary<string, RadarFavorite>>(Path.Join(profileDir, LOCATIONS_FILE));
        if (favorites != null)
            profile.RadarFavorites = favorites;
        
        var landTileSets = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, LAND_TILE_SETS_FILE));
        if (landTileSets != null)
            profile.LandTileSets = landTileSets;
        
        var staticTileSets = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, STATIC_TILE_SETS_FILE));
        if (staticTileSets != null)
            profile.StaticTileSets = staticTileSets;
            
        // Change the sequence deserialization to use List instead of SortedSet
        var sequentialLandTileSets = Deserialize<Dictionary<string, List<ushort>>>(Path.Join(profileDir, SEQUENTIAL_LAND_TILE_SETS_FILE));
        if (sequentialLandTileSets != null)
            profile.SequentialLandTileSets = sequentialLandTileSets;
            
        var sequentialStaticTileSets = Deserialize<Dictionary<string, List<ushort>>>(Path.Join(profileDir, SEQUENTIAL_STATIC_TILE_SETS_FILE));
        if (sequentialStaticTileSets != null)
            profile.SequentialStaticTileSets = sequentialStaticTileSets;
        
        var huesets = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, HUE_SETS_FILE));
        if (huesets != null)
            profile.HueSets = huesets;
        
        var landBrush = Deserialize<Dictionary<string, LandBrush>>(Path.Join(profileDir, LAND_BRUSH_FILE), Models.LandBrush.JsonOptions);
        if (landBrush != null)
            profile.LandBrush = landBrush;
        
        return profile;
    }

    private static T? Deserialize<T>(string filePath)
    {
        return Deserialize<T>(filePath, JsonSerializerOptions.Default);
    }
    private static T? Deserialize<T>(string filePath, JsonSerializerOptions options)
    {
        if (!File.Exists(filePath))
            return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath), options);
    }
}