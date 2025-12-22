using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED.IO.Models;

public record WallTileChance(ushort TileId, int Chance);

public record WallSet(ushort North, ushort South, List<WallTileChance> LeftTiles, List<WallTileChance> RightTiles);

public class Profile
{
    private const string PROFILE_FILE = "profile.json";
    private const string LOCATIONS_FILE = "locations.json";
    private const string LAND_TILE_SETS_FILE = "landtilesets.json";
    private const string STATIC_TILE_SETS_FILE = "statictilesets.json";
    private const string HUE_SETS_FILE = "huesets.json";
    private const string LAND_BRUSH_FILE = "landbrush.json";
    private const string STATIC_FILTER_FILE = "staticfilter.json";
    private const string WALL_SETS_FILE = "wallsets.json";

    [JsonIgnore] public string Name { get; set; } = "";
    public string Hostname { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 2597;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string ClientPath { get; set; } = "";
    [JsonIgnore]
    public Dictionary<string, RadarFavorite> RadarFavorites { get; set; } = new();
    [JsonIgnore] public Dictionary<string, List<ushort>> LandTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, List<ushort>> StaticTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> HueSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, LandBrush> LandBrush { get; set; } = new();
    [JsonIgnore] public List<int> StaticFilter { get; set; } = new();
    [JsonIgnore] public Dictionary<string, WallSet> WallSets { get; set; } = new();


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
        File.WriteAllText(Path.Join(profileDir, LAND_BRUSH_FILE), JsonSerializer.Serialize(LandBrush, Models.LandBrush.JsonOptions));
        File.WriteAllText(Path.Join(profileDir, STATIC_FILTER_FILE), JsonSerializer.Serialize(StaticFilter, options));
        File.WriteAllText(Path.Join(profileDir, WALL_SETS_FILE), JsonSerializer.Serialize(WallSets, options));
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

        var landTileSets = Deserialize<Dictionary<string, List<ushort>>>(Path.Join(profileDir, LAND_TILE_SETS_FILE));
        if (landTileSets != null)
            profile.LandTileSets = landTileSets;

        var staticTileSets = Deserialize<Dictionary<string, List<ushort>>>(Path.Join(profileDir, STATIC_TILE_SETS_FILE));
        if (staticTileSets != null)
            profile.StaticTileSets = staticTileSets;

        var huesets = Deserialize<Dictionary<string, SortedSet<ushort>>>(Path.Join(profileDir, HUE_SETS_FILE));
        if (huesets != null)
            profile.HueSets = huesets;

        var landBrush = Deserialize<Dictionary<string, LandBrush>>(Path.Join(profileDir, LAND_BRUSH_FILE), Models.LandBrush.JsonOptions);
        if (landBrush != null)
            profile.LandBrush = landBrush;

        var staticFilter = Deserialize<List<int>>(Path.Join(profileDir, STATIC_FILTER_FILE));
        if (staticFilter != null)
            profile.StaticFilter = staticFilter;

        var wallSets = Deserialize<Dictionary<string, WallSet>>(Path.Join(profileDir, WALL_SETS_FILE));
        if (wallSets != null)
            profile.WallSets = wallSets;

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

    public void SerializeStaticFilter(string path)
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
        
        File.WriteAllText(Path.Join(profileDir, STATIC_FILTER_FILE), JsonSerializer.Serialize(StaticFilter, options));
    }

}