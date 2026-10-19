using CentrED.Client;

namespace CentrED;

public class Application
{
    public static CentrEDGame CEDGame { get; private set; } = null!;
    public static readonly CentrEDClient CEDClient = new();

    [STAThread]
    public static void Main(string[] args)
    {
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