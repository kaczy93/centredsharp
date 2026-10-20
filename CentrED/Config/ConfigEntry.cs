namespace CentrED;

public abstract class ConfigEntry(string key)
{
    public readonly string Key = key;

    public abstract string Format();

    internal abstract void Update(string rawValue);
}

public class ConfigEntry<T>(string key, T value) : ConfigEntry(key)
{
    public T Value = value;

    public override string Format() => ConfigConverters.Get<T>().Format(Value);
    
    internal override void Update(string rawValue)
    {
        if (ConfigConverters.Get<T>().TryParse(rawValue, out var value))
            Value = value;
        else
            Console.WriteLine($"Config: invalid value for '{Key}': '{rawValue}', keeping '{Format()}'");
    }
}
