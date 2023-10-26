using System.Runtime.InteropServices;
using CentrED.Client;
using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED;

internal class CentrEDGame : Game
{
    private readonly GraphicsDeviceManager _gdm;

    private CentrEDClient _centredClient;
    private MapManager _mapManager;
    private UIManager _uiManager;
    private HuesManager _huesManager;

    public CentrEDGame()
    {
        _gdm = new GraphicsDeviceManager(this)
        {
            IsFullScreen = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24
        };

        _gdm.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents; };

        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowResized;
    }

    protected override unsafe void Initialize()
    {
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }
        
        _gdm.ApplyChanges();

        NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "x64", "zlib.dll"));
        Log.Start(LogTypes.All);
        var background = Content.Load<Texture2D>("background");
        _mapManager = new MapManager(_gdm.GraphicsDevice, background);
        _uiManager = new UIManager(this, _gdm.GraphicsDevice, _mapManager);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        CentrED.Client.Update();
        _uiManager.Update(gameTime, IsActive);
        _mapManager.Update(gameTime, IsActive, !_uiManager.CapturingMouse, !_uiManager.CapturingKeyboard);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // if (!IsActive)
            // return;
            
        _mapManager.Draw();
        _uiManager.Draw(gameTime);

        base.Draw(gameTime);
    }
    
        
    private void OnWindowResized(object? sender, EventArgs e) {
        GameWindow window = sender as GameWindow;
        if (window != null) 
            _mapManager.OnWindowsResized(window);
    }
}