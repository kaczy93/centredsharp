using CentrED.IO.Models;

namespace CentrED.IO;

public partial class ProfileManager
{
    private void MigrateLegacy()
    {
        if(ConfigLegacy.Instance != null && !string.IsNullOrEmpty(ConfigLegacy.Instance.ActiveProfile))
        {
            if (ActiveName.IsDefault)
                ActiveName.Value = ConfigLegacy.Instance.ActiveProfile;
        }
        
        if (!Directory.Exists("profiles"))
            return;
        
        foreach (var profileDir in Directory.EnumerateDirectories("profiles"))
        {
            var profName = Path.GetFileName(profileDir);
            var legacyProf = ProfileLegacy.Deserialize(profileDir);
            var prof = new Config(ProfilePath(profName));
            prof.Init("hostname", legacyProf.Hostname);
            prof.Init("port", legacyProf.Port);
            prof.Init("username", legacyProf.Username);
            prof.Init("password", legacyProf.Password);
            prof.Init("client_path", legacyProf.ClientPath);
            prof.Init("land_tile_sets", legacyProf.LandTileSets);
            prof.Init("static_tile_sets", legacyProf.StaticTileSets);
            prof.Init("hue_sets", legacyProf.HueSets);
            foreach (var (name, lbl) in legacyProf.LandBrush)
            {
                var lb = new LandBrush(prof, LandBrushManager.UniqueKey(prof, name));
                lb.Name.Value = name;
                lb.Tiles.Value = lbl.Tiles;
                lb.To.Value = lbl.Transitions;
            }
            var newFavorites = new Dictionary<string, (ushort, ushort)>();
            foreach (var (name, oldFav) in legacyProf.RadarFavorites)
            {
                newFavorites.Add(name, ((ushort)oldFav.X, (ushort)oldFav.Y));
            }
            prof.Init("radar_favorites", newFavorites);
            
            prof.Save();
            Directory.Delete(profileDir, true);
        }
    }
}