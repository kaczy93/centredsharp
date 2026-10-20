namespace CentrED.Utils;

public static class Metrics
{
    public static Dictionary<string, TimeSpan> Timers = new();
    private static readonly Dictionary<string, DateTime> Starts = new();

    public static void Start(String name)
    {
        Starts[name] = DateTime.Now;
    }

    public static void Stop(String name)
    {
        Timers[name] = DateTime.Now - Starts[name];
    }

    public static void Measure(String name, Action callback)
    {
        Start(name);
        callback();
        Stop(name);
    }
}