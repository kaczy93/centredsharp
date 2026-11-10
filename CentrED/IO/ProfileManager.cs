using static CentrED.CentrED;

namespace CentrED.IO;

public partial class ProfileManager
{
    private const string ProfilesDir = "Profiles";

    public static string ProfilePath(string name) => Path.Combine(ProfilesDir, $"{name}.cfg");
    
    public readonly ConfigValue<string> ActiveName = App.Config.Init("profile.active", "Default");
    private Config _Active;
    public Config Active => _Active; 
    
    private readonly List<string> _Names = [];
    public string[] Names { get; private set; }
    
    public ProfileManager()
    {
        if (!Directory.Exists(ProfilesDir))
        {
            Directory.CreateDirectory(ProfilesDir);
        }
        MigrateLegacy();
        foreach (var profilePath in Directory.EnumerateFiles(ProfilesDir, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            _Names.Add(Path.GetFileNameWithoutExtension(profilePath));
        }
        if (_Names.Count <= 0)
        {
            _Names.Add(ActiveName.Value);
        }
        Names = _Names.ToArray();
        _Active = new Config(ProfilePath(ActiveName.Value));
        _Active.Load();
    }
    
    public void Add(string name)
    {
        _Names.Add(name);
        Names = _Names.ToArray();
        _Active.Save();
        ActiveName.Value = name;
        _Active.Load(ProfilePath(ActiveName.Value));        
    }

    public void Remove()
    {
        var name = ActiveName.Value;
        var index = Names.IndexOf(name);
        _Names.Remove(name);
        ActiveName.Value = _Names[Math.Max(0, index - 1)];
        _Active.Load(ActiveName.Value);
        File.Delete(ProfilePath(name));
    }

    public void ChangeActive(int index)
    {
        Active.Save();
        ActiveName.Value = Names[index];
        Active.Load(ProfilePath(ActiveName.Value));
    }

    public void Save()
    {
        Active.Save();
    }
}