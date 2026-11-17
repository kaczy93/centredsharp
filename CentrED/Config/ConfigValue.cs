namespace CentrED;

public abstract class ConfigValue(string key)
{
    public readonly string Key = key;

    internal abstract void SetRaw(string rawValue);
    internal abstract void Reset();
    internal abstract string Format();
    public abstract bool IsDefault { get; }
}

public class ConfigValue<T> : ConfigValue where T : notnull
{
    private readonly ConfigConverter<T> _converter;
    private readonly string _rawDefault;
    public T Value;

    public ConfigValue(string key, T defaultValue) : base(key)
    {
        _converter = ConfigConverters.Get<T>();
        _rawDefault = _converter.Format(defaultValue);
        Value = defaultValue;
    }

    internal override void SetRaw(string rawValue)
    {
        if (_converter.TryParse(rawValue, out var value))
        {
            Value = value;
            return;
        }
        Reset();
        Console.WriteLine($"Config: invalid value for '{Key}': '{rawValue}', using default '{_rawDefault}'");
    }

    internal override string Format() => _converter.Format(Value);

    internal override void Reset()
    {
        if (_converter.TryParse(_rawDefault, out var value))
            Value = value;
    }

    public override bool IsDefault => Format() == _rawDefault;
}
