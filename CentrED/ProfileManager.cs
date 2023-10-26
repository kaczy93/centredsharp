using System.Text.Json;
using System.Text.Json.Serialization;

namespace CentrED;

public class Profile {
    [JsonIgnore]
    public string Name { get; set; }
    public string Hostname { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    // public string Password { get; set; }
    public string ClientPath { get; set; }
    public string ClientVersion { get; set; }
}

public static class ProfileManager {
    private const string ProfilesDir = "profiles";
    
    public static List<Profile> Profiles = new();
    
    static ProfileManager(){
        if (!Directory.Exists(ProfilesDir)) {
            Directory.CreateDirectory(ProfilesDir);
        }
        foreach (var filePath in Directory.EnumerateFiles(ProfilesDir)) {
            var jsonText = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<Profile>(jsonText);
            profile.Name = Path.GetFileNameWithoutExtension(filePath);
            Profiles.Add(profile);
        }
    }
    public static string[] ProfileNames => Profiles.Select(p => p.Name).ToArray();

    public static Profile? ActiveProfile => Profiles.Find(p => p.Name == Config.ActiveProfile);

    public static int Save(Profile newProfile) {
        var index = Profiles.FindIndex(p => p.Name == newProfile.Name);
        if (index != -1) {
            var profile = Profiles[index];
            profile.Hostname = newProfile.Hostname;
            profile.Port = newProfile.Port;
            profile.Username = newProfile.Username;
            profile.ClientPath = newProfile.ClientPath;
            profile.ClientVersion = newProfile.ClientVersion;
        }
        else {
            Profiles.Add(newProfile);
            index =  Profiles.Count - 1;
        }
        SaveToDisk(newProfile);
        return index;
    }

    private static void SaveToDisk(Profile profile) {
        var path = Path.Join(ProfilesDir, $"{profile.Name}.json");
        var json = JsonSerializer.Serialize(profile);
        File.WriteAllText(path, json);
    }
}