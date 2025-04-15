using System.Globalization;
using CentrED.Client;
using CentrED.Server;
using CentrED.Utils;

namespace CentrED;

public class Application
{
    static public string WorkDir { get; } = AppContext.BaseDirectory;

    public static CentrEDGame CEDGame { get; private set; } = null!;
    public static CEDServer? CEDServer;
    public static readonly CentrEDClient CEDClient = new();
    public static readonly Metrics Metrics = new();

    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        FNADllMap.Init();

        Console.WriteLine($"Root Dir: {WorkDir}");

        Config.Initialize();

        using (CEDGame = new CentrEDGame())
        {
            try
            {
                CEDGame.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                File.WriteAllText("Crash.log", e.ToString());
            }
        }
    }
}