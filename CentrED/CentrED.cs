using SDL3;

namespace CentrED;

public class CentrED
{
    public static CentrEDGame App { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var config = new Config("config.cfg");
        config.MigrateGlobalConfig();
        var backend = config.Init("graphics.driver", "SDL_GPU");
        if(!backend.IsDefault)
            SDL.SDL_SetHint("FNA3D_FORCE_DRIVER", backend.Value); //It have to be set before Game constructor
        
        using (App = new CentrEDGame(config))
        {
            try
            {
                App.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                File.WriteAllText("Crash.log", e.ToString());
            }
        }
    }
}