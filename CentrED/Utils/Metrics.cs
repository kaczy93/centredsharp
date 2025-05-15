namespace CentrED.Utils;

public class Metrics
{
    public Dictionary<string, TimeSpan> Timers = new();
    private readonly Dictionary<string, DateTime> starts = new();
    
    public TimeSpan this[string name]
    {
        set => Timers[name] = value;
    }

    public void Start(String name)
    {
       starts[name] = DateTime.Now;
    }

    public void Stop(String name)
    {
        Timers[name] = DateTime.Now - starts[name];
    }

    public void Measure(String name, Action callback)
    {
        Start(name);
        callback();
        Stop(name);
    }
}