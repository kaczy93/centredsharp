using System.Reflection;
using CentrED.Server.Config;

namespace CentrED.Server;

public class Application
{
    private static CEDServer _cedServer;
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
            _cedServer = new CEDServer(config);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => _cedServer.Save();
            if (Environment.UserInteractive && !Console.IsInputRedirected)
            {
                new Thread(HandleConsoleInput)
                {
                    IsBackground = true,
                    Name = "Console Input"
                }.Start();
            }
            _cedServer.Run();
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

    public static async void HandleConsoleInput()
    {
        while (true)
        {
            string input;
            try
            {
                input = Console.ReadLine()?.Trim();
            }
            catch(Exception e)
            {
                Console.WriteLine("Console input error!");
                Console.WriteLine(e);
                return;
            }
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }
            _cedServer.PushCommand(input);
        }
    }
}