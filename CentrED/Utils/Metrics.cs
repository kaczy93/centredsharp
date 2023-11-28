namespace CentrED.Utils;

public class Metrics
{
    public Dictionary<string, TimeSpan> Values = new();
    private readonly Dictionary<string, DateTime> starts = new();
    
    public TimeSpan this[string name]
    {
        set => Values[name] = value;
    }

    public void Start(String name)
    {
       starts[name] = DateTime.Now;
    }

    public void Stop(String name)
    {
        Values[name] = DateTime.Now - starts[name];
    }
}