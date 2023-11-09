namespace CentrED.Utility;

public class Logger
{
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    public TextWriter Out = Console.Out;

    public void LogInfo(string log)
    {
        Log("INFO", log);
    }

    public void LogError(string log)
    {
        Log("ERROR", log);
    }

    public void LogDebug(string log)
    {
        if (DEBUG)
            Log("DEBUG", log);
    }

    internal void Log(string level, string log)
    {
        Out.WriteLine($"[{level}] {DateTime.Now} {log}");
    }
}