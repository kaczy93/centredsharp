using System.Reflection;
using CentrED.Map;
using CentrED.UI;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using static SDL3.SDL;

namespace CentrED;

public class CentrEDGame : Game
{
    public readonly GraphicsDeviceManager _gdm;

    public MapManager MapManager;
    public UIManager UIManager;
    public bool Closing { get; set; }
    
    public CentrEDGame()
    {
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
        Window.Title = $"{appName.Name} {appName.Version}";
        
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowResized;
    }
    
    protected override void Initialize()
    {
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }

        _gdm.ApplyChanges();

        Log.Start(LogTypes.All);
        MapManager = new MapManager(_gdm.GraphicsDevice);
        UIManager = new UIManager(_gdm.GraphicsDevice, Window);
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
        CEDClient.Disconnect();
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            Metrics.Start("UpdateClient");
            CEDClient.Update();
            Metrics.Stop("UpdateClient");
            MapManager.Update(gameTime, IsActive, !UIManager.CapturingMouse, !UIManager.CapturingKeyboard);
            Config.AutoSave();
        }
        catch(Exception e)
        {
            UIManager.ReportCrash(e);
        }
        base.Update(gameTime);
    }

    protected override bool BeginDraw()
    {
        //Resize BackBuffer if needed
        //We can rely on UIManager, since it draws UI over the main window as well as handles to all the extra windows
        var maxWindowSize = UIManager.MaxWindowSize();
        var pp = GraphicsDevice.PresentationParameters;
        if (maxWindowSize.X != 0 && pp.BackBufferWidth != maxWindowSize.X || maxWindowSize.Y != 0 && pp.BackBufferHeight != maxWindowSize.Y)
        {
            pp.BackBufferWidth = (int)maxWindowSize.X;
            pp.BackBufferHeight = (int)maxWindowSize.Y;
            pp.DeviceWindowHandle = Window.Handle;
            GraphicsDevice.Reset(pp);
        }
        Rectangle bounds = Window.ClientBounds;
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.Viewport = new Viewport(0, 0, bounds.Width, bounds.Height);
        return base.BeginDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (gameTime.ElapsedGameTime.Ticks > 0)
        {
            try
            {
                if (IsActive)
                {
                    MapManager.Draw();
                    UIManager.Draw(gameTime, IsActive);
                    Present();
                    UIManager.DrawExtraWindows();
                }
            }
            catch (Exception e)
            {
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
        //We handle everything in Draw
    }

    private void OnWindowResized(object? sender, EventArgs e)
    {
        GameWindow window = sender as GameWindow;
        if (window != null)
            MapManager.OnWindowsResized(window);
    }
}