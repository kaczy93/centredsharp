using CentrED.IO.Models;

namespace CentrED.IO;

internal static class ProfileMigration
{
    private const string LEGACY_DIR = "profiles";
    private const string BACKUP_DIR = "profiles.bak";

    public static List<int>? ActiveStaticFilter { get; private set; }

    public static void MigrateLegacy(NamedConfigManager profiles)
    {
        if(ConfigLegacy.Instance != null && !string.IsNullOrEmpty(ConfigLegacy.Instance.ActiveProfile))
        {
            if (profiles.ActiveName.IsDefault)
                profiles.ActiveName.Value = ConfigLegacy.Instance.ActiveProfile;
        }
        
        if (!Directory.Exists(LEGACY_DIR))
            return;
        
        foreach (var profileDir in Directory.EnumerateDirectories(LEGACY_DIR))
        {
            var profName = Path.GetFileName(profileDir);
            var legacyProf = ProfileLegacy.Deserialize(profileDir);
            if(legacyProf == null)
                continue;
            
            var prof = new Config(profiles.PathOf(profName));
            prof.Init("hostname", legacyProf.Hostname);
            prof.Init("port", legacyProf.Port);
            prof.Init("username", legacyProf.Username);
            prof.Init("password", legacyProf.Password);
            prof.Init("client_path", legacyProf.ClientPath);
            prof.Init("land_tile_sets", legacyProf.LandTileSets);
            prof.Init("static_tile_sets", legacyProf.StaticTileSets);
            prof.Init("hue_sets", legacyProf.HueSets);
            prof.Init
            (
                "land_brushes",
                legacyProf.LandBrush.ToDictionary(p => p.Key, p => new LandBrush { Tiles = p.Value.Tiles, To = p.Value.Transitions }),
                LandBrushManager.CONFIG_CONVERTER
            );
            var newFavorites = new Dictionary<string, (ushort, ushort)>();
            foreach (var (name, oldFav) in legacyProf.RadarFavorites)
            {
                newFavorites.Add(name, ((ushort)oldFav.X, (ushort)oldFav.Y));
            }
            prof.Init("radar_favorites", newFavorites);
            if (string.Equals(profName, profiles.ActiveName.Value, StringComparison.OrdinalIgnoreCase))
                ActiveStaticFilter = legacyProf.StaticFilter;
            
            prof.Save();
            
            // TODO: delete instead of backup in 7.1
            // Directory.Delete(profileDir, true);
            BackupLegacy(profileDir, profName);
        }
    }
    
    private static void BackupLegacy(string profileDir, string profName)
    {
        Directory.CreateDirectory(BACKUP_DIR);
        var target = Path.Combine(BACKUP_DIR, profName);
        if (Directory.Exists(target))
            Directory.Delete(target, true);
        Directory.Move(profileDir, target);
    }
}