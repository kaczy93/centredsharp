using CentrED.IO.Models;
using static CentrED.CentrED;

namespace CentrED.IO;

public class ProfileManager
{
    private const string ProfilesDir = "Profiles";

    public static string ProfilePath(string name) => Path.Combine(ProfilesDir, $"{name}.cfg");
    
    public readonly ConfigValue<string> ActiveName;
    public Profile Active => Profiles[ActiveName.Value]; //Throws when key is missing, but it should never happen
    
    public readonly Dictionary<string, Profile> Profiles = [];
    
    public string[] ProfileNames = [];
    
    public ProfileManager()
    {
        if (!Directory.Exists(ProfilesDir))
        {
            Directory.CreateDirectory(ProfilesDir);
        }
        foreach (var profilePath in Directory.EnumerateFiles(ProfilesDir, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(profilePath);
            var profile = new Profile(name);
            profile.Load();
            Profiles.Add(name, profile);
        }
        ActiveName = App.Config.Init("profile.active", "Default");
        if (Profiles.Count <= 0)
        {
            Profiles.Add("Default", new Profile("Default"));
            ActiveName.Value = "Default";
        }
        ProfileNames =  Profiles.Keys.ToArray();
    }
    
    public void Add(string name)
    {
        Profiles.Add(name, new Profile(name));
        ActiveName.Value = name;
        ProfileNames = Profiles.Keys.ToArray();
    }

    public bool Remove(string name)
    {
        throw new NotImplementedException("TODO");
        var index = ProfileNames.IndexOf(name);
        Profiles.Remove(name);
    }

    public void Save()
    {
        Active.Save();
    }
}

public class Profile
{
    private Config _Config;

    public readonly string Name;
    public readonly ConfigValue<string> Hostname;
    public readonly ConfigValue<int> Port;
    public readonly ConfigValue<string> Username;
    public readonly ConfigValue<string> Password;
    public readonly ConfigValue<string> ClientPath;
    public readonly ConfigValue<Dictionary<string, (ushort, ushort)>> RadarFavorites;
    public readonly ConfigValue<Dictionary<string, List<ushort>>> LandTileSets;
    public readonly ConfigValue<Dictionary<string, List<ushort>>> StaticTileSets;
    public readonly ConfigValue<Dictionary<string, SortedSet<ushort>>> HueSets;
    public readonly ConfigValue<Dictionary<string, LandBrush>> LandBrush;
    public readonly ConfigValue<List<int>> StaticFilter;
    
    public Profile(string name)
    {
        Name = name;
        _Config = new Config(ProfileManager.ProfilePath(name));
        Hostname = _Config.Init("hostname", "");
        Port = _Config.Init("port", 2597);
        Username = _Config.Init("username", "");
        Password = _Config.Init("password", "");
        ClientPath = _Config.Init("client_path", "");
        RadarFavorites = _Config.Init("radar_favorites", new Dictionary<string, (ushort, ushort)>());
        LandTileSets = _Config.Init("land_tile_sets", new Dictionary<string, List<ushort>>());
        StaticTileSets = _Config.Init("static_tile_sets", new Dictionary<string, List<ushort>>());
        HueSets  = _Config.Init("hue_sets", new Dictionary<string, SortedSet<ushort>>());
        LandBrush = _Config.Init("land_brush", new Dictionary<string, LandBrush>());
        StaticFilter = _Config.Init("static_filter", new List<int>());
    }

    public void Load()
    {
        _Config.Load();
    }
    
    internal void Save()
    {
        _Config.Save();
    }
}