//Server/cedserver.lpr

using System.Reflection;
using Cedserver.Config;

public class Server {
    public static void Main(string[] args) {
        Console.WriteLine($"CentrED# Server Version {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine("Copyright " + GetCopyright());
        Console.WriteLine("Credits to Andreas Schneider, StaticZ");
        Console.WriteLine(Directory.GetCurrentDirectory());
        CEDConfig config = CEDConfig.Read();
        Console.WriteLine();
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