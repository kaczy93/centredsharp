using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED.IO.Models;

public class ProfileLegacy
{
    private const string PROFILE_FILE = "profile.json";
    private const string LOCATIONS_FILE = "locations.json";
    private const string LAND_TILE_SETS_FILE = "landtilesets.json";
    private const string STATIC_TILE_SETS_FILE = "statictilesets.json";
    private const string HUE_SETS_FILE = "huesets.json";
    private const string LAND_BRUSH_FILE = "landbrush.json";
    private const string STATIC_FILTER_FILE = "staticfilter.json";

    [JsonIgnore] public string Name { get; set; } = "";
    public string Hostname { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 2597;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string ClientPath { get; set; } = "";
    [JsonIgnore]
    public Dictionary<string, RadarFavoriteLegacy> RadarFavorites { get; set; } = new();
    [JsonIgnore] public Dictionary<string, List<ushort>> LandTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, List<ushort>> StaticTileSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, SortedSet<ushort>> HueSets { get; set; } = new();
    [JsonIgnore] public Dictionary<string, LandBrushLegacy> LandBrush { get; set; } = new();
    [JsonIgnore] public List<int> StaticFilter { get; set; } = new();


    public static ProfileLegacy? Deserialize(string profileDir)
    {
        DirectoryInfo dir = new DirectoryInfo(profileDir);
        if (!dir.Exists)
            return null;

        var profile = JsonSerializer.Deserialize<ProfileLegacy>(File.ReadAllText(Path.Join(profileDir, PROFILE_FILE)));
        if (profile == null)
            return null;
        profile.Name = dir.Name;

        var favorites = Deserialize<Dictionary<string, RadarFavoriteLegacy>>(Path.Join(profileDir, LOCATIONS_FILE));
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

        var landBrush = Deserialize<Dictionary<string, LandBrushLegacy>>(Path.Join(profileDir, LAND_BRUSH_FILE), LandBrushLegacy.JsonOptions);
        if (landBrush != null)
            profile.LandBrush = landBrush;

        var staticFilter = Deserialize<List<int>>(Path.Join(profileDir, STATIC_FILTER_FILE));
        if (staticFilter != null)
            profile.StaticFilter = staticFilter;

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

public class LandBrushLegacy
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    public string Name = "";
    public List<uint> Tiles = new();
    public Dictionary<string, List<LandBrushTransition>> Transitions = new();
}

public class RadarFavoriteLegacy
{
    public int X { get; set; }
    public int Y { get; set; }
}
