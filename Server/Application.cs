//Server/cedserver.lpr

using System.Reflection;
using Cedserver;

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
        try {
            CEDServer.Run();
        }
        finally {
            Console.Write($"[{DateTime.Now}] Shutting down");
            Config.Flush();
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