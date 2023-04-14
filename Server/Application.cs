using System.Reflection;
using Cedserver;

namespace Server;

public class Application {
    public static void Main(string[] args) {
        
        Console.WriteLine($"CentrED# Server Version {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine("Copyright " + GetCopyright());
        Console.WriteLine("Credits to Andreas Schneider, StaticZ");
        Config.Init(args);
        try {
            CEDServer.Run();
        }
        finally {
            CEDServer.LogInfo("Shutting down");
            Config.Flush();
            if (!CEDServer.Valid) {
                Console.Write("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    private static string GetCopyright() {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttributes()
            .Where(a => a is AssemblyCopyrightAttribute)
            .Select(a => (a as AssemblyCopyrightAttribute).Copyright)
            .FirstOrDefault("undefined");
    }

    public static string GetCurrentExecutable() {
        return AppDomain.CurrentDomain.FriendlyName;
    }
}