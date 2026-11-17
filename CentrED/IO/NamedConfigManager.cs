using static CentrED.CentrED;

namespace CentrED.IO;

public class NamedConfigManager
{
    private const string DEFAULT_NAME = "Default";

    private readonly string _dir;
    private readonly List<string> _names = [];

    public readonly ConfigValue<string> ActiveName;
    public readonly Config Active;
    public event Action? Changed;
    public string[] Names { get; private set; } = [];

    public NamedConfigManager(string dir, string activeKey, Action<NamedConfigManager>? migrate = null)
    {
        _dir = dir;
        ActiveName = App.Config.Init(activeKey, DEFAULT_NAME);
        Directory.CreateDirectory(_dir);
        migrate?.Invoke(this);

        foreach (var path in Directory.EnumerateFiles(_dir, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            _names.Add(Path.GetFileNameWithoutExtension(path));
        }
        if (_names.Count == 0)
        {
            _names.Add(ActiveName.Value);
        }
        UpdateNames();
        ActiveName.Value = Find(ActiveName.Value) ?? Names[0];
        Active = new Config(PathOf(ActiveName.Value));
    }

    public string PathOf(string name) => Path.Combine(_dir, $"{name}.cfg");

    public bool Exists(string name) => Find(name) != null;

    public static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    public bool IsValidNewName(string name)
    {
        name = Sanitize(name);
        return name.Length > 0 && !Exists(name);
    }

    public void ChangeActive(string name)
    {
        Active.Save();
        ActiveName.Value = name;
        Active.Load(PathOf(name));
        Changed?.Invoke();
    }

    public void Create(string name)
    {
        name = Sanitize(name);
        if (!IsValidNewName(name))
            throw new ArgumentException($"Invalid or existing name '{name}'", nameof(name));
        ChangeActive(name);
        _names.Add(name);
        UpdateNames();
    }

    public void Duplicate(string name)
    {
        name = Sanitize(name);
        if (!IsValidNewName(name))
            throw new ArgumentException($"Invalid or existing name '{name}'", nameof(name));
        Active.Save();
        File.Copy(Active.FilePath, PathOf(name), true);
        Create(name);
    }

    public void Reset()
    {
        File.Delete(Active.FilePath);
        Active.Load();
        Active.Save();
        Changed?.Invoke();
    }

    public void Remove(string name)
    {
        if (Names.Length <= 1)
            return;

        if (name == ActiveName.Value)
        {
            var index = Array.IndexOf(Names, name);
            ActiveName.Value = Names[index > 0 ? index - 1 : 1];
            Active.Load(PathOf(ActiveName.Value));
            Changed?.Invoke();
        }
        File.Delete(PathOf(name));
        _names.Remove(name);
        UpdateNames();
    }

    public void Save()
    {
        Active.Save();
    }

    private string? Find(string name) =>
        _names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));

    private void UpdateNames()
    {
        Names = _names.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
