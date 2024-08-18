using System.Reflection;
using CentrED.Server.Config;

namespace CentrED.Server;

public class Application
{
    public static void Main(string[] args)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        var title = $"{assemblyName.Name} {assemblyName.Version}";
        Console.Title = title;
        Console.WriteLine(title);
        Console.WriteLine("Copyright 2024 Kaczy" );
        Console.WriteLine("Credits to Andreas Schneider, StaticZ");
        try
        {
            var config = ConfigRoot.Init(args);
            var server = new CEDServer(config);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => server.Save();
            server.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            Console.WriteLine("Shutting down");
        }
    }

    public static string GetCurrentExecutable()
    {
        return AppDomain.CurrentDomain.FriendlyName;
    }
}