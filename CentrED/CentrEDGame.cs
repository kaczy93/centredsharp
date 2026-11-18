using System.Globalization;
using System.Reflection;
using CentrED.Client;
using CentrED.IO;
using CentrED.Lights;
using CentrED.Map;
using CentrED.UI;
using CentrED.Utils;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static SDL3.SDL;
using RadarMap = CentrED.Map.RadarMap;

namespace CentrED;

public class CentrEDGame : Game
{
    public string WorkDir { get; } = AppContext.BaseDirectory;
    
    public readonly GraphicsDeviceManager _gdm;

    public Keymap Keymap;
    public MapManager MapManager;
    public UIManager UIManager;
    public NamedConfigManager Profile;
    public NamedConfigManager Workspace;
    public LandBrushManager LandBrush;
    public LightsManager Lights;
    public LangManager Lang;
    public bool Closing { get; set; }
    
    public readonly CentrEDClient Client = new();
    public readonly Config Config;
    
    public CentrEDGame(Config config)
    {
        Config = config;
        Keymap = new Keymap(config);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine($"Working Directory: {WorkDir}");
        
        _gdm = new GraphicsDeviceManager(this)
        {
            IsFullScreen = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24
        };

        _gdm.PreparingDeviceSettings += (sender, e) =>
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                RenderTargetUsage.DiscardContents;
        };
        var appName = Assembly.GetExecutingAssembly().GetName();
        SDL_SetWindowTitle(Window.Handle, $"{appName.Name} {appName.Version}");

        SDL_ShowCursor();
        SDL_SetWindowResizable(Window.Handle, true);
        Window.ClientSizeChanged += OnWindowResized;
    }
    
    protected override void Initialize()
    {
        Profile = new NamedConfigManager("Profiles", "profile.active", ProfileMigration.MigrateLegacy);
        Workspace = new NamedConfigManager("Workspaces", "workspace.active", WorkspaceMigration.MigrateLegacy, UIManager.LAYOUT_EXT);
        Client.Disconnected += Profile.Save;
        
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }

        _gdm.ApplyChanges();

        Log.Start(LogTypes.All);
        Lang = new LangManager();
        Lights = new LightsManager();
        //UIManager have to exist before MapManager, since Tools can be dependent on Windows
        UIManager = new UIManager(_gdm.GraphicsDevice, Window);
        MapManager = new MapManager(_gdm.GraphicsDevice, Window);
        LandBrush = new LandBrushManager();
        RadarMap.Initialize(_gdm.GraphicsDevice);
        
        base.Initialize();
    }

    protected override void BeginRun()
    {
        base.BeginRun();
        SDL_MaximizeWindow(Window.Handle);
    }

    protected override void UnloadContent()
    {
        Client.Disconnect();
        Config.Save();
        Profile.Save();
        Workspace.Save();
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            Keymap.Update(Keyboard.GetState());
            Metrics.Start("UpdateClient");
            if(Client.Running)
                Client.Update();
            Metrics.Stop("UpdateClient");
            MapManager.Update(gameTime, IsActive, !UIManager.CapturingMouse, !UIManager.CapturingKeyboard);
            Config.AutoSave();
            Profile.Active.AutoSave();
            Workspace.Active.AutoSave();
        }
        catch(Exception e)
        {
            UIManager.ReportCrash(e);
        }
        base.Update(gameTime);
    }

    protected override bool BeginDraw()
    {
        Metrics.Start("BeginDraw");
        //We can rely on UIManager, since it draws UI over the main window as well as handles to all the extra windows
        var maxWindowSize = UIManager.MaxWindowSize();
        var width = (int)maxWindowSize.X;
        var height = (int)maxWindowSize.Y;
        if (width > 0 && height > 0)
        {
            var pp = GraphicsDevice.PresentationParameters;
            if (width != pp.BackBufferWidth || height != pp.BackBufferHeight)
            {
                pp.BackBufferWidth = width;
                pp.BackBufferHeight = height;
                pp.DeviceWindowHandle = Window.Handle;
                GraphicsDevice.Reset(pp);
            }
        }
        Metrics.Stop("BeginDraw");
        return base.BeginDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (gameTime.ElapsedGameTime.Ticks > 0)
        {
            try
            {
                Metrics.Start("Draw");
                MapManager.Draw();
                UIManager.Draw();
                Present();
                UIManager.DrawExtraWindows();
                MapManager.AfterDraw();
                Metrics.Stop("Draw");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                UIManager.ReportCrash(e);
            }
        }
        base.Draw(gameTime);
    }

    private void Present()
    {
        Rectangle bounds = Window.ClientBounds;
        GraphicsDevice.Present(
            new Rectangle(0, 0, bounds.Width, bounds.Height),
            null,
            Window.Handle
        );
    }

    protected override void EndDraw()
    {
        //Restore main window viewport and scissor rectangle for next tick Update()
        var gameWindowRect = Window.ClientBounds;
        GraphicsDevice.Viewport = new Viewport(0, 0, gameWindowRect.Width, gameWindowRect.Height);
        GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, gameWindowRect.Width, gameWindowRect.Height);
    }

    private void OnWindowResized(object? sender, EventArgs e)
    {
        GameWindow window = sender as GameWindow;
        if (window != null)
            MapManager.OnWindowsResized(window);
    }
}