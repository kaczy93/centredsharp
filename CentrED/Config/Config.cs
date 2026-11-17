using System.Text;

namespace CentrED;

public partial class Config
{
    private static readonly TimeSpan SaveRate = TimeSpan.FromSeconds(30);

    private Dictionary<string, string> _rawEntries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConfigValue> _entries = new(StringComparer.Ordinal);
    private DateTime _lastSave = DateTime.UtcNow;
    private string _lastWritten = "";

    public Config(string filePath)
    {
        FilePath = filePath;
        Load();
    }

    public string FilePath { get; private set; }

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

    public void Load()
    {
        Load(FilePath);
    }
    
    public void Load(string filePath)
    {
        _rawEntries.Clear();
        _lastWritten = "";
        
        FilePath = filePath;
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
            sb.Append(key).Append('=').Append(output[key].ReplaceLineEndings(" ")).Append('\n');
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

    public string[] SubKeys(string prefix)
    {
        prefix = SanitizeKey(prefix) + '.';
        return _rawEntries.Keys.Concat(_entries.Keys)
                          .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                          .Select(k => k[prefix.Length..].Split('.')[0])
                          .Distinct()
                          .Order(StringComparer.Ordinal)
                          .ToArray();
    }

    public void Remove(string key)
    {
        key = SanitizeKey(key);
        var prefix = key + '.';
        var toRemove = _rawEntries.Keys.Concat(_entries.Keys)
                                  .Where(k => k == key || k.StartsWith(prefix, StringComparison.Ordinal))
                                  .ToArray();
        foreach (var k in toRemove)
        {
            _rawEntries.Remove(k);
            _entries.Remove(k);
        }
    }
    
    public void ReleasePrefix(string prefix)
    {
        prefix = SanitizeKey(prefix) + '.';
        foreach (var k in _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            _entries.Remove(k);
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
            result[key.ToLowerInvariant()] = line[(separator + 1)..];
        }
    }
}
