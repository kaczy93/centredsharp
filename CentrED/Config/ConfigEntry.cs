namespace CentrED;

public abstract class ConfigEntry(string key)
{
    public string Key { get; } = key;

    internal virtual bool IsDictionary => false;

    internal abstract void Load(IReadOnlyDictionary<string, string> raw);
    internal abstract void Write(IDictionary<string, string> output);
}

public abstract class ConfigEntry<T>(string key) : ConfigEntry(key)
{
    public T Value = default!;

    public abstract T Default { get; }

    public void Reset() => Value = Default;
}

internal sealed class ConfigValueEntry<T> : ConfigEntry<T>
{
    private readonly ConfigConverter<T> _converter;
    private readonly string _defaultText;

    public ConfigValueEntry(string key, T defaultValue) : base(key)
    {
        _converter = ConfigConverters.Get<T>();
        _defaultText = _converter.Format(defaultValue);
    }

    public override T Default => _converter.Parse(_defaultText);

    internal override void Load(IReadOnlyDictionary<string, string> raw)
    {
        if (raw.TryGetValue(Key, out var text))
        {
            try
            {
                Value = _converter.Parse(text);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Config: invalid value for '{Key}': '{text}' ({e.Message}), using default");
            }
        }
        Reset();
    }

    internal override void Write(IDictionary<string, string> output)
    {
        output[Key] = _converter.Format(Value);
    }
}

internal sealed class ConfigDictionaryEntry<T>(string prefix) : ConfigEntry<Dictionary<string, T>>(prefix)
{
    private readonly ConfigConverter<T> _converter = ConfigConverters.Get<T>();
    private readonly string _keyPrefix = prefix + '.';

    internal override bool IsDictionary => true;

    public override Dictionary<string, T> Default => new();

    internal override void Load(IReadOnlyDictionary<string, string> raw)
    {
        Value = new Dictionary<string, T>();
        foreach (var (key, text) in raw)
        {
            if (!key.StartsWith(_keyPrefix, StringComparison.Ordinal))
                continue;
            try
            {
                Value[key[_keyPrefix.Length..]] = _converter.Parse(text);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Config: invalid value for '{key}': '{text}' ({e.Message}), skipping");
            }
        }
    }

    internal override void Write(IDictionary<string, string> output)
    {
        foreach (var key in output.Keys.Where(k => k.StartsWith(_keyPrefix, StringComparison.Ordinal)).ToList())
        {
            output.Remove(key);
        }
        foreach (var (subKey, value) in Value)
        {
            var key = _keyPrefix + subKey;
            if (!Config.IsValidKey(key))
            {
                Console.WriteLine($"Config: invalid key '{key}', skipping");
                continue;
            }
            output[key] = _converter.Format(value);
        }
    }
}
