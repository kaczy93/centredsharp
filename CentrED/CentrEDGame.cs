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

    public CentrEDGame()
    {
        _gdm = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 900,
            IsFullScreen = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24
        };

        _gdm.PreparingDeviceSettings += (sender, e) => { e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents; };

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }
        _gdm.ApplyChanges();
        
        
        NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "x64", "zlib.dll"));
        Log.Start(LogTypes.All);
        UOFileManager.Load(ClientVersion.CV_70796, @"D:\Games\Ultima Online Classic_7_0_95_0_modified", false, "enu");
        
        TextureAtlas.InitializeSharedTexture(_gdm.GraphicsDevice);
        _centredClient = new CentrEDClient("127.0.0.1", 2597, "admin", "admin");
        _mapManager = new MapManager(_gdm.GraphicsDevice, _centredClient);
        _uiManager = new UIManager(_gdm.GraphicsDevice, _mapManager);

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
        _centredClient.Update();
        _uiManager.Update(gameTime);
        _mapManager.Update(gameTime, !_uiManager.CapturingMouse, !_uiManager.CapturingKeyboard);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!IsActive)
            return;

        _mapManager.Draw();
        _uiManager.Draw();

        base.Draw(gameTime);
    }
}