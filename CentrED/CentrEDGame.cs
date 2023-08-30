using System.Runtime.InteropServices;
using CentrED.Client;
using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ClassicUO.IO;
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
        UOFileManager.Load(ClientVersion.CV_70796, @"D:\Games\Ultima Online Classic_7_0_95_0_modified", false, "enu");
        
        TextureAtlas.InitializeSharedTexture(_gdm.GraphicsDevice);
        HuesManager.Initialize(_gdm.GraphicsDevice);
        _mapManager = new MapManager(_gdm.GraphicsDevice);
        _uiManager = new UIManager(_gdm.GraphicsDevice, _mapManager);

        //Preload all graphics
        for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++) {
            if (ArtLoader.Instance.GetValidRefEntry(i).Equals(UOFileIndex.Invalid)) continue;
            ArtLoader.Instance.GetLandTexture((uint)i, out _);
            TexmapsLoader.Instance.GetLandTexture((uint)i, out _);
        }
        for (int i = 0; i < TileDataLoader.Instance.StaticData.Length; i++) {
            if (ArtLoader.Instance.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals(UOFileIndex.Invalid)) continue;
            ArtLoader.Instance.GetStaticTexture((uint)i, out _);
        }
        
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
        _uiManager.Update(gameTime);
        _mapManager.Update(gameTime, !_uiManager.CapturingMouse, !_uiManager.CapturingKeyboard);

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