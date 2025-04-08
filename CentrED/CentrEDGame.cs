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
        var assName = Assembly.GetExecutingAssembly().GetName();
        Window.Title = $"{assName.Name} {assName.Version}";
        
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
        UIManager = new UIManager(_gdm.GraphicsDevice);
        RadarMap.Initialize(_gdm.GraphicsDevice);
        SDL_MaximizeWindow(Window.Handle);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
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
            UIManager.Update(gameTime, IsActive);
            MapManager.Update(gameTime, IsActive, !UIManager.CapturingMouse, !UIManager.CapturingKeyboard);
            Config.AutoSave();
        }
        catch(Exception e)
        {
            UIManager.ReportCrash(e);
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // if (!IsActive)
        // return;
        try
        {
            MapManager.Draw();
            UIManager.Draw(gameTime);
        }
        catch(Exception e)
        {
            UIManager.ReportCrash(e);
        }

        base.Draw(gameTime);
    }


    private void OnWindowResized(object? sender, EventArgs e)
    {
        GameWindow window = sender as GameWindow;
        if (window != null)
            MapManager.OnWindowsResized(window);
    }
}