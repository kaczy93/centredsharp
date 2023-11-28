namespace CentrED.Utils;

public class Metrics
{
    public Dictionary<string, TimeSpan> Values = new();
    public TimeSpan this[string name]
    {
        set => Values[name] = value;
    }
}