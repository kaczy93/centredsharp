using System.Diagnostics.CodeAnalysis;
using CentrED.Blueprints;
using CentrED.Client;
using CentrED.Lights;
using CentrED.Network;
using CentrED.Renderer;
using CentrED.Renderer.Effects;
using CentrED.Tools;
using CentrED.UI.Windows;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Renderer.Arts;
using ClassicUO.Renderer.Texmaps;
using ClassicUO.Utility;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;
using static CentrED.Constants;
using Color = System.Drawing.Color;
using FNARectangle = Microsoft.Xna.Framework.Rectangle;
using FNAColor = Microsoft.Xna.Framework.Color;
using FNAVector2 = Microsoft.Xna.Framework.Vector2;
using FNAVector3 = Microsoft.Xna.Framework.Vector3;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.Map;

public class MapManager
{
    private readonly GraphicsDevice _gfxDevice;
    private readonly GameWindow _gameWindow;
    private readonly MapRenderer _mapRenderer;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _background;
    private readonly FontSystem _fontSystem;
    private readonly Keymap _keymap;

    private RenderTarget2D _selectionBuffer;
    public bool DebugDrawSelectionBuffer;
    private RenderTarget2D _lightMap;
    public bool DebugDrawLightMap;
    
    public MapEffect MapEffect { get; private set; }

    public UOFileManager UoFileManager { get; private set; }
    private AnimatedStaticsManager _animatedStaticsManager;
    public Art Arts;
    public Texmap Texmaps;
    public BlueprintManager BlueprintManager;

    internal List<Tool> Tools = [];
    private Tool _activeTool;

    public Tool ActiveTool
    {
        get => _activeTool;
        set
        {
            _activeTool.OnMouseLeave(Selected);
            _activeTool.OnDeactivated(Selected);
            _activeTool = value;
            _activeTool.OnActivated(Selected);
            _activeTool.OnMouseEnter(Selected);
        }
    }
    
    private readonly CentrEDClient Client;

    public bool ShowLand = true;
    public bool ShowStatics = true;
    public bool ShowVirtualLayer = false;
    public bool ShowNoDraw = false;
    public int VirtualLayerZ;
    public bool UseVirtualLayer = false;
    public bool WalkableSurfaces = false;
    public bool FlatView = false;
    public bool FlatShowHeight = false;
    public bool AnimatedStatics = true;
    public bool ShowGrid = false;
    public bool DebugLogging;
    public bool DebugInvalidTiles;

    public readonly Camera Camera = new();

    private DepthStencilState _DepthStencilState = new()
    {
        DepthBufferEnable = true,
        DepthBufferWriteEnable = true,
        DepthBufferFunction = CompareFunction.Less,
        StencilEnable = false
    };

    public int MinZ = -128;
    public int MaxZ = 127;

    public SortedSet<int> ObjectIdFilter = new();
    public bool ObjectIdFilterEnabled;
    public bool ObjectIdFilterInclusive = true;
    public SortedSet<int> ObjectHueFilter = new();
    public bool ObjectHueFilterEnabled;
    public bool ObjectHueFilterInclusive = true;
    public HashSet<LandObject> _ToRecalculate = new();

    public List<ushort> ValidLandIds { get; } = [];
    public List<ushort> ValidStaticIds { get; } = [];

    public MapManager(GraphicsDevice gd, GameWindow window, Keymap keymap)
    {
        _gfxDevice = gd;
        _gameWindow = window;
        _keymap = keymap;
        
        MapEffect = new MapEffect(gd);
        _mapRenderer = new MapRenderer(gd, window);
        _spriteBatch = new SpriteBatch(gd);
        _fontSystem = new FontSystem();
        
        _fontSystem.AddFont(File.ReadAllBytes("roboto.ttf"));
        
        using (var fileStream = File.OpenRead("background.png"))
        {
            _background = Texture2D.FromStream(gd, fileStream);
        }
        
        Client = CEDClient;
        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;
        EnableBlockLoading();
        Client.LandTileReplaced += OnLandTileReplaced;
        Client.LandTileElevated += OnLandTileElevated;
        Client.StaticTileAdded += tile =>
        {
            StaticsManager.Add(tile);
            MarkSelectionBufferDirty();
            MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
        };
        Client.StaticTileRemoved += tile =>
        {
            StaticsManager.Remove(tile);
            MarkSelectionBufferDirty();
            MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
        };
        Client.StaticTileMoved += (tile, x, y) =>
        {
            StaticsManager.Move(tile, x, y);
            MarkSelectionBufferDirty();
            MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
            MarkStaticRegionDirtyAtTile(x, y);
        };
        Client.StaticTileElevated += (tile, z) =>
        {
            StaticsManager.Elevate(tile, z);
            MarkSelectionBufferDirty();
            MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
        };
        Client.StaticTileHued += HueStatic;
        Client.AfterStaticChanged += AfterStaticChanged;
        Client.Moved += (x, y) => TilePosition = new Point(x,y);
        #if DEBUG
        Client.LoggedDebug += Console.WriteLine;
        Client.LoggedInfo += Console.WriteLine;
        Client.LoggedWarn += Console.WriteLine;
        Client.LoggedError += Console.WriteLine;
        #endif
        
        Tools.Add(new SelectTool()); //Select tool have to be first!
        Tools.Add(new DrawTool());
        Tools.Add(new MoveTool());
        Tools.Add(new ElevateTool());
        Tools.Add(new DeleteTool());
        Tools.Add(new HueTool());
        Tools.Add(new LandBrushTool());
        Tools.Add(new MeshEditTool());
        Tools.Add(new AltitudeGradientTool());
        Tools.Add(new CoastlineTool());
        
        Tools.ForEach(t => t.PostConstruct(this));

        _activeTool = Tools[0];
        OnWindowsResized(window);
    }
    
    private void OnConnected()
    {
        LandTiles = new LandObject[Client.WidthInTiles, Client.HeightInTiles];
        StaticsManager.Initialize(Client.WidthInTiles, Client.HeightInTiles);
        VirtualLayer.Width = Client.WidthInTiles;
        VirtualLayer.Height = Client.HeightInTiles;
        InitRegionCaches();
        _materializedBlocks.Clear();
        _materializationComplete = false;
        _hasLastMaterializeViewRange = false;
        _bgRadius = 0;
        _bgRingPos = 0;
        (_bgCenterX, _bgCenterY) = CameraBlock();
        _bgMaterializeDone = false;
        EvaluateMemoryTiers();
        _adaptiveMaterializeCap = MaxMaterializeCap;
        _lastInteractionFrame = long.MinValue;
        _cacheRateTimestamp = 0;
        _cacheRateLastCount = 0;
        _cacheBlocksPerSecond = 0;
        if (_backgroundFillEnabled)
        {
            Client.RequestAllBlocks(_bgCenterX, _bgCenterY);
        }
    }

    private long _debugAvailableMemoryOverrideBytes;
    public int DebugAvailableMemoryOverrideMB
    {
        get => (int)(_debugAvailableMemoryOverrideBytes / (1024 * 1024));
        set
        {
            _debugAvailableMemoryOverrideBytes = value > 0 ? (long)value * 1024 * 1024 : 0;
            EvaluateMemoryTiers(log: false);
            EvictMaterializedBlocksOutsideView();
        }
    }

    private void EvaluateMemoryTiers(bool log = true)
    {
        var available = _debugAvailableMemoryOverrideBytes > 0
            ? _debugAvailableMemoryOverrideBytes
            : GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        _totalAvailableMemoryBytes = available;

        var blocks = (long)Client.Width * Client.Height;
        var preloadBytes = blocks * EstimatedBytesPerBlock;
        var preloadFits = blocks > 0 && (available <= 0 || preloadBytes <= available * PreloadMemoryFraction);
        _backgroundFillEnabled = Config.Instance.PreloadMapOnConnect && preloadFits;

        var regionCacheBytes = blocks * EstimatedRegionCacheBytesPerBlock;
        _regionCacheEvictionEnabled = available > 0 && regionCacheBytes > available * RegionCacheKeepAllFraction;

        _materializeBudgetBlocks = available > 0
            ? Math.Max(64, (long)(available * MaterializeMemoryFraction / EstimatedBytesPerBlock))
            : long.MaxValue;
        _materializeEvictionEnabled = available > 0 && blocks > _materializeBudgetBlocks;

        if (!log)
            return;
        if (Config.Instance.PreloadMapOnConnect && !preloadFits)
        {
            Console.WriteLine(
                $"[MapManager] Full-map preload skipped: estimated {preloadBytes / (1024 * 1024)} MB for " +
                $"{Client.Width}x{Client.Height} blocks exceeds the safe budget of " +
                $"{(long)(available * PreloadMemoryFraction) / (1024 * 1024)} MB. Blocks will load on demand instead.");
        }
        if (_regionCacheEvictionEnabled)
        {
            Console.WriteLine(
                $"[MapManager] Region render caches will be evicted to a window around the view " +
                $"(estimated full-map cache {regionCacheBytes / (1024 * 1024)} MB vs " +
                $"{available / (1024 * 1024)} MB available).");
        }
        if (_materializeEvictionEnabled)
        {
            Console.WriteLine(
                $"[MapManager] Low memory for this map: capping materialized blocks at ~{_materializeBudgetBlocks:N0} " +
                $"(of {blocks:N0}); zoom-out is floored and off-screen blocks are released as you pan.");
        }
    }

    private float ComputeMinZoom()
    {
        if (!_materializeEvictionEnabled || _materializeBudgetBlocks <= 0)
            return 0f;
        double sum = Camera.ScreenSize.Width + Camera.ScreenSize.Height;
        if (sum <= 0)
            return 0f;
        var allowedLinearBlocks = Math.Max(8.0, Math.Sqrt(_materializeBudgetBlocks * ViewBudgetFraction));
        return (float)(2.0 * sum / (2.6 * TILE_SIZE * 8.0 * allowedLinearBlocks));
    }

    private void EnforceZoomFloor()
    {
        var minZoom = ComputeMinZoom();
        if (minZoom > 0f && Camera.Zoom < minZoom)
            Camera.Zoom = minZoom;
    }

    private void EvictMaterializedBlocksOutsideView()
    {
        if (!_materializeEvictionEnabled || _materializedBlocks.Count == 0)
            return;
        int bx1 = ViewRange.X1 / 8 - MaterializeKeepMarginBlocks;
        int by1 = ViewRange.Y1 / 8 - MaterializeKeepMarginBlocks;
        int bx2 = ViewRange.X2 / 8 + MaterializeKeepMarginBlocks;
        int by2 = ViewRange.Y2 / 8 + MaterializeKeepMarginBlocks;
        _blocksToDematerialize.Clear();
        foreach (var packed in _materializedBlocks)
        {
            int bx = packed >> 16, by = packed & 0xFFFF;
            if (bx < bx1 || bx > bx2 || by < by1 || by > by2)
                _blocksToDematerialize.Add(packed);
        }
        foreach (var packed in _blocksToDematerialize)
            DematerializeBlock(packed >> 16, packed & 0xFFFF);
    }

    private void DematerializeBlock(int bx, int by)
    {
        if (!_materializedBlocks.Remove(PackBlock(bx, by)))
            return;
        int minX = bx * 8, minY = by * 8;
        for (int x = minX; x < minX + 8; x++)
            for (int y = minY; y < minY + 8; y++)
                RemoveTiles((ushort)x, (ushort)y);
        MarkCacheRegionsDirtyForBlock(bx, by);
    }

    private (int bx, int by) CameraBlock()
    {
        int tx = (int)(Camera.Position.X / TILE_SIZE);
        int ty = (int)(Camera.Position.Y / TILE_SIZE);
        int bx = Math.Clamp(tx / 8, 0, Math.Max(0, Client.Width - 1));
        int by = Math.Clamp(ty / 8, 0, Math.Max(0, Client.Height - 1));
        return (bx, by);
    }

