namespace CentrED.Utility; 

public static class Logger {
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    public static void LogInfo(string log) {
        Log("INFO", log);
    }

    public static void LogError(string log) {
        Log("ERROR", log);
    }

    public static void LogDebug(string log) {
        if (DEBUG) Log("DEBUG", log);
    }

    internal static void Log(string level, string log) {
        Console.WriteLine($"[{level}] {DateTime.Now} {log}");
    }
}