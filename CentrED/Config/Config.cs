using System.Text;

namespace CentrED;

public class Config(string filePath)
{
    private static readonly TimeSpan SaveRate = TimeSpan.FromSeconds(30);

    private Dictionary<string, string> _rawEntries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConfigEntry> _entries = new(StringComparer.Ordinal);
    private DateTime _lastSave = DateTime.UtcNow;
    private string _lastWritten = "";

    public string FilePath { get; } = filePath;

    public ConfigEntry<T> GetEntry<T>(string key, T defaultValue)
    {
        if (!IsValidKey(key))
            throw new ArgumentException($"Invalid config key '{key}'", nameof(key));
        
        if (_entries.TryGetValue(key, out var existing))
        {
            return existing as ConfigEntry<T> ?? throw new InvalidOperationException($"Config key '{key}' already registered as {existing.GetType()}");
        }

        ConfigEntry<T> entry;
        if (_rawEntries.TryGetValue(key, out var rawValue))
        {
            entry = new ConfigEntry<T>(key, ConfigConverters.Get<T>().Parse(rawValue));
        }
        else
        {
            entry = new ConfigEntry<T>(key, defaultValue);
        }
        _entries[key] = entry;
        return entry;
    }

    public T Get<T>(string key, T defaultValue) => GetEntry(key, defaultValue).Value;

    public void Set<T>(string key, T value) => GetEntry(key, value).Value = value;

    public void Initialize()
    {
        throw new NotImplementedException();
        ConfigLegacy.Initialize();
        Load();
    }

    public void Load()
    {
        _rawEntries.Clear();
        _lastWritten = "";
        if (File.Exists(FilePath))
        {
            _lastWritten = File.ReadAllText(FilePath);
            ParseInto(_lastWritten, _rawEntries);
        }
        //Update existing entries in place when we reload in case something is holding entry handle
        foreach (var entry in _entries.Values)
        {
            if (_rawEntries.TryGetValue(entry.Key, out var rawValue))
            {
                entry.Update(rawValue);
            }
        }
    }

    public void AutoSave()
    {
        if (DateTime.UtcNow > _lastSave + SaveRate)
        {
            Save();
        }
    }

    public void Save()
    {
        var output = new Dictionary<string, string>(_rawEntries, StringComparer.Ordinal);
        foreach (var (key, entry) in _entries)
        {
            output[key] = entry.Format();
        }
        _rawEntries = output;
        _lastSave = DateTime.UtcNow;

        var sb = new StringBuilder();
        foreach (var key in output.Keys.Order(StringComparer.Ordinal))
        {
            sb.Append(key).Append('=').Append(Escape(output[key])).Append('\n');
        }
        var text = sb.ToString();
        if (text == _lastWritten)
            return;
        File.WriteAllText(FilePath, text);
        _lastWritten = text;
    }

    public static bool IsValidKey(string key)
    {
        if (key.Length == 0)
            return false;
        foreach (var c in key)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.')
                return false;
        }
        return true;
    }
    
    private static void ParseInto(string text, Dictionary<string, string> result)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            var separator = line.IndexOf('=');
            var key = separator < 0 ? line : line[..separator];
            if (separator < 0 || !IsValidKey(key))
            {
                Console.WriteLine($"Config: invalid line '{line}', skipping");
                continue;
            }
            result[key] = Unescape(line[(separator + 1)..]);
        }
    }

    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '=': sb.Append(@"\="); break;
                case '\n': sb.Append(@"\n"); break;
                case '\r': sb.Append(@"\r"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string Unescape(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c != '\\' || i + 1 == value.Length)
            {
                sb.Append(c);
                continue;
            }
            var next = value[++i];
            sb.Append
            (
                next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    _ => next
                }
            );
        }
        return sb.ToString();
    }
}
