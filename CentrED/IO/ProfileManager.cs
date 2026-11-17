using static CentrED.CentrED;

namespace CentrED.IO;

public partial class ProfileManager
{
    private const string ProfilesDir = "Profiles";

    public static string ProfilePath(string name) => Path.Combine(ProfilesDir, $"{name}.cfg");
    
    public readonly ConfigValue<string> ActiveName = App.Config.Init("profile.active", "Default");
    public readonly Config Active;
    
    public event Action? Changed;

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
        Active = new Config(ProfilePath(ActiveName.Value));
        App.Client.Disconnected += Save;
    }
    
    public void Add(string name)
    {
        _Names.Add(name);
        Names = _Names.ToArray();
        ChangeActive(Names.IndexOf(name));
    }

    public void Remove()
    {
        var curName = ActiveName.Value;
        var index = Names.IndexOf(curName);
        ChangeActive(index > 0 ? index - 1 : 1);
        _Names.Remove(curName);
        Names = _Names.ToArray();
        File.Delete(ProfilePath(curName));
    }

    public void ChangeActive(int index)
    {
        Active.Save();
        ActiveName.Value = Names[index];
        Active.Load(ProfilePath(ActiveName.Value));
        Changed?.Invoke();
    }

    public void Save()
    {
        Active.Save();
    }
}