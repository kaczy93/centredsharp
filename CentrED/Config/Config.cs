using System.Text;

namespace CentrED;

public class Config(string filePath)
{
    private static readonly TimeSpan SaveRate = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, ConfigEntry> _entries = new(StringComparer.Ordinal);
    private Dictionary<string, string> _raw = new(StringComparer.Ordinal);
    private DateTime _lastSave = DateTime.UtcNow;
    private string _lastWritten = "";

    public string FilePath { get; } = filePath;

    public ConfigEntry<T> Entry<T>(string key, T defaultValue)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            return existing as ConfigValueEntry<T> ?? throw TypeMismatch(key, existing);
        }
        ValidateNewKey(key, false);
        var entry = new ConfigValueEntry<T>(key, defaultValue);
        entry.Load(_raw);
        _entries[key] = entry;
        return entry;
    }

    public ConfigEntry<Dictionary<string, T>> DictionaryEntry<T>(string prefix)
    {
        if (_entries.TryGetValue(prefix, out var existing))
        {
            return existing as ConfigDictionaryEntry<T> ?? throw TypeMismatch(prefix, existing);
        }
        ValidateNewKey(prefix, true);
        var entry = new ConfigDictionaryEntry<T>(prefix);
        entry.Load(_raw);
        _entries[prefix] = entry;
        return entry;
    }

    public T GetOrDefault<T>(string key, T defaultValue) => Entry(key, defaultValue).Value;

    public Dictionary<string, T> GetDictionary<T>(string prefix) => DictionaryEntry<T>(prefix).Value;

    public void Set<T>(string key, T value) => Entry(key, value).Value = value;

    public bool Remove(string key)
    {
        var removed = _entries.Remove(key, out var entry);
        removed |= _raw.Remove(key);
        if (entry is { IsDictionary: true })
        {
            var keyPrefix = key + '.';
            foreach (var rawKey in _raw.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.Ordinal)).ToList())
            {
                _raw.Remove(rawKey);
            }
        }
        return removed;
    }

    public void Initialize()
    {
        throw new NotImplementedException();
        ConfigLegacy.Initialize();
        Load();
    }

    public void Load()
    {
        _raw = new Dictionary<string, string>(StringComparer.Ordinal);
        _lastWritten = "";
        if (File.Exists(FilePath))
        {
            _lastWritten = File.ReadAllText(FilePath);
            Parse(_lastWritten, _raw);
        }
        foreach (var entry in _entries.Values)
        {
            entry.Load(_raw);
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
        var output = new Dictionary<string, string>(_raw, StringComparer.Ordinal);
        foreach (var entry in _entries.Values)
        {
            entry.Write(output);
        }
        _raw = output;
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

    private void ValidateNewKey(string key, bool isDictionary)
    {
        if (!IsValidKey(key))
            throw new ArgumentException($"Invalid config key '{key}'", nameof(key));
        foreach (var other in _entries.Values)
        {
            if ((other.IsDictionary && key.StartsWith(other.Key + '.', StringComparison.Ordinal)) ||
                (isDictionary && other.Key.StartsWith(key + '.', StringComparison.Ordinal)))
                throw new InvalidOperationException($"Config key '{key}' overlaps with '{other.Key}'");
        }
    }

    private static InvalidOperationException TypeMismatch(string key, ConfigEntry existing)
    {
        return new InvalidOperationException($"Config key '{key}' already registered as {existing.GetType()}");
    }

    private static void Parse(string text, Dictionary<string, string> raw)
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
            raw[key] = Unescape(line[(separator + 1)..]);
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
