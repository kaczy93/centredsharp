using System.Text;

namespace CentrED;

public partial class Config(string filePath)
{
    private static readonly TimeSpan SaveRate = TimeSpan.FromSeconds(30);

    private Dictionary<string, string> _rawEntries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConfigValue> _entries = new(StringComparer.Ordinal);
    private DateTime _lastSave = DateTime.UtcNow;
    private string _lastWritten = "";

    public string FilePath { get; } = filePath;

    public ConfigValue<T> Init<T>(string key, T defaultValue) where T : notnull
    {
        key = SanitizeKey(key);
        if (!IsValidKey(key))
            throw new ArgumentException($"Invalid config key '{key}'", nameof(key));
        
        if (_entries.ContainsKey(key))
             throw new InvalidOperationException($"Config key {key} already initialised");

        var entry = new ConfigValue<T>(key, defaultValue);
        if (_rawEntries.TryGetValue(key, out var rawValue))
        {
            entry.SetRaw(rawValue);
        }
        _entries[key] = entry;
        return entry;
    }

    // public ConfigValue<T> Get<T>(string key)
    // {
    //     if (_entries.TryGetValue(key, out var entry))
    //     {
    //         return entry as ConfigValue<T> ?? throw new InvalidOperationException($"Config key '{key}' is registered as {entry.GetType()}");
    //     }
    //     throw new InvalidOperationException("Config entry doesn't exist");
    // }
    
    public void Load()
    {
        _rawEntries.Clear();
        _lastWritten = "";
        if (File.Exists(FilePath))
        {
            _lastWritten = File.ReadAllText(FilePath);
            ParseInto(_lastWritten, _rawEntries);
        }
        //Update existing entries in place 
        foreach (var entry in _entries.Values)
        {
            if (_rawEntries.TryGetValue(entry.Key, out var rawValue))
            {
                entry.SetRaw(rawValue);
            }
            else
            {
                entry.Reset();
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
        if (text == _lastWritten && File.Exists(FilePath))
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

    public static string SanitizeKey(string key)
    {
        return key.Replace(" ", "_").ToLowerInvariant();
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
            result[key.ToLowerInvariant()] = Unescape(line[(separator + 1)..]);
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