    private void MaybeRecenterBackgroundFill()
    {
        if (!_backgroundFillEnabled || _bgMaterializeDone)
            return;
        var (cbx, cby) = CameraBlock();
        if ((Math.Abs(cbx - _bgCenterX) > 16 || Math.Abs(cby - _bgCenterY) > 16) &&
            !_materializedBlocks.Contains(PackBlock(cbx, cby)))
        {
            _bgCenterX = cbx;
            _bgCenterY = cby;
            _bgRadius = 0;
            _bgRingPos = 0;
        }
    }

    private void OnDisconnected()
    {
        Reset();
    }

    public void EnableBlockLoading()
    {
        Client.BlockLoaded += OnBlockLoaded;
        Client.BlockUnloaded += OnBlockUnloaded;
    }

    public void DisableBlockLoading()
    {
        Client.BlockLoaded -= OnBlockLoaded;
        Client.BlockUnloaded -= OnBlockUnloaded;
    }

    private void OnBlockLoaded(Block block)
    {
        if (_materializedBlocks.Contains(PackBlock(block.LandBlock.X, block.LandBlock.Y)))
        {
            MaterializeBlock(block);
            return;
        }
        if (!IsBlockInViewRegion(block.LandBlock.X, block.LandBlock.Y))
        {
            return;
        }
        if (!TryMaterializeBlock(block))
        {
            _materializationComplete = false;
        }
    }

    private void MaterializeBlock(Block block)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        ClearBlock(block);
        foreach (var landTile in block.LandBlock.Tiles)
        {
            AddTile(landTile);
        }
        StaticsManager.AddRange(block.StaticBlock.AllTiles());
        //Recalculate tiles one and two tiles away from block, to fix corners and normals
        var landBlock = block.LandBlock;
        var minTileX = landBlock.X * 8;
        var maxTileX = minTileX + 7;
        var minTileY = landBlock.Y * 8;
        var maxTileY = minTileY + 7;
        for (var x = minTileX - 2; x <= maxTileX + 2; x++)
        {
            for (var y = minTileY - 2; y <= maxTileY + 2; y++)
            {
                if (!Client.IsValidX(x) || !Client.IsValidY(y))
                    continue;
                var tile = LandTiles?[x, y];
                if (tile != null)
                {
                    _ToRecalculate.Add(tile);
                }
            }
        }

