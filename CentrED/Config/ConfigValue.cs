namespace CentrED;

public abstract class ConfigValue(string key)
{
    public readonly string Key = key;

    internal abstract void SetRaw(string rawValue);
    public abstract string Format();
    internal abstract void Reset();
    public abstract bool IsDefault { get; }
}

public class ConfigValue<T>(string key, T defaultValue) : ConfigValue(key) where T : notnull
{
    private T _default = defaultValue;
    public T Value = defaultValue;

    internal override void SetRaw(string rawValue)
    {
        if (ConfigConverters.Get<T>().TryParse(rawValue, out var value))
            Value = value;
        else
            Console.WriteLine($"Config: invalid value for '{Key}': '{rawValue}', keeping '{Format()}'");
    }

    public override string Format() => ConfigConverters.Get<T>().Format(Value);

    internal override void Reset()
    {
        Value = _default;
    }

    public override bool IsDefault => _default.Equals(Value);
}
