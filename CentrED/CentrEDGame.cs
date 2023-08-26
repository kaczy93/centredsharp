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
    public static Texture2D _hueSampler;

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
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowResized;
    }

    protected override unsafe void Initialize()
    {
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }
        
        const int TEXTURE_WIDTH = 32;
        const int TEXTURE_HEIGHT = 3000;
        
        _gdm.ApplyChanges();

        NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "x64", "zlib.dll"));
        Log.Start(LogTypes.All);
        UOFileManager.Load(ClientVersion.CV_70796, @"D:\Games\Ultima Online Classic_7_0_95_0_modified", false, "enu");
        
        _hueSampler = new Texture2D(GraphicsDevice, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);

        fixed (uint* ptr = buffer) {
            HuesLoader.Instance.CreateShaderColors(buffer);
            _hueSampler.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        System.Buffers.ArrayPool<uint>.Shared.Return(buffer);
        GraphicsDevice.Textures[2] = _hueSampler;
        GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
        
        TextureAtlas.InitializeSharedTexture(_gdm.GraphicsDevice);
        _mapManager = new MapManager(_gdm.GraphicsDevice);
        _uiManager = new UIManager(_gdm.GraphicsDevice, _mapManager);

        //Preload all graphics
        for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++) {
            if (ArtLoader.Instance.GetValidRefEntry(i).Equals(UOFileIndex.Invalid)) continue;
            ArtLoader.Instance.GetLandTexture((uint)i, out _);
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
        _uiManager.Draw();

        base.Draw(gameTime);
    }
    
        
    private void OnWindowResized(object? sender, EventArgs e) {
        GameWindow window = sender as GameWindow;
        if (window != null) 
            _mapManager.OnWindowsResized(window);
    }
}