        MarkCacheRegionsDirtyForBlock(landBlock.X, landBlock.Y);
        _materializedBlocks.Add(PackBlock(landBlock.X, landBlock.Y));
        if (IsBlockInViewRegion(landBlock.X, landBlock.Y))
            MarkSelectionBufferDirty();
        sw.Stop();
        _frameMaterializeMs += sw.Elapsed.TotalMilliseconds;
        _frameMaterializeCount++;
    }

    private bool CanMaterializeMore()
    {
        return _frameMaterializeCount < (int)_adaptiveMaterializeCap &&
               _frameMaterializeMs < MaterializeBudgetMs;
    }

    private void AdaptMaterializeCap()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var frameMs = _lastUpdateTimestamp == 0
            ? InteractiveTargetFrameMs
            : System.Diagnostics.Stopwatch.GetElapsedTime(_lastUpdateTimestamp, now).TotalMilliseconds;
        _lastUpdateTimestamp = now;

        var idle = _frameCounter - _lastInteractionFrame > IdleFramesBeforeFastFill;
        var targetMs = idle ? IdleTargetFrameMs : InteractiveTargetFrameMs;
        if (frameMs < targetMs)
            _adaptiveMaterializeCap = Math.Min(MaxMaterializeCap, _adaptiveMaterializeCap + 8);
        else
            _adaptiveMaterializeCap = Math.Max(MinMaterializeCap, _adaptiveMaterializeCap * 0.7);
    }

    private bool TryMaterializeBlock(Block block)
    {
        if (!CanMaterializeMore())
            return false;
        MaterializeBlock(block);
        return true;
    }

    private bool IsBlockInViewRegion(ushort blockX, ushort blockY)
    {
        var r = ViewRange;
        return blockX >= r.X1 / 8 && blockX <= r.X2 / 8 &&
               blockY >= r.Y1 / 8 && blockY <= r.Y2 / 8;
    }

    private void UpdateMaterializedRegion()
    {
        if (!Client.Running || _bgMaterializeDone)
            return;

        int bx1 = ViewRange.X1 / 8, by1 = ViewRange.Y1 / 8;
        int bx2 = ViewRange.X2 / 8, by2 = ViewRange.Y2 / 8;

        if (!_hasLastMaterializeViewRange || ViewRange != _lastMaterializeViewRange)
        {
            _lastMaterializeViewRange = ViewRange;
            _hasLastMaterializeViewRange = true;
            var (cbx, cby) = CameraBlock();
            _fgCenterX = Math.Clamp(cbx, bx1, bx2);
            _fgCenterY = Math.Clamp(cby, by1, by2);
            _fgRadius = 0;
            _fgRingPos = 0;
            _fgAnyUnmaterialized = false;
            _materializationComplete = false;
        }
        if (_materializationComplete)
            return;

        int maxR = Math.Max(Math.Max(_fgCenterX - bx1, bx2 - _fgCenterX),
                            Math.Max(_fgCenterY - by1, by2 - _fgCenterY));
        var scanned = 0;
        while (CanMaterializeMore() && scanned < MaterializeScanCap && _fgRadius <= maxR)
        {
            scanned++;
            SpiralBlock(_fgCenterX, _fgCenterY, _fgRadius, _fgRingPos, out var bx, out var by);
            var ringCount = _fgRadius == 0 ? 1 : 8 * _fgRadius;
            if (++_fgRingPos >= ringCount)
            {
                _fgRingPos = 0;
                _fgRadius++;
            }

            if (bx < bx1 || bx > bx2 || by < by1 || by > by2)
                continue;
            if (_materializedBlocks.Contains(PackBlock(bx, by)))
                continue;
            _fgAnyUnmaterialized = true;
            var block = Client.GetLoadedBlock((ushort)bx, (ushort)by);
            if (block != null)
                MaterializeBlock(block);
        }

        if (_fgRadius > maxR)
        {
            if (_fgAnyUnmaterialized)
            {
                _fgRadius = 0;
                _fgRingPos = 0;
                _fgAnyUnmaterialized = false;
            }
            else
            {
                _materializationComplete = true;
                UpdateLights();
            }
        }
    }

    private void BackgroundMaterializeStep()
    {
        if (_bgMaterializeDone || !_backgroundFillEnabled || !Client.Running)
            return;
        int w = Client.Width, h = Client.Height;
        if (w == 0 || h == 0)
            return;

        int cx = _bgCenterX, cy = _bgCenterY;
        int maxR = Math.Max(Math.Max(cx, w - 1 - cx), Math.Max(cy, h - 1 - cy));

        var scanned = 0;
        while (CanMaterializeMore() && scanned < MaterializeScanCap && _bgRadius <= maxR)
        {
            scanned++;
            SpiralBlock(cx, cy, _bgRadius, _bgRingPos, out var bx, out var by);

            var ringCount = _bgRadius == 0 ? 1 : 8 * _bgRadius;
            if (++_bgRingPos >= ringCount)
            {
                _bgRingPos = 0;
                _bgRadius++;
            }

            if (bx >= 0 && bx < w && by >= 0 && by < h &&
                !_materializedBlocks.Contains(PackBlock(bx, by)))
            {
                var block = Client.GetLoadedBlock((ushort)bx, (ushort)by);
                if (block != null)
                    MaterializeBlock(block);
            }
        }

        if (_bgRadius > maxR)
        {
            _bgRadius = 0;
            _bgRingPos = 0;
            if (_materializedBlocks.Count >= w * h)
            {
                _bgMaterializeDone = true;
                UpdateLights();
            }
        }
    }

    private static void SpiralBlock(int cx, int cy, int r, int pos, out int bx, out int by)
    {
        if (r == 0)
        {
            bx = cx; by = cy; return;
        }
        int side = 2 * r;
        if (pos < side)            { bx = cx - r + pos;        by = cy - r; }
        else if (pos < 2 * side)   { bx = cx + r;             by = cy - r + (pos - side); }
        else if (pos < 3 * side)   { bx = cx + r - (pos - 2 * side); by = cy + r; }
        else                       { bx = cx - r;             by = cy + r - (pos - 3 * side); }
    }

    public void EnsureRegionMaterialized(RectU16 region)
    {
        int bx1 = region.X1 / 8, by1 = region.Y1 / 8;
        int bx2 = region.X2 / 8, by2 = region.Y2 / 8;
        for (int bx = bx1; bx <= bx2; bx++)
        {
            for (int by = by1; by <= by2; by++)
            {
                if (_materializedBlocks.Contains(PackBlock(bx, by)))
                    continue;
                var block = Client.GetLoadedBlock((ushort)bx, (ushort)by);
                if (block != null)
                    MaterializeBlock(block);
            }
        }
    }

    private static int PackBlock(int bx, int by) => (bx << 16) | by;

    private void OnBlockUnloaded(Block block)
    {
        _materializedBlocks.Remove(PackBlock(block.LandBlock.X, block.LandBlock.Y));
        var tile = block.LandBlock.Tiles[0];
        if (ViewRange.Contains(tile.X, tile.Y))
        {
            return;
        }
        foreach (var landTile in block.LandBlock.Tiles)
        {
            RemoveTiles(landTile.X, landTile.Y);
        }
        block.Disposed = true;
        MarkSelectionBufferDirty();
        MarkCacheRegionsDirtyForBlock(block.LandBlock.X, block.LandBlock.Y);
    }

    private void OnLandTileReplaced(LandTile tile, ushort newId, sbyte newZ)
    {
        var landTile = LandTiles[tile.X, tile.Y];
        if (landTile != null)
        {
            _ToRecalculate.Add(landTile);
        }
        MarkSelectionBufferDirty();
        MarkCacheRegionsDirtyAtTile(tile.X, tile.Y);
    }

    public void OnLandTileElevated(LandTile tile, sbyte newZ)
    {
        RefreshLandTileNeighbors(tile);
        MarkCacheRegionsDirtyAtTile(tile.X, tile.Y);
    }

    public void RefreshLandTileNeighbors(LandTile tile)
    {
        for (int x = -2; x < 2; x++)
        {
            for (int y = -2; y < 2; y++)
            {
                var newX = tile.X + x;
                var newY = tile.Y + y;
                if (!Client.IsValidX(newX) || !Client.IsValidY(newY))
                    continue;

                var landObject = LandTiles[newX, newY];
                if (landObject != null)
                {
                    _ToRecalculate.Add(landObject);
                }
            }
        }
        MarkSelectionBufferDirty();
    }

    public void ClearGhosts()
    {
        foreach (var parent in GhostLandTiles.Keys)
        {
            parent.Reset();
            RefreshLandTileNeighbors(parent.LandTile);
        }
        GhostLandTiles.Clear();
        StaticsManager.ClearGhosts();
    }
    
    private void HueStatic(StaticTile tile, ushort newHue)
    {
        StaticsManager.Get(tile)?.UpdateHue(newHue);
        MarkSelectionBufferDirty();
        MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
    }

    private void AfterStaticChanged(StaticTile tile)
    {
        foreach (var staticObject in StaticsManager.Get(tile.X, tile.Y))
        {
            staticObject.UpdateDepthOffset();
        }
        MarkSelectionBufferDirty();
        MarkStaticRegionDirtyAtTile(tile.X, tile.Y);
    }

    private void AddTile(LandTile landTile)
    {
        var lo = new LandObject(landTile);
        LandTiles[landTile.X, landTile.Y] = lo;
        LandTilesIdDictionary.Add(lo.ObjectId, lo);
        LandTilesCount++;
    }

    public void ReloadShader()
    {
        if(File.Exists("MapEffect.fxc")) 
            MapEffect = new MapEffect(_gfxDevice, File.ReadAllBytes("MapEffect.fxc"));
    }

    public void Load(string clientPath)
    {
        var tiledataFile = Path.Combine(clientPath, "tiledata.mul");
        var clientVersion = new FileInfo(tiledataFile).Length switch
        {
            >= 3188736 => ClientVersion.CV_7090,
            >= 1644544 => ClientVersion.CV_7000,
            _ => ClientVersion.CV_6000
        };
        UoFileManager = new UOFileManager(clientVersion, clientPath);
        //We don't UoFileManager.Load() as we don't need all the assets
        UoFileManager.Arts.Load();
        UoFileManager.Hues.Load();
        UoFileManager.TileData.Load();
        UoFileManager.Texmaps.Load();
        UoFileManager.AnimData.Load();
        UoFileManager.Lights.Load();
        UoFileManager.Multis.Load();
        
        _animatedStaticsManager = new AnimatedStaticsManager();
        _animatedStaticsManager.Initialize();
        Arts = new Art(UoFileManager.Arts, UoFileManager.Hues, _gfxDevice);
        Texmaps = new Texmap(UoFileManager.Texmaps, _gfxDevice);
        HuesManager.Load(_gfxDevice);
        LightsManager.Load(_gfxDevice);

        var tdl = UoFileManager.TileData;
        ValidLandIds.Clear();
        for (var i = 0; i < tdl.LandData.Length; i++)
        {
            var isArtValid = CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(i).Length > 0;

            var texId = tdl.LandData[i].TexID;
            var isTexValid = CEDGame.MapManager.UoFileManager.Texmaps.File.GetValidRefEntry(texId).Length > 0;

            // Only show tiles if art OR texture is valid
            if (isArtValid || isTexValid)
            {
                ValidLandIds.Add((ushort)i);
            }
        }
        ValidStaticIds.Clear();
        for (var i = 0; i < tdl.StaticData.Length; i++)
        {
            if (!UoFileManager.Arts.File.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals
                    (UOFileIndex.Invalid))
            {
                ValidStaticIds.Add((ushort)i);
            }
        }
        var landTileData = tdl.LandData.Select(ltd => new TileDataLand((ulong)ltd.Flags, ltd.TexID, ltd.Name)).ToArray();
        var staticTileData = tdl.StaticData.Select(std => new TileDataStatic((ulong)std.Flags, std.Weight, std.Layer, std.Count, std.AnimID, std.Hue, std.LightIndex, std.Height, std.Name)).ToArray(); 
        Client.InitTileData(landTileData, staticTileData);

        BlueprintManager = new BlueprintManager(UoFileManager.Multis);
        BlueprintManager.Load();
    }

    public Vector2 Position
    {
        get => new(Camera.Position.X, Camera.Position.Y);
        set
        {
            var maxX = Client.WidthInTiles * TILE_SIZE;
            var maxY = Client.HeightInTiles * TILE_SIZE;
            Camera.Position.X = maxX > 0 ? Math.Clamp(value.X, 0, maxX) : value.X;
            Camera.Position.Y = maxY > 0 ? Math.Clamp(value.Y, 0, maxY) : value.Y;
            Client.InternalSetPos((ushort)(Camera.Position.X / TILE_SIZE), (ushort)(Camera.Position.Y / TILE_SIZE));
        }
    }

    public void Move(float xDelta, float yDelta)
    {
        var oldPos = (Position.X, Position.Y);
        Position = new Vector2(oldPos.X + xDelta, oldPos.Y + yDelta);
    }

    public Point TilePosition
    {
        get => new(Client.X, Client.Y);
        set
        {
            Camera.Position.X = value.X * TILE_SIZE;
            Camera.Position.Y = value.Y * TILE_SIZE;
            Client.InternalSetPos((ushort)value.X, (ushort)value.Y);
        }
    }

    public static Vector2 ScreenToMapCoordinates(float x, float y)
    {
        return new Vector2(x * RSQRT2 - y * -RSQRT2, x * -RSQRT2 + y * RSQRT2);
    }

    private Dictionary<int, TileObject> LandTilesIdDictionary = new();
    
    public LandObject?[,] LandTiles;
    public int LandTilesCount;
    public Dictionary<LandObject, LandObject> GhostLandTiles = new();

    public StaticsManager StaticsManager = new();
    public VirtualLayerObject VirtualLayer = VirtualLayerObject.Instance; //Used for drawing
    public ImageOverlay ImageOverlay = new(); //Used for image overlay feature

    private bool _selectionBufferDirty = true;
    private long _lastCameraMotionFrame = long.MinValue;
    private const int SelectionFullViewMaxTiles = 40000;
    private const int SelectionWindowRadius = 64;
    private int _lastSelectionMouseX = int.MinValue;
    private int _lastSelectionMouseY = int.MinValue;
    private bool _selectionCameraInitialized;
    private Vector3 _lastSelectionCameraPosition;
    private float _lastSelectionZoom;
    private float _lastSelectionYaw;
    private float _lastSelectionPitch;
    private float _lastSelectionRoll;
    private Rectangle _lastSelectionScreenSize;
    private int _lastSelectionStateSignature;
    private readonly FNAColor[] _selectionPixel = new FNAColor[1];
    private long _detailedObjectsCulled;
    private long _frameCounter;
    private const float LowZoomTerrainThreshold = 0.22f;
    private const float LowZoomStaticThreshold = 0.35f;
    private const float AnimatedStaticMinZoom = 0.2f;
    private const float StaticCacheMinTextureSize = 30f;

    private const int RegionBlocks = 32;
    private const int MaxRegionBuildsPerFrame = 64;
    private const double RegionBuildBudgetMs = 6.0;
    private const int RegionCacheKeepMargin = 1;
    private const long EstimatedRegionCacheBytesPerBlock = 24 * 1024;
    private const double RegionCacheKeepAllFraction = 0.25;
    private static readonly int MapVertexSizeBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<MapVertex>();
    private bool _regionCacheEvictionEnabled;
    private long _cachedRegionVertexBytes;
    private long _totalAvailableMemoryBytes;
    private int _regionsX, _regionsY;
    private List<CachedRenderBatch>?[] _terrainRegions = Array.Empty<List<CachedRenderBatch>?>();
    private List<CachedRenderBatch>?[] _staticRegions = Array.Empty<List<CachedRenderBatch>?>();
    private bool[] _terrainRegionDirty = Array.Empty<bool>();
    private bool[] _staticRegionDirty = Array.Empty<bool>();
    private int _terrainCacheSignature;
    private int _staticCacheSignature;
    private bool _forceFullTerrainCache;
    private bool _forceFullStaticCache;
    private readonly List<int> _visibleDirtyRegions = new();
    private int _terrainRegionsDrawn, _terrainRegionsBuilt, _staticRegionsDrawn, _staticRegionsBuilt;

    private readonly HashSet<int> _materializedBlocks = new();
    private const double MaterializeMemoryFraction = 0.5;
    private const int MaterializeKeepMarginBlocks = 16;
    private const double ViewBudgetFraction = 0.6;
    private long _materializeBudgetBlocks = long.MaxValue;
    private bool _materializeEvictionEnabled;
    private readonly List<int> _blocksToDematerialize = new();
    private const int MaterializeScanCap = 8192;
    private const double MaterializeBudgetMs = 70.0;
    private const int MinMaterializeCap = 4;
    private const int MaxMaterializeCap = 512;
    private const int IdleFramesBeforeFastFill = 12;
    private const double InteractiveTargetFrameMs = 14.0;
    private const double IdleTargetFrameMs = 50.0;
    private double _adaptiveMaterializeCap = MaxMaterializeCap;
    private long _lastUpdateTimestamp;
    private long _lastInteractionFrame = long.MinValue;
    private double _frameMaterializeMs;
    private int _frameMaterializeCount;
    private RectU16 _lastMaterializeViewRange;
    private bool _hasLastMaterializeViewRange;
    private bool _materializationComplete;
    private int _fgRadius;
    private int _fgRingPos;
    private int _fgCenterX;
    private int _fgCenterY;
    private bool _fgAnyUnmaterialized;
    private bool _backgroundFillEnabled;
    private int _bgRadius;
    private int _bgRingPos;
    private int _bgCenterX;
    private int _bgCenterY;
    private bool _bgMaterializeDone;
    private long _cacheRateTimestamp;
    private int _cacheRateLastCount;
    private double _cacheBlocksPerSecond;

    private const long EstimatedBytesPerBlock = 48 * 1024;
    private const double PreloadMemoryFraction = 0.5;

    public bool CacheInProgress =>
        Client.Running && _backgroundFillEnabled && !_bgMaterializeDone && Client.Width > 0;
    public int CacheMaterializedBlocks => _materializedBlocks.Count;
    public int CacheTotalBlocks => Client.Width * Client.Height;
    public float CacheProgress
    {
        get
        {
            var total = CacheTotalBlocks;
            return total > 0 ? Math.Clamp((float)_materializedBlocks.Count / total, 0f, 1f) : 0f;
        }
    }
    public double CacheEtaSeconds
    {
        get
        {
            if (_cacheBlocksPerSecond <= 1.0)
                return -1;
            var remaining = CacheTotalBlocks - _materializedBlocks.Count;
            return remaining <= 0 ? 0 : remaining / _cacheBlocksPerSecond;
        }
    }

    private void UpdateCacheRate()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_cacheRateTimestamp == 0)
        {
            _cacheRateTimestamp = now;
            _cacheRateLastCount = _materializedBlocks.Count;
            return;
        }
        var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(_cacheRateTimestamp, now).TotalSeconds;
        if (elapsed < 0.25)
            return;
        var delta = _materializedBlocks.Count - _cacheRateLastCount;
        var instRate = delta / elapsed;
        _cacheBlocksPerSecond = _cacheBlocksPerSecond <= 0 ? instRate : _cacheBlocksPerSecond * 0.7 + instRate * 0.3;
        _cacheRateTimestamp = now;
        _cacheRateLastCount = _materializedBlocks.Count;
    }

    private sealed class CachedRenderBatch : IDisposable
    {
        public required Texture2D Texture { get; init; }
        public required VertexBuffer VertexBuffer { get; init; }
        public required int VertexCount { get; init; }
        public required int PrimitiveCount { get; init; }

        public void Dispose()
        {
            VertexBuffer.Dispose();
        }
    }

    public void UpdateAllTiles()
    {
        foreach (var tile in LandTilesIdDictionary.Values)
        {
            if (tile is LandObject lo)
            {
                lo.Update();
            }
        }
        StaticsManager.UpdateAll();
        MarkTerrainCacheDirty();
        MarkStaticCacheDirty();
    }

    public LandTile? GetLandTile(int x, int y)
    {
        if (!Client.IsValidX(x) || !Client.IsValidY(y))
        {
            return null;
        }
        var realLandTile = LandTiles[x, y];
        if (realLandTile == null)
            return null;
        if (GhostLandTiles.TryGetValue(realLandTile, out var ghostLandTile))
        {
            return ghostLandTile.LandTile;
        }
        return realLandTile.LandTile;
    }

    public bool TryGetLandTile(int x, int y, [MaybeNullWhen(false)] out LandTile result)
    {
        result = GetLandTile(x, y);
        return result != null;
    }

    public void ClearBlock(Block block)
    {
        var minX = block.LandBlock.X * 8;
        var maxX = minX + 8;
        var minY = block.LandBlock.Y * 8;
        var maxY = minY + 8;
        for (var x = minX; x < maxX; x++)
        {
            for (var y = minY; y < maxY; y++)
            {
                RemoveTiles((ushort)x, (ushort)y);
            }
        }
    }

    public void RemoveTiles(ushort x, ushort y)
    {
        var lo = LandTiles[x, y];
        if (lo != null)
        {
            LandTiles[x, y] = null;
            LandTilesIdDictionary.Remove(lo.ObjectId);
            LandTilesCount--;
        }
        StaticsManager.Remove(x, y);
    }

    public IEnumerable<TileObject> GetTiles(TileObject? t1, TileObject? t2, bool topTilesOnly)
    {
        if (t1 == null || t2 == null)
            yield break;
        var mx = t1.Tile.X < t2.Tile.X ? (t1.Tile.X, t2.Tile.X) : (t2.Tile.X, t1.Tile.X);
        var my = t1.Tile.Y < t2.Tile.Y ? (t1.Tile.Y, t2.Tile.Y) : (t2.Tile.Y, t1.Tile.Y);
        for (var x = mx.Item1; x <= mx.Item2; x++)
        {
            for (var y = my.Item1; y <= my.Item2; y++)
            {
                if (UseVirtualLayer)
                {
                    yield return new VirtualLayerTile(x, y, (sbyte)VirtualLayerZ);
                }
                else
                {
                    var staticTiles = StaticsManager.Get(x, y).Where(so => CanDrawStatic(so, includeBuried: true));
                    if (topTilesOnly)
                    {
                        var topTile = staticTiles.LastOrDefault();
                        if (topTile != null)
                        {
                            yield return topTile;
                            continue;
                        }
                    }
                    else
                    {
                        foreach (var tile in staticTiles)
                        {
                            yield return tile;
                        }
                    }
                    var landTile = LandTiles[x, y];
                    if (landTile != null && CanDrawLand(landTile))
                    {
                        yield return landTile;
                    }
                }
            }
        }
    }
    
    private MouseState _prevMouseState = Mouse.GetState();
    private bool _IsMouseDragging;
    public RectU16 ViewRange { get; private set; }

    public void Update(GameTime gameTime, bool isActive, bool processMouse, bool processKeyboard)
    {
        if (CEDGame.Closing)
            return;
        if (CEDClient.ServerState != ServerState.Running)
            return;
        
        Metrics.Start("UpdateMap");
        _frameCounter++;
        _frameMaterializeMs = 0;
        _frameMaterializeCount = 0;
        var mouseState = Mouse.GetState();
        var movementKeys = _keymap.IsActionDown(Keymap.MoveLeft) || _keymap.IsActionDown(Keymap.MoveRight) ||
                           _keymap.IsActionDown(Keymap.MoveUp) || _keymap.IsActionDown(Keymap.MoveDown);
        if (mouseState.X != _prevMouseState.X || mouseState.Y != _prevMouseState.Y ||
            mouseState.LeftButton == ButtonState.Pressed || mouseState.RightButton == ButtonState.Pressed ||
            mouseState.MiddleButton == ButtonState.Pressed ||
            mouseState.ScrollWheelValue != _prevMouseState.ScrollWheelValue || movementKeys)
        {
            _lastInteractionFrame = _frameCounter;
        }
        AdaptMaterializeCap();
        if (processMouse)
        {
            if (Client.Running)
            {
                if (PrevSelected != Selected)
                {
                    if (DebugLogging)
                    {
                        Console.WriteLine($"New selected: {Selected?.Tile}");
                    }
                    ActiveTool.OnMouseLeave(PrevSelected);
                    PrevSelected = Selected;
                    ActiveTool.OnMouseEnter(Selected);
                }
            }
            if (isActive)
            {
                if (Selected != null)
                {
                    if (_prevMouseState.LeftButton == ButtonState.Released && mouseState.LeftButton == ButtonState.Pressed)
                    {
                        ActiveTool.OnMousePressed(Selected);
                    }
                }
                if (_prevMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released)
                {
                    ActiveTool.OnMouseReleased(Selected);
                    ActiveTool.OnMouseLeave(Selected); //Make sure that we leave tile to clear any ghosts
                    Selected = null; //Very dirty way to retrigger OnMouseEnter() after something presumably changed
                }
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    var mouseDelta = new Vector2(_prevMouseState.X - mouseState.X, _prevMouseState.Y - mouseState.Y);
                    if (mouseDelta != Vector2.Zero)
                    {
                        var moveOffset = ScreenToMapCoordinates(mouseDelta.X, mouseDelta.Y) / Camera.Zoom;
                        Move(moveOffset.X, moveOffset.Y);
                        _IsMouseDragging = true;
                    }
                }
                if (mouseState.RightButton == ButtonState.Released)
                {
                    if (_IsMouseDragging)
                    {
                        _IsMouseDragging = false;
                    }
                    else if (!_IsMouseDragging && _prevMouseState.RightButton == ButtonState.Pressed)
                    {
                        CEDGame.UIManager.OpenContextMenu(RealSelected);
                    }
                }
                if (mouseState.MiddleButton == ButtonState.Pressed)
                {
                    var mouseDelta = new Vector2(_prevMouseState.X - mouseState.X, _prevMouseState.Y - mouseState.Y);
                    if (mouseDelta != Vector2.Zero)
                    {
                        var mod = 0.5f;
                        Camera.Pitch -= mouseDelta.Y * mod;
                        Camera.Roll += mouseDelta.X * mod;
                    }
                }
                if (mouseState.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
                {
                    var scrollDelta = (mouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue) / 1200f;
                    if (Config.Instance.LegacyMouseScroll ^ (_keymap.IsKeyDown(Keys.LeftControl) || _keymap.IsKeyDown
                            (Keys.RightControl)))
                    {
                        if (Selected != null)
                            Selected.Tile.Z += (sbyte)(scrollDelta * 10);
                    }
                    else
                    {
                        Camera.ZoomIn(scrollDelta);
                    }
                }
            }
            else
            {
                ActiveTool.OnMouseReleased(Selected);
            }
        }
        else
        {
            ActiveTool.OnMouseLeave(PrevSelected);
            ActiveTool.OnMouseReleased(PrevSelected);
            Selected = null;
        }
        _prevMouseState = mouseState;

        if (processKeyboard)
        {
            foreach (var key in _keymap.GetKeysReleased())
            {
                ActiveTool.OnKeyReleased(key);
            }
            if (isActive)
            {
                var delta = _keymap.IsKeyDown(Keys.LeftShift) ? 30 : 10;

                foreach (var key in _keymap.GetKeysPressed())
                {
                    ActiveTool.OnKeyPressed(key);
                }

                if (mouseState.LeftButton == ButtonState.Released)
                {
                    foreach (var tool in Tools)
                    {
                        if (_keymap.IsKeyPressed(tool.Shortcut))
                        {
                            if (tool == ActiveTool)
                                tool.OpenPopup();
                            else
                            {
                                tool.ClosePopup();
                                ActiveTool = tool;
                            }
                            break;
                        }
                    }
                }
                if (_keymap.IsActionPressed(Keymap.ToggleAnimatedStatics))
                {
                    AnimatedStatics = !AnimatedStatics;
                }
                if (_keymap.IsActionPressed(Keymap.Minimap))
                {
                    var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
                    minimapWindow.Show = !minimapWindow.Show;
                }
                else
                {
                    if (_keymap.IsKeyDown(Keys.LeftControl) || _keymap.IsKeyDown(Keys.RightControl))
                    {
                        if (_keymap.IsKeyDown(Keys.LeftShift) || _keymap.IsKeyDown(Keys.RightShift))
                        {
                            if (_keymap.IsKeyPressed(Keys.Z))
                            {
                                ClearGhosts();
                                Client.Redo();
                            }
                        }
                        else if (_keymap.IsKeyPressed(Keys.Z))
                        {
                            ClearGhosts();
                            Client.Undo();
                        }

                        if (_keymap.IsKeyPressed(Keys.R))
                        {
                            ReloadView();
                        }
                        if (_keymap.IsKeyPressed(Keys.W))
                        {
                            WalkableSurfaces = !WalkableSurfaces;
                        }
                        if (_keymap.IsKeyPressed(Keys.F))
                        {
                            FlatView = !FlatView;
                            UpdateAllTiles();
                        }
                        if (_keymap.IsKeyPressed(Keys.H))
                        {
                            FlatShowHeight = !FlatShowHeight;
                        }
                        if (_keymap.IsKeyPressed(Keys.G))
                        {
                            ShowGrid = !ShowGrid;
                        }
                    }
                    else
                    {
                        if (_keymap.IsKeyPressed(Keys.Escape))
                        {
                            Camera.ResetCamera();
                        }
                        if (_keymap.IsActionDown(Keymap.MoveLeft))
                        {
                            Move(-delta, delta);
                        }
                        if (_keymap.IsActionDown(Keymap.MoveRight))
                        {
                            Move(delta, -delta);
                        }
                        if (_keymap.IsActionDown(Keymap.MoveUp))
                        {
                            Move(-delta, -delta);
                        }
                        if (_keymap.IsActionDown(Keymap.MoveDown))
                        {
                            Move(delta, delta);
                        }
                    }
                }
            }
        }

        EnforceZoomFloor();
        Camera.Update();
        TrackSelectionInvalidation();
        var viewRangeChanged = false;
        if (Client.Running)
        {
            var newViewRange = CalculateViewRange(Camera);
            if (ViewRange != newViewRange)
            {
                viewRangeChanged = true;
                ViewRange = newViewRange;
                if (!_bgMaterializeDone)
                    Metrics.Measure("RequestBlocks", () => Client.RequestBlocks(ViewRange));
                MarkSelectionBufferDirty();
            }
        }
        else
        {
            ViewRange = default;
        }
        Metrics.SetCounter("ViewRangeChanged", viewRangeChanged ? 1 : 0);
        if (Client.Running)
        {
            Metrics.Measure("Materialize", () =>
            {
                UpdateMaterializedRegion();
                MaybeRecenterBackgroundFill();
                BackgroundMaterializeStep();
            });
            UpdateCacheRate();
            if (viewRangeChanged)
                Metrics.Measure("DematerializeOutsideView", EvictMaterializedBlocksOutsideView);
        }
        if (Client.Running && AnimatedStatics && Camera.Zoom >= AnimatedStaticMinZoom)
        {
            _animatedStaticsManager.Process(gameTime);
            foreach (var animatedStaticTile in StaticsManager.AnimatedTiles)
            {
                animatedStaticTile.UpdateId();
                animatedStaticTile.Update();
            }
        }
        if (_ToRecalculate.Count > 0)
        {
            MarkSelectionBufferDirty();
        }
        foreach (var landObject in _ToRecalculate)
        {
            if (GhostLandTiles.TryGetValue(landObject, out var ghostLandObject))
            {
                ghostLandObject.Update();
            }
            landObject.Update();
        }
        _ToRecalculate.Clear();
        Metrics.Stop("UpdateMap");
    }

    public void Reset()
    {
        LandTilesCount = 0;
        
        LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
        LandTilesIdDictionary.Clear();
        ClearRegionCaches();
        PrevSelected = null;
        Selected = null;
        RealSelected = null;
        GhostLandTiles.Clear();
        StaticsManager.Clear();
        ViewRange = default;
        _materializedBlocks.Clear();
        _materializationComplete = false;
        _hasLastMaterializeViewRange = false;
        _bgRadius = 0;
        _bgRingPos = 0;
        (_bgCenterX, _bgCenterY) = CameraBlock();
        _bgMaterializeDone = false;
        _backgroundFillEnabled = false;
        MarkSelectionBufferDirty();
        Client.ResetCache();
    }

    private const int ReloadViewMaxBlocks = 4096;

    public void ReloadView()
    {
        if (!Client.Running)
            return;
        int bx1 = ViewRange.X1 / 8, by1 = ViewRange.Y1 / 8;
        int bx2 = ViewRange.X2 / 8, by2 = ViewRange.Y2 / 8;
        if ((long)(bx2 - bx1 + 1) * (by2 - by1 + 1) > ReloadViewMaxBlocks)
            return;
        for (int bx = bx1; bx <= bx2; bx++)
        {
            for (int by = by1; by <= by2; by++)
            {
                var block = Client.GetLoadedBlock((ushort)bx, (ushort)by);
                if (block != null)
                    MaterializeBlock(block);
            }
        }
        MarkSelectionBufferDirty();
    }

    public void UpdateLights()
    {
        foreach (var light in StaticsManager.LightTiles.Values)
        {
            light.Update();   
        }
    }

    private void MarkSelectionBufferDirty()
    {
        _selectionBufferDirty = true;
    }

    private void MarkTerrainCacheDirty()
    {
        if (_terrainRegionDirty.Length > 0)
            Array.Fill(_terrainRegionDirty, true);
    }

    private void MarkStaticCacheDirty()
    {
        if (_staticRegionDirty.Length > 0)
            Array.Fill(_staticRegionDirty, true);
    }

    private void InitRegionCaches()
    {
        ClearRegionCaches();
        _regionsX = Math.Max(1, (Client.Width + RegionBlocks - 1) / RegionBlocks);
        _regionsY = Math.Max(1, (Client.Height + RegionBlocks - 1) / RegionBlocks);
        var n = _regionsX * _regionsY;
        _terrainRegions = new List<CachedRenderBatch>?[n];
        _staticRegions = new List<CachedRenderBatch>?[n];
        _terrainRegionDirty = new bool[n];
        _staticRegionDirty = new bool[n];
        Array.Fill(_terrainRegionDirty, true);
        Array.Fill(_staticRegionDirty, true);
        _terrainCacheSignature = GetTerrainCacheSignature();
        _staticCacheSignature = GetStaticCacheSignature();
    }

    private void ClearRegionCaches()
    {
        foreach (var batches in _terrainRegions)
            DisposeRegionBatches(batches);
        foreach (var batches in _staticRegions)
            DisposeRegionBatches(batches);
        _terrainRegions = Array.Empty<List<CachedRenderBatch>?>();
        _staticRegions = Array.Empty<List<CachedRenderBatch>?>();
        _terrainRegionDirty = Array.Empty<bool>();
        _staticRegionDirty = Array.Empty<bool>();
        _regionsX = _regionsY = 0;
        _cachedRegionVertexBytes = 0;
    }

    private void DisposeRegionBatches(List<CachedRenderBatch>? batches)
    {
        if (batches == null)
            return;
        foreach (var b in batches)
        {
            _cachedRegionVertexBytes -= (long)b.VertexCount * MapVertexSizeBytes;
            b.Dispose();
        }
    }

    private void EvictRegionCachesOutsideWindow(
        List<CachedRenderBatch>?[] regions, bool[] dirty, int rx1, int rx2, int ry1, int ry2)
    {
        if (!_regionCacheEvictionEnabled || _regionsX == 0)
            return;
        int kx1 = rx1 - RegionCacheKeepMargin, kx2 = rx2 + RegionCacheKeepMargin;
        int ky1 = ry1 - RegionCacheKeepMargin, ky2 = ry2 + RegionCacheKeepMargin;
        for (int idx = 0; idx < regions.Length; idx++)
        {
            if (regions[idx] == null)
                continue;
            int rx = idx / _regionsY, ry = idx % _regionsY;
            if (rx >= kx1 && rx <= kx2 && ry >= ky1 && ry <= ky2)
                continue;
            DisposeRegionBatches(regions[idx]);
            regions[idx] = null;
            dirty[idx] = true;
        }
    }

    private int RegionIndex(int rx, int ry) => rx * _regionsY + ry;

    private (int rx, int ry) CameraRegion()
    {
        var (bx, by) = CameraBlock();
        return (Math.Clamp(bx / RegionBlocks, 0, Math.Max(0, _regionsX - 1)),
                Math.Clamp(by / RegionBlocks, 0, Math.Max(0, _regionsY - 1)));
    }

    private void MarkCacheRegionsDirtyForBlock(int bx, int by)
    {
        if (_terrainRegionDirty.Length == 0)
            return;
        for (int dbx = -1; dbx <= 1; dbx++)
        {
            for (int dby = -1; dby <= 1; dby++)
            {
                int nbx = bx + dbx, nby = by + dby;
                if (nbx < 0 || nby < 0 || nbx >= Client.Width || nby >= Client.Height)
                    continue;
                var idx = RegionIndex(nbx / RegionBlocks, nby / RegionBlocks);
                _terrainRegionDirty[idx] = true;
                _staticRegionDirty[idx] = true;
            }
        }
    }

    private void MarkCacheRegionsDirtyAtTile(int tileX, int tileY)
    {
        MarkCacheRegionsDirtyForBlock(tileX / 8, tileY / 8);
    }

    private void MarkStaticRegionDirtyAtTile(int tileX, int tileY)
    {
        if (_staticRegionDirty.Length == 0)
            return;
        int bx = tileX / 8, by = tileY / 8;
        if (bx < 0 || by < 0 || bx >= Client.Width || by >= Client.Height)
            return;
        _staticRegionDirty[RegionIndex(bx / RegionBlocks, by / RegionBlocks)] = true;
    }

    private int GetTerrainCacheSignature()
    {
        var hash = new HashCode();
        hash.Add(ShowLand);
        hash.Add(ShowNoDraw);
        hash.Add(FlatView);
        hash.Add(MinZ);
        hash.Add(MaxZ);
        return hash.ToHashCode();
    }

    private int RegionDistance(int idx, int rx, int ry)
    {
        return Math.Max(Math.Abs(idx / _regionsY - rx), Math.Abs(idx % _regionsY - ry));
    }

    private void BuildTerrainRegion(int idx, int rx, int ry)
    {
        var batches = _terrainRegions[idx];
        if (batches == null)
            _terrainRegions[idx] = batches = new List<CachedRenderBatch>();
        else
        {
            DisposeRegionBatches(batches);
            batches.Clear();
        }
        _terrainRegionDirty[idx] = false;
        if (!ShowLand || LandTiles == null)
            return;

        var accum = new Dictionary<Texture2D, List<MapVertex>>();
        int bx0 = rx * RegionBlocks, by0 = ry * RegionBlocks;
        int bx1 = Math.Min(bx0 + RegionBlocks, Client.Width);
        int by1 = Math.Min(by0 + RegionBlocks, Client.Height);
        for (int bx = bx0; bx < bx1; bx++)
        {
            for (int by = by0; by < by1; by++)
            {
                if (!_materializedBlocks.Contains(PackBlock(bx, by)))
                    continue;
                int minX = bx * 8, minY = by * 8;
                for (int x = minX; x < minX + 8; x++)
                {
                    for (int y = minY; y < minY + 8; y++)
                    {
                        var lo = LandTiles[x, y];
                        if (lo == null || !lo.CanDraw || !CanDrawLand(lo))
                            continue;
                        if (!accum.TryGetValue(lo.Texture, out var list))
                            accum[lo.Texture] = list = new List<MapVertex>();
                        list.AddRange(lo.Vertices);
                    }
                }
            }
        }
        foreach (var (texture, vertices) in accum)
        {
            if (vertices.Count == 0)
                continue;
            var batch = BuildRenderBatch(texture, vertices);
            _cachedRegionVertexBytes += (long)batch.VertexCount * MapVertexSizeBytes;
            batches.Add(batch);
        }
    }

    private void DrawCachedTerrainRegions(RectU16 viewRange)
    {
        if (_regionsX == 0 || _terrainRegions.Length == 0)
            return;

        var signature = GetTerrainCacheSignature();
        if (signature != _terrainCacheSignature)
        {
            _terrainCacheSignature = signature;
            MarkTerrainCacheDirty();
        }

        int rx1 = Math.Clamp(viewRange.X1 / 8 / RegionBlocks - 1, 0, _regionsX - 1);
        int rx2 = Math.Clamp(viewRange.X2 / 8 / RegionBlocks + 1, 0, _regionsX - 1);
        int ry1 = Math.Clamp(viewRange.Y1 / 8 / RegionBlocks - 1, 0, _regionsY - 1);
        int ry2 = Math.Clamp(viewRange.Y2 / 8 / RegionBlocks + 1, 0, _regionsY - 1);

        var (cbrx, cbry) = CameraRegion();
        _visibleDirtyRegions.Clear();
        for (int rx = rx1; rx <= rx2; rx++)
            for (int ry = ry1; ry <= ry2; ry++)
                if (_terrainRegionDirty[RegionIndex(rx, ry)])
                    _visibleDirtyRegions.Add(RegionIndex(rx, ry));
        if (_visibleDirtyRegions.Count > 0)
        {
            _visibleDirtyRegions.Sort((a, b) =>
                RegionDistance(a, cbrx, cbry).CompareTo(RegionDistance(b, cbrx, cbry)));
            var buildStart = System.Diagnostics.Stopwatch.GetTimestamp();
            int built = 0;
            foreach (var idx in _visibleDirtyRegions)
            {
                if (!_forceFullTerrainCache && built > 0 &&
                    (built >= MaxRegionBuildsPerFrame ||
                     System.Diagnostics.Stopwatch.GetElapsedTime(buildStart).TotalMilliseconds >= RegionBuildBudgetMs))
                    break;
                BuildTerrainRegion(idx, idx / _regionsY, idx % _regionsY);
                built++;
            }
            _terrainRegionsBuilt = built;
        }
        else
        {
            _terrainRegionsBuilt = 0;
        }

        _mapRenderer.FlushPending();
        int drawn = 0;
        for (int rx = rx1; rx <= rx2; rx++)
        {
            for (int ry = ry1; ry <= ry2; ry++)
            {
                var batches = _terrainRegions[RegionIndex(rx, ry)];
                if (batches == null)
                    continue;
                foreach (var batch in batches)
                    _mapRenderer.DrawCachedVertices(batch.Texture, batch.VertexBuffer, batch.VertexCount, batch.PrimitiveCount);
                drawn++;
            }
        }
        _terrainRegionsDrawn = drawn;
        EvictRegionCachesOutsideWindow(_terrainRegions, _terrainRegionDirty, rx1, rx2, ry1, ry2);
    }

    private unsafe CachedRenderBatch BuildRenderBatch(Texture2D texture, List<MapVertex> vertices)
    {
        var vertexBuffer = new VertexBuffer(_gfxDevice, typeof(MapVertex), vertices.Count, BufferUsage.WriteOnly);
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(vertices);
        fixed (MapVertex* p = span)
        {
            vertexBuffer.SetDataPointerEXT(0, (IntPtr)p,
                System.Runtime.CompilerServices.Unsafe.SizeOf<MapVertex>() * vertices.Count, SetDataOptions.None);
        }

        var tileCount = vertices.Count / 4;
        _mapRenderer.EnsureQuadIndexCapacity(tileCount);

        return new CachedRenderBatch
        {
            Texture = texture,
            VertexBuffer = vertexBuffer,
            VertexCount = vertices.Count,
            PrimitiveCount = tileCount * 2
        };
    }

    private int GetStaticCacheSignature()
    {
        var hash = new HashCode();
        hash.Add(ShowStatics);
        hash.Add(ShowNoDraw);
        hash.Add(FlatView);
        hash.Add(MinZ);
        hash.Add(MaxZ);
        hash.Add(ObjectIdFilterEnabled);
        hash.Add(ObjectIdFilterInclusive);
        if (ObjectIdFilterEnabled)
            foreach (var id in ObjectIdFilter)
                hash.Add(id);
        hash.Add(ObjectHueFilterEnabled);
        hash.Add(ObjectHueFilterInclusive);
        if (ObjectHueFilterEnabled)
            foreach (var hue in ObjectHueFilter)
                hash.Add(hue);
        return hash.ToHashCode();
    }

    private bool ShouldDrawCachedStatics(Camera camera)
    {
        return ShowStatics && !WalkableSurfaces && camera.Zoom <= LowZoomStaticThreshold;
    }

    private static bool IsStaticCacheable(StaticObject so)
    {
        return Math.Max(so.TextureBounds.Width, so.TextureBounds.Height) >= StaticCacheMinTextureSize;
    }

    private void BuildStaticRegion(int idx, int rx, int ry)
    {
        var batches = _staticRegions[idx];
        if (batches == null)
            _staticRegions[idx] = batches = new List<CachedRenderBatch>();
        else
        {
            DisposeRegionBatches(batches);
            batches.Clear();
        }
        _staticRegionDirty[idx] = false;
        if (!ShowStatics)
            return;

        var accum = new Dictionary<Texture2D, List<MapVertex>>();
        int bx0 = rx * RegionBlocks, by0 = ry * RegionBlocks;
        int bx1 = Math.Min(bx0 + RegionBlocks, Client.Width);
        int by1 = Math.Min(by0 + RegionBlocks, Client.Height);
        for (int bx = bx0; bx < bx1; bx++)
        {
            for (int by = by0; by < by1; by++)
            {
                if (!_materializedBlocks.Contains(PackBlock(bx, by)))
                    continue;
                int minX = bx * 8, minY = by * 8;
                for (int x = minX; x < minX + 8; x++)
                {
                    for (int y = minY; y < minY + 8; y++)
                    {
                        var statics = StaticsManager.GetRaw(x, y);
                        if (statics == null)
                            continue;
                        foreach (var so in statics)
                        {
                            if (so.IsAnimated || !IsStaticCacheable(so) || !so.CanDraw || !CanDrawStatic(so))
                                continue;
                            if (!accum.TryGetValue(so.Texture, out var list))
                                accum[so.Texture] = list = new List<MapVertex>();
                            list.AddRange(so.Vertices);
                        }
                    }
                }
            }
        }
        foreach (var (texture, vertices) in accum)
        {
            if (vertices.Count == 0)
                continue;
            var batch = BuildRenderBatch(texture, vertices);
            _cachedRegionVertexBytes += (long)batch.VertexCount * MapVertexSizeBytes;
            batches.Add(batch);
        }
    }

    private void DrawCachedStaticRegions(RectU16 viewRange)
    {
        if (_regionsX == 0 || _staticRegions.Length == 0)
            return;

        var signature = GetStaticCacheSignature();
        if (signature != _staticCacheSignature)
        {
            _staticCacheSignature = signature;
            MarkStaticCacheDirty();
        }

        int rx1 = Math.Clamp(viewRange.X1 / 8 / RegionBlocks - 1, 0, _regionsX - 1);
        int rx2 = Math.Clamp(viewRange.X2 / 8 / RegionBlocks + 1, 0, _regionsX - 1);
        int ry1 = Math.Clamp(viewRange.Y1 / 8 / RegionBlocks - 1, 0, _regionsY - 1);
        int ry2 = Math.Clamp(viewRange.Y2 / 8 / RegionBlocks + 1, 0, _regionsY - 1);

        var (cbrx, cbry) = CameraRegion();
        _visibleDirtyRegions.Clear();
        for (int rx = rx1; rx <= rx2; rx++)
            for (int ry = ry1; ry <= ry2; ry++)
                if (_staticRegionDirty[RegionIndex(rx, ry)])
                    _visibleDirtyRegions.Add(RegionIndex(rx, ry));
        if (_visibleDirtyRegions.Count > 0)
        {
            _visibleDirtyRegions.Sort((a, b) =>
                RegionDistance(a, cbrx, cbry).CompareTo(RegionDistance(b, cbrx, cbry)));
            var buildStart = System.Diagnostics.Stopwatch.GetTimestamp();
            int built = 0;
            foreach (var idx in _visibleDirtyRegions)
            {
                if (!_forceFullStaticCache && built > 0 &&
                    (built >= MaxRegionBuildsPerFrame ||
                     System.Diagnostics.Stopwatch.GetElapsedTime(buildStart).TotalMilliseconds >= RegionBuildBudgetMs))
                    break;
                BuildStaticRegion(idx, idx / _regionsY, idx % _regionsY);
                built++;
            }
            _staticRegionsBuilt = built;
        }
        else
        {
            _staticRegionsBuilt = 0;
        }

        _mapRenderer.FlushPending();
        int drawn = 0;
        for (int rx = rx1; rx <= rx2; rx++)
        {
            for (int ry = ry1; ry <= ry2; ry++)
            {
                var batches = _staticRegions[RegionIndex(rx, ry)];
                if (batches == null)
                    continue;
                foreach (var batch in batches)
                    _mapRenderer.DrawCachedVertices(batch.Texture, batch.VertexBuffer, batch.VertexCount, batch.PrimitiveCount);
                drawn++;
            }
        }
        _staticRegionsDrawn = drawn;
        EvictRegionCachesOutsideWindow(_staticRegions, _staticRegionDirty, rx1, rx2, ry1, ry2);
    }

    private void TrackSelectionInvalidation()
    {
        var stateSignature = GetSelectionStateSignature();
        var cameraMoved = !_selectionCameraInitialized ||
                          Camera.Position != _lastSelectionCameraPosition ||
                          Camera.Zoom != _lastSelectionZoom ||
                          Camera.Yaw != _lastSelectionYaw ||
                          Camera.Pitch != _lastSelectionPitch ||
                          Camera.Roll != _lastSelectionRoll ||
                          Camera.ScreenSize != _lastSelectionScreenSize;
        var stateChanged = stateSignature != _lastSelectionStateSignature;

        if (cameraMoved || stateChanged)
        {
            MarkSelectionBufferDirty();
            if (cameraMoved)
                _lastCameraMotionFrame = _frameCounter;
            _selectionCameraInitialized = true;
            _lastSelectionCameraPosition = Camera.Position;
            _lastSelectionZoom = Camera.Zoom;
            _lastSelectionYaw = Camera.Yaw;
            _lastSelectionPitch = Camera.Pitch;
            _lastSelectionRoll = Camera.Roll;
            _lastSelectionScreenSize = Camera.ScreenSize;
            _lastSelectionStateSignature = stateSignature;
        }
    }

    private int GetSelectionStateSignature()
    {
        var hash = new HashCode();
        hash.Add(ShowLand);
        hash.Add(ShowStatics);
        hash.Add(ShowNoDraw);
        hash.Add(UseVirtualLayer);
        hash.Add(VirtualLayerZ);
        hash.Add(WalkableSurfaces);
        hash.Add(FlatView);
        hash.Add(MinZ);
        hash.Add(MaxZ);
        hash.Add(ObjectIdFilterEnabled);
        hash.Add(ObjectIdFilterInclusive);
        if (ObjectIdFilterEnabled)
        {
            foreach (var id in ObjectIdFilter)
            {
                hash.Add(id);
            }
        }
        hash.Add(ObjectHueFilterEnabled);
        hash.Add(ObjectHueFilterInclusive);
        if (ObjectHueFilterEnabled)
        {
            foreach (var hue in ObjectHueFilter)
            {
                hash.Add(hue);
            }
        }
        return hash.ToHashCode();
    }

    private TileObject? PrevSelected;
    public TileObject? Selected { get; private set; }
    public TileObject? RealSelected { get; private set; }

    private bool SelectionActive => Client.Running;

    private bool SelectionWindowed => (long)ViewRange.Width * ViewRange.Height > SelectionFullViewMaxTiles;

    // At low zoom the full view is too big to render every frame, so we only render the selection
    // buffer for a bounded window around the cursor - enough to pick the tile under it.
    private RectU16 SelectionRange()
    {
        if (!SelectionWindowed)
            return ViewRange;
        var world = Unproject(_prevMouseState.X, _prevMouseState.Y, 0);
        int cx = Math.Clamp((int)world.X, ViewRange.X1, ViewRange.X2);
        int cy = Math.Clamp((int)world.Y, ViewRange.Y1, ViewRange.Y2);
        int x1 = Math.Max(ViewRange.X1, cx - SelectionWindowRadius);
        int y1 = Math.Max(ViewRange.Y1, cy - SelectionWindowRadius);
        int x2 = Math.Min(ViewRange.X2, cx + SelectionWindowRadius);
        int y2 = Math.Min(ViewRange.Y2, cy + SelectionWindowRadius);
        return new RectU16((ushort)x1, (ushort)y1, (ushort)x2, (ushort)y2);
    }

    private void UpdateMouseSelection(int x, int y)
    {
        if (!SelectionActive)
        {
            RealSelected = null;
        }
        else if (!_selectionBuffer.Bounds.Contains(x, y))
        {
            RealSelected = null;
        }
        else if (CEDGame.UIManager.IsOverUI(x, y))
        {
            RealSelected = null;
        }
        else
        {
            _selectionBuffer.GetData(0, new Microsoft.Xna.Framework.Rectangle(x, y, 1, 1), _selectionPixel, 0, 1);
            var pixel = _selectionPixel[0];
            var selectedIndex = pixel.R | (pixel.G << 8) | (pixel.B << 16);
            if (selectedIndex < 1)
                RealSelected = null;
            else
                RealSelected = LandTilesIdDictionary.TryGetValue(selectedIndex, out var lo) ? lo : StaticsManager.Get(selectedIndex);
        }
        
        Selected = RealSelected;
        
        if (UseVirtualLayer)
        {
            var z = FlatView ? 0 : VirtualLayerZ;
            var virtualLayerPos = Unproject(x, y, z);
            var newX = (ushort)Math.Clamp(virtualLayerPos.X + 1, 0, Client.Width * 8 - 1);
            var newY = (ushort)Math.Clamp(virtualLayerPos.Y + 1, 0, Client.Height * 8 - 1);
            if (newX != PrevSelected?.Tile.X || newY != PrevSelected.Tile.Y)
            {
                Selected = new VirtualLayerTile(newX, newY, (sbyte)z);
            }
            else
                Selected = PrevSelected;
        }
    }

    private RectU16 CalculateViewRange(Camera camera)
    {
        float zoom = camera.Zoom;
        int screenWidth = camera.ScreenSize.Width;
        int screenHeight = camera.ScreenSize.Height;
        
        // Calculate the size of the drawing diamond in pixels
        // Default is 2.0f, but 2.6f gives smaller viewrange without any visible problems 
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom / 2.6f;
        
        Vector3 center = camera.Position;
        
        // Render a few extra rows at the top to deal with things at lower z
        var minTileX = Client.ClampX((int)((center.X - screenDiamondDiagonal) / TILE_SIZE - 8));
        var minTileY = Client.ClampY((int)((center.Y - screenDiamondDiagonal) / TILE_SIZE - 8));
        
        // Render a few extra rows at the bottom to deal with things at higher z
        var maxTileX = Client.ClampX((int)((center.X + screenDiamondDiagonal) / TILE_SIZE + 8));
        var maxTileY = Client.ClampY((int)((center.Y + screenDiamondDiagonal) / TILE_SIZE + 8));
        
        return new RectU16(minTileX, minTileY, maxTileX, maxTileY);
    }

    public Vector3 Unproject(int x, int y, int z)
    {
        var worldPoint = _gfxDevice.Viewport.Unproject
        (
            new FNAVector3(x, y, -(z / 384f) + 0.5f),
            Camera.FnaWorldViewProj,
            Matrix.Identity,
            Matrix.Identity
        );
        return new Vector3
        (
            worldPoint.X / TILE_SIZE,
            worldPoint.Y / TILE_SIZE,
            worldPoint.Z / TILE_Z_SCALE
        );
    }

    private bool CanDrawLand(LandObject lo)
    {
        if(!ShowLand || (lo.Tile.Id <= 2 && !ShowNoDraw))
            return false;
        return WithinZRange(lo.Tile.Z);
    }

    public bool CanDrawStatic(StaticObject so, bool includeBuried = false)
    {
        var tile = so.StaticTile;
        var id = tile.Id;
        if (id >= UoFileManager.TileData.StaticData.Length)
            return false;

        ref StaticTiles data = ref UoFileManager.TileData.StaticData[id];

        // Outlands specific
        // if ((data.Flags & TileFlag.NoDraw) != 0)
        //     return false;

        if (!ShowNoDraw)
        {
            switch (id)
            {
                case 0x0001:
                case 0x21BC:
                case 0x63D3: return false;

                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D: return (data.Flags & TileFlag.Background) == 0 
                            // && (data.Flags & TileFlag.NoDraw) == 0 // Outlands specific
                            && (data.Flags & TileFlag.Surface) == 0;

                case 0x2198:
                case 0x2199:
                case 0x21A0:
                case 0x21A1:
                case 0x21A2:
                case 0x21A3:
                case 0x21A4: return false;
            }
        }
        if (!Client.IsValidX(tile.X) || !Client.IsValidY(tile.Y))
        {
            return false;
        }
        if (!ShowStatics)
            return false;
        
        if (!WithinZRange(tile.Z))
            return false;
        // Statics buried under raised terrain aren't drawn, but are still selectable for editing
        // (so an area edit can elevate them back out from under the land).
        var landTile = LandTiles[tile.X, tile.Y];
        if (!includeBuried && !FlatView && landTile != null && CanDrawLand(landTile) &&
            WithinZRange(landTile.Tile.Z) && landTile.AverageZ() >= tile.PriorityZ + 5)
            return false;

        var show = so.Visible;
        
        if(show && ObjectIdFilterEnabled)
        {
            show &= !(ObjectIdFilterInclusive ^ ObjectIdFilter.Contains(id));
        }
        if(show && ObjectHueFilterEnabled)
        {
            show &= !(ObjectHueFilterInclusive ^ ObjectHueFilter.Contains(tile.Hue));
        }
        return show;
    }

    private static Vector4 NonWalkableHue = HuesManager.Instance.GetRGBVector(Color.FromArgb(50, 0, 0));
    private static Vector4 WalkableHue = HuesManager.Instance.GetRGBVector(Color.FromArgb(0, 50, 0));
    public Vector4 GhostLandTilesHue = Vector4.Zero;
    
    public bool IsWalkable(LandObject lo)
    {
        //TODO: Save value for later to avoid calculation, recalculate whole cell on operations
        if (lo.Walkable.HasValue)
            return lo.Walkable.Value;
        
        var landTile = lo.LandTile;
        bool walkable = !UoFileManager.TileData.LandData[landTile.Id].IsImpassable;
        if (!walkable)
        {
            return false;
        }
        var staticObjects = StaticsManager.Get(landTile.X, landTile.Y);
        if (staticObjects != null)
        {
            foreach (var so in staticObjects)
            {
                var staticTile = so.StaticTile;
                var staticTileData = UoFileManager.TileData.StaticData[staticTile.Id];
                var ok = staticTile.Z + staticTileData.Height <= landTile.Z || landTile.Z + 16 <= staticTile.Z;
                if (!ok && !staticTileData.IsSurface && staticTileData.IsImpassable)
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    public bool IsWalkable(StaticObject so)
    {
        if (so.Walkable.HasValue)
            return so.Walkable.Value;
        
        var tile = so.StaticTile;
        var thisTileData = UoFileManager.TileData.StaticData[tile.Id];
        var thisCalculatedHeight = thisTileData.IsBridge ? thisTileData.Height / 2 : thisTileData.Height;
        bool walkable = !thisTileData.IsImpassable;
        if (walkable)
        {
            var staticObjects = StaticsManager.Get(tile.X, tile.Y);
            if (staticObjects != null)
            {
                foreach (var so2 in staticObjects)
                {
                    var staticTile = so2.StaticTile;
                    var staticTileData = UoFileManager.TileData.StaticData[staticTile.Id];
                    var ok = staticTile.Z + staticTileData.Height <= tile.Z + thisCalculatedHeight || tile.Z + 16 <= staticTile.Z;
                    if (!ok && !staticTileData.IsSurface && staticTileData.IsImpassable)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private bool WithinZRange(short z)
    {
        return z >= MinZ && z <= MaxZ;
    }

    private bool ShouldClipDetailedObjects(Camera camera)
    {
        return camera.Zoom <= 0.5f;
    }

    private bool ShouldDrawCachedTerrain(Camera camera, string technique)
    {
        return camera.Zoom <= LowZoomTerrainThreshold && technique == "Terrain" && !WalkableSurfaces;
    }

    private bool IsInClipSpace(MapObject mapObject, Camera camera)
    {
        const float margin = 0.15f;
        var allLeft = true;
        var allRight = true;
        var allAbove = true;
        var allBelow = true;
        foreach (var vertex in mapObject.Vertices)
        {
            var clip = Vector4.Transform(new Vector4(vertex.Position, 1f), camera.WorldViewProj);
            var w = Math.Abs(clip.W);
            if (w <= float.Epsilon)
            {
                w = 1f;
            }

            allLeft &= clip.X < -w - margin;
            allRight &= clip.X > w + margin;
            allAbove &= clip.Y < -w - margin;
            allBelow &= clip.Y > w + margin;

            if (!allLeft && !allRight && !allAbove && !allBelow)
            {
                return true;
            }
        }

        _detailedObjectsCulled++;
        return false;
    }

    private bool DrawStatic(StaticObject so, Vector4 hueOverride = default)
    {
        if (!CanDrawStatic(so))
            return false;
        
        _mapRenderer.DrawMapObject(so, hueOverride);
        return true;
    }
    
    private void DrawLand(LandObject lo, Vector4 hueOverride = default)
    {
        var landTile = lo.LandTile;
        if (landTile.Id > UoFileManager.TileData.LandData.Length)
            return;
        if (!CanDrawLand(lo))
            return;

        _mapRenderer.DrawMapObject(lo, hueOverride);
    }

    public void Draw()
    {
        _mapRenderer.ResetFrameStats();
        Metrics.Start("DrawMap");
        if (!Client.Running || CEDGame.Closing)
        {
            DrawBackground();
            return;
        }
        _detailedObjectsCulled = 0;
        Metrics.SetCounter("ViewRangeTiles", ViewRange.Width * ViewRange.Height);

        Metrics.Measure("DrawSelection", DrawSelectionBufferIfNeeded);
        Metrics.Start("GetMouseSelection");
        UpdateMouseSelection(_prevMouseState.X, _prevMouseState.Y);
        Metrics.Stop("GetMouseSelection");
        if (DebugDrawSelectionBuffer)
            return;
        
        Metrics.Measure("DrawLights", () => DrawLights(Camera));
        if (DebugDrawLightMap)
            return;
        
        _mapRenderer.SetRenderTarget(null);
        Metrics.Measure("DrawImageOverlayBelow", () => DrawImageOverlay(false));
        Metrics.Measure("DrawLand", () => DrawLand(Camera, ViewRange));
        Metrics.Start("DrawLandGrid");
        if (ShowGrid)
        {
            DrawLand(Camera, ViewRange, "TerrainGrid");
        }
        Metrics.Stop("DrawLandGrid");
        Metrics.Measure("DrawLandHeight", DrawLandHeight);
        Metrics.Measure("DrawStatics", () => DrawStatics(Camera, ViewRange));
        Metrics.Measure("DrawImageOverlayAbove", () => DrawImageOverlay(true));
        Metrics.Measure("ApplyLights", ApplyLights);
        Metrics.Measure("DrawVirtualLayer", DrawVirtualLayer);
        RecordRendererStats();
        Metrics.Stop("DrawMap");    
    }

    public void AfterDraw()
    {
        if (Export)
        {
            ExportImage();
            Export = false;
        }
    }

    private void RecordRendererStats()
    {
        var stats = _mapRenderer.Stats;
        Metrics.SetCounter("RendererDrawCalls", stats.DrawCalls);
        Metrics.SetCounter("RendererCachedDrawCalls", stats.CachedDrawCalls);
        Metrics.SetCounter("RendererFlushes", stats.Flushes);
        Metrics.SetCounter("RendererTextureEvictions", stats.TextureEvictions);
        Metrics.SetCounter("RendererVertexUploads", stats.VertexUploads);
        Metrics.SetCounter("RendererVerticesUploaded", stats.VerticesUploaded);
        Metrics.SetCounter("DetailedObjectsCulled", _detailedObjectsCulled);
        Metrics.SetCounter("PendingBlockRequests", Client.PendingBlockRequests);
        Metrics.SetCounter("ForegroundPendingBlockRequests", Client.ForegroundPendingBlockRequests);
        Metrics.SetCounter("QueuedBlockRequests", Client.QueuedBlockRequests);
        Metrics.SetCounter("ForegroundQueuedBlockRequests", Client.ForegroundQueuedBlockRequests);
        Metrics.SetCounter("BackgroundQueuedBlockRequests", Client.BackgroundQueuedBlockRequests);
        Metrics.SetCounter("LoadedBlockCount", Client.LoadedBlockCount);
        Metrics.SetCounter("BlockCacheCapacity", Client.BlockCacheCapacity);
        Metrics.SetCounter("BackgroundPreloadActive", Client.BackgroundPreloadActive ? 1 : 0);
        Metrics.SetCounter("BackgroundPreloadRemaining", Client.BackgroundPreloadRemaining);
        Metrics.SetCounter("MaterializedBlocks", _materializedBlocks.Count);
        Metrics.SetCounter("MaterializationComplete", _materializationComplete ? 1 : 0);
        Metrics.SetCounter("MaterializeCap", (long)_adaptiveMaterializeCap);
        Metrics.SetCounter("CamTileX", (long)(Camera.Position.X / TILE_SIZE));
        Metrics.SetCounter("CamTileY", (long)(Camera.Position.Y / TILE_SIZE));
        Metrics.SetCounter("Zoomx1000", (long)(Camera.Zoom * 1000));
        Metrics.SetCounter("BgCenterBlockX", _bgCenterX);
        Metrics.SetCounter("BgCenterBlockY", _bgCenterY);
        Metrics.SetCounter("MapBlocksW", Client.Width);
        Metrics.SetCounter("MapBlocksH", Client.Height);
        Metrics.SetCounter("ManagedHeapMB", GC.GetTotalMemory(false) / (1024 * 1024));
        Metrics.SetCounter("ProcessWorkingSetMB", Environment.WorkingSet / (1024 * 1024));
        Metrics.SetCounter("AvailableMemoryMB", _totalAvailableMemoryBytes / (1024 * 1024));
        Metrics.SetCounter("RegionCacheMB", _cachedRegionVertexBytes / (1024 * 1024));
        Metrics.SetCounter("RegionCacheEviction", _regionCacheEvictionEnabled ? 1 : 0);
        Metrics.SetCounter("MaterializeBudgetBlocks", _materializeBudgetBlocks == long.MaxValue ? -1 : _materializeBudgetBlocks);
        Metrics.SetCounter("MaterializeEviction", _materializeEvictionEnabled ? 1 : 0);
        Metrics.SetCounter("ZoomFloorx1000", (long)(ComputeMinZoom() * 1000));
    }

    private void DrawBackground()
    {
        _mapRenderer.SetRenderTarget(null);
        _gfxDevice.Clear(FNAColor.Black);
        _gfxDevice.BlendState = BlendState.AlphaBlend;
        _spriteBatch.Begin();
        var windowRect = _gameWindow.ClientBounds;
        var backgroundRect = new FNARectangle
        (
            windowRect.Width / 2 - _background.Width / 2,
            windowRect.Height / 2 - _background.Height / 2,
            _background.Width,
            _background.Height
        );
        _spriteBatch.Draw(_background, backgroundRect, FNAColor.White);
        _spriteBatch.End();
    }

    private void DrawSelectionBufferIfNeeded()
    {
        if (DebugDrawSelectionBuffer)
        {
            DrawSelectionBuffer();
            Metrics.SetCounter("SelectionBufferRedrawn", 1);
            return;
        }

        if (!SelectionActive)
        {
            Metrics.SetCounter("SelectionBufferRedrawn", 0);
            return;
        }

        if (SelectionWindowed)
        {
            var mouseMoved = _prevMouseState.X != _lastSelectionMouseX || _prevMouseState.Y != _lastSelectionMouseY;
            if (!mouseMoved && !_selectionBufferDirty)
            {
                Metrics.SetCounter("SelectionBufferRedrawn", 0);
                return;
            }
            DrawSelectionBuffer();
            _lastSelectionMouseX = _prevMouseState.X;
            _lastSelectionMouseY = _prevMouseState.Y;
            _selectionBufferDirty = false;
            Metrics.SetCounter("SelectionBufferRedrawn", 1);
            return;
        }

        if (!_selectionBufferDirty || _lastCameraMotionFrame == _frameCounter)
        {
            Metrics.SetCounter("SelectionBufferRedrawn", 0);
            return;
        }

        DrawSelectionBuffer();
        _selectionBufferDirty = false;
        Metrics.SetCounter("SelectionBufferRedrawn", 1);
    }

    private void DrawSelectionBuffer()
    {
        MapEffect.WorldViewProj = Camera.FnaWorldViewProj;
        MapEffect.CurrentTechnique = MapEffect.Techniques["Selection"];
        _mapRenderer.SetRenderTarget(DebugDrawSelectionBuffer ? null : _selectionBuffer);
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        var range = SelectionRange();
        var clipDetailedObjects = ShouldClipDetailedObjects(Camera);
        for (int x = range.X1; x <= range.X2; x++)
        {
            for (int y = range.Y1; y <= range.Y2; y++)
            {
                var landTile = LandTiles[x, y];
                if (landTile != null)
                {
                    DrawLand(landTile, landTile.ObjectIdColor);
                }
            }
        }

        for (int x = range.X1; x <= range.X2; x++)
        {
            for (int y = range.Y1; y <= range.Y2; y++)
            {
                var tiles = StaticsManager.GetRaw(x, y);
                if (tiles == null) continue;
                foreach (var tile in tiles)
                {
                    if (tile.CanDraw && (!clipDetailedObjects || IsInClipSpace(tile, Camera)))
                    {
                        DrawStatic(tile, tile.ObjectIdColor);
                    }
                }
            }
        }
        _mapRenderer.End();
    }
    
    private void DrawLights(Camera camera)
    {
        if (LightsManager.Instance.MaxGlobalLight && !LightsManager.Instance.AltLights && !DebugDrawLightMap) 
        {
            return; //Little performance boost
        }
        MapEffect.WorldViewProj = camera.FnaWorldViewProj;
        MapEffect.CurrentTechnique = MapEffect.Techniques["Statics"];
        _mapRenderer.SetRenderTarget(DebugDrawLightMap ? null : _lightMap);
        _gfxDevice.Clear(ClearOptions.Target, LightsManager.Instance.GlobalLightLevelColor, 0f, 0);
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone, 
            SamplerState.PointClamp,
            DepthStencilState.None, 
            BlendState.Additive
        );
        var clipDetailedObjects = ShouldClipDetailedObjects(camera);
        foreach (var kvp in StaticsManager.LightTiles)
        {
            var staticTile = kvp.Key;
            var light = kvp.Value;
            if (light.CanDraw)
            {
                if (CanDrawStatic(staticTile))
                {
                    if (!clipDetailedObjects || IsInClipSpace(light, camera))
                    {
                        _mapRenderer.DrawMapObject(light, default);
                    }
                }
            }
        }
        _mapRenderer.End();
    }

    private void DrawLand(Camera camera, RectU16 viewRange, string technique = "Terrain")
    {
        if (!ShowLand)
        {
            return;
        }
        MapEffect.WorldViewProj = camera.FnaWorldViewProj;
        MapEffect.CurrentTechnique = MapEffect.Techniques[technique];
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        if (ShouldDrawCachedTerrain(camera, technique))
        {
            DrawCachedTerrainRegions(viewRange);
            Metrics.SetCounter("TerrainRegionsDrawn", _terrainRegionsDrawn);
            Metrics.SetCounter("TerrainRegionsBuilt", _terrainRegionsBuilt);

            foreach (var tile in GhostLandTiles.Values)
            {
                DrawLand(tile, GhostLandTilesHue);
            }
            _mapRenderer.End();
            return;
        }

        for (int x = viewRange.X1; x <= viewRange.X2; x++)
        {
            for (int y = viewRange.Y1; y <= viewRange.Y2; y++)
            {
                var tile = LandTiles[x, y];
                if (tile != null && tile.CanDraw)
                {
                    var hueOverride = Vector4.Zero;
                    if (WalkableSurfaces && !UoFileManager.TileData.LandData[tile.LandTile.Id].IsWet)
                    {
                        hueOverride = IsWalkable(tile) ? WalkableHue : NonWalkableHue;
                    }
                    DrawLand(tile, hueOverride);
                }
            }
        }

        foreach (var tile in GhostLandTiles.Values)
        {
            DrawLand(tile, GhostLandTilesHue);
        }
        _mapRenderer.End();
    }

    private void DrawLandHeight()
    {
        if (!FlatView || !FlatShowHeight)
        {
            return;
        }
        var font = _fontSystem.GetFont(18 * Camera.Zoom);
        var halfTile = TILE_SIZE * 0.5f * Camera.Zoom;
        _spriteBatch.Begin();
        for (int x = ViewRange.X1; x <= ViewRange.X2; x++)
        {
            for (int y = ViewRange.Y1; y <= ViewRange.Y2; y++)
            {
                var tile = LandTiles[x, y];
                if (tile != null && tile.CanDraw)
                {
                    DrawTileHeight(tile, font, halfTile);
                }
            }
        }
        foreach (var tile in GhostLandTiles.Values)
        {
            DrawTileHeight(tile, font, halfTile);
        }
        _spriteBatch.End();
    }

    private void DrawTileHeight(LandObject tile, DynamicSpriteFont font, float yOffset)
    {
        var text = tile.LandTile.Z.ToString();
        var halfTextSize = font.MeasureString(text) / 2;
        var tilePos = tile.Vertices[0].Position;
        var projected = _gfxDevice.Viewport.Project
            (new FNAVector3(tilePos.X, tilePos.Y, tilePos.Z), Camera.FnaWorldViewProj, Matrix.Identity, Matrix.Identity);
        var pos = new Vector2
            (projected.X - halfTextSize.X, projected.Y + yOffset);
        var windowRect = _gameWindow.ClientBounds;
        if (pos.X > 0 && pos.X < windowRect.Width && pos.Y > 0 &&
            pos.Y < windowRect.Height)
        {
            _spriteBatch.DrawString(font, text, new FNAVector2(pos.X, pos.Y), FNAColor.White);
        }
    }

    private void DrawStatics(Camera camera, RectU16 viewRange)
    {
        if (!ShowStatics)
        {
            return;
        }
        MapEffect.WorldViewProj = camera.FnaWorldViewProj;
        MapEffect.CurrentTechnique = MapEffect.Techniques["Statics"];
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        if (ShouldDrawCachedStatics(camera))
        {
            DrawCachedStaticRegions(viewRange);
            Metrics.SetCounter("StaticRegionsDrawn", _staticRegionsDrawn);
            Metrics.SetCounter("StaticRegionsBuilt", _staticRegionsBuilt);

            foreach (var tile in StaticsManager.AnimatedTiles)
            {
                if (viewRange.Contains(tile.Tile.X, tile.Tile.Y))
                    DrawStatic(tile);
            }

            foreach (var tile in StaticsManager.GhostTiles)
            {
                DrawStatic(tile);
            }
            _mapRenderer.End();
            return;
        }

        var clipDetailedObjects = ShouldClipDetailedObjects(camera);
        for (int x = viewRange.X1; x <= viewRange.X2; x++)
        {
            for (int y = viewRange.Y1; y <= viewRange.Y2; y++)
            {
                var tiles = StaticsManager.GetRaw(x, y);
                if(tiles == null) continue;
                foreach (var tile in tiles)
                {
                    if (tile.CanDraw && (!clipDetailedObjects || IsInClipSpace(tile, camera)))
                    {
                        var hueOverride = Vector4.Zero;
                        if (WalkableSurfaces && UoFileManager.TileData.StaticData[tile.Tile.Id].IsSurface)
                        {
                            hueOverride = IsWalkable(tile) ? WalkableHue : NonWalkableHue;
                        }
                        DrawStatic(tile, hueOverride);
                    }
                }
            }
        }
        foreach (var tile in StaticsManager.GhostTiles)
        {
            DrawStatic(tile);
        }
        _mapRenderer.End();
    }

    public void ApplyLights()
    {
        if (LightsManager.Instance.MaxGlobalLight && !LightsManager.Instance.AltLights) 
        {
            return; //Skip lighting
        }
        _spriteBatch.Begin(SpriteSortMode.Deferred, LightsManager.Instance.ApplyBlendState);
        _spriteBatch.Draw(_lightMap, FNAVector2.Zero, LightsManager.Instance.ApplyBlendColor);
        _spriteBatch.End();
    }

    public void DrawVirtualLayer()
    {
        if (!ShowVirtualLayer)
        {
            return;
        }
        MapEffect.CurrentTechnique = MapEffect.Techniques["VirtualLayer"];
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        VirtualLayer.Z = (sbyte)VirtualLayerZ;
        _mapRenderer.DrawMapObject(VirtualLayer, Vector4.Zero);
        _mapRenderer.End();
    }

    public void DrawImageOverlay(bool aboveTerrain)
    {
        if (!ImageOverlay.Enabled || ImageOverlay.Texture == null)
        {
            return;
        }
        if (ImageOverlay.DrawAboveTerrain != aboveTerrain)
        {
            return;
        }
        MapEffect.WorldViewProj = Camera.FnaWorldViewProj;
        MapEffect.CurrentTechnique = MapEffect.Techniques["ImageOverlay"];
        _mapRenderer.Begin
        (
            MapEffect,
            RasterizerState.CullNone,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            BlendState.AlphaBlend
        );
        _mapRenderer.DrawMapObject(ImageOverlay, Vector4.Zero);
        _mapRenderer.End();
    }

    public bool Export = false;
    public string ExportPath = "render.png";
    public int ExportWidth = 1920;
    public int ExportHeight = 1080;
    public float ExportZoom = 1.0f;
    
    public void ExportImage()
    {
        var pp = _gfxDevice.PresentationParameters;
        if (ExportWidth != pp.BackBufferWidth || ExportHeight != pp.BackBufferHeight)
        {
            pp.BackBufferWidth = ExportWidth;
            pp.BackBufferHeight = ExportHeight;
            pp.DeviceWindowHandle = CEDGame.Window.Handle;
            _gfxDevice.Reset(pp);
        }
        var myRenderTarget = new RenderTarget2D(_gfxDevice, ExportWidth, ExportHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        var newLightMap = new RenderTarget2D
        (
            _gfxDevice,
            ExportWidth,
            ExportHeight,
            _lightMap.LevelCount >  1,
            _lightMap.Format,
            _lightMap.DepthStencilFormat
        );
        _lightMap.Dispose();
        _lightMap = newLightMap;
        
        var myCamera = new Camera();
        myCamera.Position = Camera.Position;
        myCamera.Zoom = ExportZoom;
        var rbounds = myRenderTarget.Bounds;
        myCamera.ScreenSize = new Rectangle(rbounds.X, rbounds.Y, rbounds.Width, rbounds.Height);
        myCamera.Update();
        
        var cameraBounds = CalculateViewRange(myCamera);
        Client.RequestBlocks(cameraBounds);
        while(Client.WaitingForBlocks)
            Client.Update();

        EnsureRegionMaterialized(cameraBounds);

        foreach (var landObject in _ToRecalculate)
        {
            landObject.Update();
        }
        _ToRecalculate.Clear();

        MapEffect.WorldViewProj = myCamera.FnaWorldViewProj;
        DrawLights(myCamera);
        _mapRenderer.SetRenderTarget(myRenderTarget, new FNARectangle(0,0, ExportWidth, ExportHeight));
        _forceFullTerrainCache = myCamera.Zoom <= LowZoomTerrainThreshold;
        DrawLand(myCamera, cameraBounds);
        _forceFullTerrainCache = false;
        _forceFullStaticCache = ShouldDrawCachedStatics(myCamera);
        DrawStatics(myCamera, cameraBounds);
        _forceFullStaticCache = false;
        ApplyLights();
        using var fs = new FileStream(ExportPath, FileMode.OpenOrCreate);
        if(ExportPath.EndsWith(".png"))
            myRenderTarget.SaveAsPng(fs, myRenderTarget.Width, myRenderTarget.Height);
        else
        {
            if (!ExportPath.EndsWith(".jpg"))
            {
                Console.WriteLine("[EXPORT], invalid file format, exporting as JPEG");
            }
            myRenderTarget.SaveAsJpeg(fs, myRenderTarget.Width, myRenderTarget.Height);
        }
        _mapRenderer.SetRenderTarget(null);
        myRenderTarget.Dispose();
        OnWindowsResized(_gameWindow);
    }

    public void OnWindowsResized(GameWindow window)
    {
        var windowSize = window.ClientBounds;
        Camera.ScreenSize = new Rectangle(windowSize.X, windowSize.Y, windowSize.Width, windowSize.Height);
        Camera.Update();

        _selectionBuffer?.Dispose();
        _selectionBuffer = new RenderTarget2D
        (
            _gfxDevice,
            windowSize.Width,
            windowSize.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24
        );
        _lightMap?.Dispose();
        _lightMap = new RenderTarget2D
        (
            _gfxDevice,
            windowSize.Width,
            windowSize.Height,
            
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );
        MarkSelectionBufferDirty();
    }
}
