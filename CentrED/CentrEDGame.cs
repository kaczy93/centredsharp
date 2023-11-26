using System.Runtime.InteropServices;
using CentrED.Map;
using CentrED.UI;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED;

public class CentrEDGame : Game
{
    public readonly GraphicsDeviceManager _gdm;

    public MapManager MapManager;
    public UIManager UIManager;

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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "x64", "zlib.dll"));
        }
        Log.Start(LogTypes.All);
        MapManager = new MapManager(_gdm.GraphicsDevice);
        UIManager = new UIManager(_gdm.GraphicsDevice);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        Config.Save();
    }

    protected override void Update(GameTime gameTime)
    {
        Application.CEDClient.Update();
        UIManager.Update(gameTime, IsActive);
        MapManager.Update(gameTime, IsActive, !UIManager.CapturingMouse, !UIManager.CapturingKeyboard);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // if (!IsActive)
        // return;

        MapManager.Draw();
        UIManager.Draw(gameTime);

        base.Draw(gameTime);
    }


    private void OnWindowResized(object? sender, EventArgs e)
    {
        GameWindow window = sender as GameWindow;
        if (window != null)
            MapManager.OnWindowsResized(window);
    }
}