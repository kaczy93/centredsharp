//Server/cedserver.lpr

using System.Reflection;
using Cedserver.Config;

namespace Server;

public class Application {
    public static void Main(string[] args) {
        Console.WriteLine($"CentrED# Server Version {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine("Copyright " + GetCopyright());
        Console.WriteLine("Credits to Andreas Schneider, StaticZ");
        if(File.Exists(Config.DefaultPath))
            Config.Read();
        else {
            Config.Init();
        }
        Console.WriteLine($"[{DateTime.Now}] Initialization started");
        CEDServer.Init();
        Console.WriteLine($"[{DateTime.Now}] Initialization done");
        try {
            CEDServer.Run();
        }
        finally {
            Console.Write($"[{DateTime.Now}] Shutting down");
            CEDServer.Stop();
            Config.Write();
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