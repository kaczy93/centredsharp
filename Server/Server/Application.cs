using System.Reflection;

namespace CentrED.Server;

public class Application {
    public static void Main(string[] args) {
        
        Console.WriteLine($"CentrED# Server Version {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine("Copyright " + Constants.Assembly.Copyright);
        Console.WriteLine("Credits to Andreas Schneider, StaticZ");
        try {
            CEDServer.Init(args);
            CEDServer.Run();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
        finally {
            Console.WriteLine("Shutting down");
        }
    }

    public static string GetCurrentExecutable() {
        return AppDomain.CurrentDomain.FriendlyName;
    }
}