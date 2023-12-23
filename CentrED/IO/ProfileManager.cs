using CentrED.IO.Models;

namespace CentrED.IO;

public static class ProfileManager
{
    private const string ProfilesDir = "profiles";

    public static List<Profile> Profiles = new();

    static ProfileManager()
    {
        if (!Directory.Exists(ProfilesDir))
        {
            Directory.CreateDirectory(ProfilesDir);
        }
        foreach (var profileDir in Directory.EnumerateDirectories(ProfilesDir))
        {
            var profile = Profile.Deserialize(profileDir);
            if(profile != null)
                Profiles.Add(profile);
        }
    }

    public static string[] ProfileNames => Profiles.Select(p => p.Name).ToArray();

    public static Profile ActiveProfile => Profiles.Find(p => p.Name == Config.Instance.ActiveProfile) ?? new Profile();

    public static int Save()
    {
        return Save(ActiveProfile);
    }
    
    public static int Save(Profile newProfile)
    {
        var index = Profiles.FindIndex(p => p.Name == newProfile.Name);
        if (index != -1)
        {
            var profile = Profiles[index];
            profile.Hostname = newProfile.Hostname;
            profile.Port = newProfile.Port;
            profile.Username = newProfile.Username;
            profile.ClientPath = newProfile.ClientPath;
            profile.ClientVersion = newProfile.ClientVersion;
            profile.RadarFavorites = newProfile.RadarFavorites;
        }
        else
        {
            Profiles.Add(newProfile);
            index = Profiles.Count - 1;
        }
        newProfile.Serialize(ProfilesDir);
        Config.Instance.ActiveProfile = newProfile.Name;
        return index;
    }
}