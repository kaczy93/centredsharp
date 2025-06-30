using System.Diagnostics.CodeAnalysis;
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
using ClassicUO.Utility.Logging;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;
using static CentrED.Constants;

namespace CentrED.Map;

public class MapManager
{
    private readonly GraphicsDevice _gfxDevice;
    private readonly GameWindow _GameWindow;

    private MapEffect _mapEffect;
    public MapEffect MapEffect => _mapEffect;
    private readonly MapRenderer _mapRenderer;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _background;
    private readonly FontSystem _fontSystem;

    private RenderTarget2D _selectionBuffer;
    public bool DebugDrawSelectionBuffer;
    private RenderTarget2D _lightMap;
    public bool DebugDrawLightMap;

    private AnimatedStaticsManager _animatedStaticsManager;
    public Art Arts;
    public Texmap Texmaps;

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
    public bool UseRandomTileSet = false;
    public bool UseSequentialTileSet = false;
    internal int _currentSequenceIndex = 0;
    public bool WalkableSurfaces = false;
    public bool FlatView = false;
    public bool FlatShowHeight = false;
    public bool FlatStatics = false;
    public bool AnimatedStatics = true;
    public bool ShowGrid = false;
    public bool DebugLogging;

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

    public bool StaticFilterEnabled;
    public bool StaticFilterInclusive = true;
    public SortedSet<int> StaticFilterIds = new();
    public HashSet<LandObject> _ToRecalculate = new();

    public int[] ValidLandIds { get; private set; }
    public int[] ValidStaticIds { get; private set; }

    public void ResetSequence()
    {
        _currentSequenceIndex = 0;
    }

    public MapManager(GraphicsDevice gd, GameWindow window)
    {
        _gfxDevice = gd;
        _GameWindow = window;
        _mapEffect = new MapEffect(gd);
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
        Client.StaticTileAdded += AddStatic;
        Client.StaticTileRemoved += RemoveStatic;
        Client.StaticTileMoved += MoveStatic;
        Client.StaticTileElevated += ElevateStatic;
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
        Tools.Add(new RemoveTool());
        Tools.Add(new HueTool());
        Tools.Add(new LandBrushTool());
        Tools.Add(new MeshEditTool());
        Tools.Add(new AltitudeGradientTool());

        _activeTool = Tools[0];
        OnWindowsResized(window);
    }
    
    private void OnConnected()
    {
        LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
        StaticTiles = new List<StaticObject>[Client.Width * 8, Client.Height * 8];
        VirtualLayer.Width = (ushort)(Client.Width * 8);
        VirtualLayer.Height = (ushort)(Client.Height * 8);
    }

    private void OnDisconnected()
    {
        LandTiles = new LandObject[0, 0];
        StaticTiles = new List<StaticObject>[0, 0];
        AllTiles.Clear();
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
        ClearBlock(block);
        foreach (var landTile in block.LandBlock.Tiles)
        {
            AddTile(landTile);
        }
        foreach (var staticTile in block.StaticBlock.AllTiles())
        {
            AddStatic(staticTile);
        }
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

        UpdateLights();
    }

    private void OnBlockUnloaded(Block block)
    {
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
    }

    private void OnLandTileReplaced(LandTile tile, ushort newId, sbyte newZ)
    {
        var landTile = LandTiles[tile.X, tile.Y];
        if (landTile != null)
        {
            _ToRecalculate.Add(landTile);
        }
    }

    public void OnLandTileElevated(LandTile tile, sbyte newZ)
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
    }

    private void AddStatic(StaticTile staticTile)
    {
        var so = new StaticObject(staticTile);
        var x = staticTile.X;
        var y = staticTile.Y;
        var list = StaticTiles[x,y];
        if (list == null)
        {
            list = new();
            StaticTiles[x, y] = list;
        }
        list.Add(so);
        list.Sort();
        AllTiles.Add(so.ObjectId, so);
        StaticTilesCount++;
        if (so.IsAnimated)
        {
            AnimatedStaticTiles.Add(so);
        }
        if (so.IsLight)
        {
            LightTiles.Add(so, new LightObject(so));
        }
    }
    
    private void RemoveStatic(StaticTile staticTile)
    {
        var x = staticTile.X;
        var y = staticTile.Y;
        var list = StaticTiles[x, y];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            list.Sort();
            AllTiles.Remove(found.ObjectId);
            if (found.IsAnimated)
            {
                AnimatedStaticTiles.Remove(found);
            }
            if (found.IsLight)
            {
                LightTiles.Remove(found);
            }
        }
        StaticTilesCount--;
    }
    
    private void MoveStatic(StaticTile staticTile, ushort newX, ushort newY)
    {
        var x = staticTile.X;
        var y = staticTile.Y;
        var list = StaticTiles[x, y];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            found.UpdatePos(newX, newY, staticTile.Z);
            var newList = StaticTiles[newX, newY];
            if (newList == null)
            {
                newList = new();
                StaticTiles[newX, newY] = newList;
            }
            newList.Add(found);
            newList.Sort();
        }
    }

    private void ElevateStatic(StaticTile tile, sbyte newZ)
    {
        GetTile(tile)?.UpdatePos(tile.X, tile.Y, newZ);
        StaticTiles?[tile.X, tile.Y]?.Sort();
    }
    
    private void HueStatic(StaticTile tile, ushort newHue)
    {
        GetTile(tile)?.UpdateHue(newHue);
    }
    
    private void AfterStaticChanged(StaticTile tile)
    {
        foreach (var staticObject in StaticTiles[tile.X, tile.Y])
        {
            staticObject.UpdateDepthOffset();
        }
    }

    private void AddTile(LandTile landTile)
    {
        var lo = new LandObject(landTile);
        LandTiles[landTile.X, landTile.Y] = lo;
        AllTiles.Add(lo.ObjectId, lo);
        LandTilesCount++;
    }

    public void ReloadShader()
    {
        if(File.Exists("MapEffect.fxc")) 
            _mapEffect = new MapEffect(_gfxDevice, File.ReadAllBytes("MapEffect.fxc"));
    }

    public void Load(string clientPath, string clientVersion)
    {
        var valid = ClientVersionHelper.IsClientVersionValid(clientVersion, out UOFileManager.Version);
        UOFileManager.BasePath = clientPath;
        UOFileManager.IsUOPInstallation = UOFileManager.Version >= ClientVersion.CV_7000 && File.Exists
            (UOFileManager.GetUOFilePath("MainMisc.uop"));

        if (!Task.WhenAll
            (
                new List<Task>
                {
                    ArtLoader.Instance.Load(),
                    HuesLoader.Instance.Load(),
                    TileDataLoader.Instance.Load(),
                    TexmapsLoader.Instance.Load(),
                    AnimDataLoader.Instance.Load(),
                    LightsLoader.Instance.Load()
                }
            ).Wait(TimeSpan.FromSeconds(10.0)))
            Log.Panic("Loading files timeout.");

        _animatedStaticsManager = new AnimatedStaticsManager();
        _animatedStaticsManager.Initialize();
        Arts = new Art(_gfxDevice);
        Texmaps = new Texmap(_gfxDevice);
        HuesManager.Load(_gfxDevice);
        LightsManager.Load(_gfxDevice);

        var tdl = TileDataLoader.Instance;
        var landIds = new List<int>();
        for (int i = 0; i < tdl.LandData.Length; i++)
        {
            if (!ArtLoader.Instance.GetValidRefEntry(i).Equals(UOFileIndex.Invalid))
            {
                landIds.Add(i);
            }
        }
        ValidLandIds = landIds.ToArray();
        var staticIds = new List<int>();
        for (int i = 0; i < tdl.StaticData.Length; i++)
        {
            if (!ArtLoader.Instance.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals
                    (UOFileIndex.Invalid))
            {
                staticIds.Add(i);
            }
        }
        ValidStaticIds = staticIds.ToArray();
        var landTileData = tdl.LandData.Select(ltd => new TileDataLand((ulong)ltd.Flags, ltd.TexID, ltd.Name)).ToArray();
        var staticTileData = tdl.StaticData.Select(std => new TileDataStatic((ulong)std.Flags, std.Weight, std.Layer, std.Count, std.AnimID, std.Hue, std.LightIndex, std.Height, std.Name)).ToArray(); 
        Client.InitTileData(landTileData, staticTileData);
    }

    public Vector2 Position
    {
        get => new(Camera.Position.X, Camera.Position.Y);
        set
        {
            Camera.Position.X = value.X;
            Camera.Position.Y = value.Y;
            Client.InternalSetPos((ushort)(value.X / TILE_SIZE), (ushort)(value.Y / TILE_SIZE));
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

    public Dictionary<int, TileObject> AllTiles = new();
    public LandObject?[,] LandTiles;
    public int LandTilesCount;
    public Dictionary<LandObject, LandObject> GhostLandTiles = new();
    public List<StaticObject>?[,] StaticTiles;
    public int StaticTilesCount;
    public List<StaticObject> AnimatedStaticTiles = new();
    public Dictionary<StaticObject, LightObject> LightTiles = new();
    public Dictionary<TileObject, StaticObject> GhostStaticTiles = new();
    public VirtualLayerObject VirtualLayer = VirtualLayerObject.Instance; //Used for drawing

    public void UpdateAllTiles()
    {
        foreach (var tile in AllTiles.Values)
        {
            if (tile is LandObject lo)
            {
                lo.Update();
            }
            else if (tile is StaticObject so)
            {
                so.Update();
            }
        }
    }

    public bool TryGetLandTile(int x, int y, [MaybeNullWhen(false)] out LandTile result)
    {
        result = null;
        if (!Client.IsValidX(x) || !Client.IsValidY(y))
        {
            return false;
        }
        var realLandTile = LandTiles[x, y];
        if (realLandTile == null)
            return false;
        
        if (GhostLandTiles.TryGetValue(realLandTile, out var ghostLandTile))
        {
            result = ghostLandTile.LandTile;
        }
        else
        {
            result = realLandTile.LandTile;
        }
        return true;
    }

    public StaticObject? GetTile(StaticTile staticTile)
    {
        var list = StaticTiles[staticTile.X, staticTile.Y];
        if (list == null || list.Count == 0)
            return null;
        return list.FirstOrDefault(so => so.StaticTile.Equals(staticTile));
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
            AllTiles.Remove(lo.ObjectId);
            LandTilesCount--;
        }
        var so = StaticTiles[x, y];
        if (so != null)
        {
            StaticTiles[x, y] = null;
            StaticTilesCount -= so.Count;
            foreach (var staticObject in so)
            {
                AllTiles.Remove(staticObject.ObjectId);
                if (staticObject.IsAnimated)
                {
                    AnimatedStaticTiles.Remove(staticObject);
                }
                if (staticObject.IsLight)
                {
                    LightTiles.Remove(staticObject);
                }
            }
        }
    }

    public IEnumerable<TileObject> GetTiles(TileObject? t1, TileObject? t2, bool topTilesOnly)
    {
        var landOnly = ActiveTool.Name == "Draw" && CEDGame.UIManager.GetWindow<TilesWindow>().LandMode;
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
                    var tiles = StaticTiles[x, y]?.Where(CanDrawStatic);
                    var landTile = LandTiles[x, y];
                    if (tiles != null && tiles.Any() && !landOnly)
                    {
                        if (topTilesOnly)
                        {
                            yield return tiles.Last();
                        }
                        else
                        {
                            foreach (var tile in tiles)
                            {
                                yield return tile;
                            }
                        }
                    }
                    else if (landTile != null && CanDrawLand(landTile))
                    {
                        yield return landTile;
                    }
                }
            }
        }
    }
    
    private MouseState _prevMouseState = Mouse.GetState();
    private bool _IsMouseDragging;
    private KeyboardState _prevKeyState = Keyboard.GetState();
    public Rectangle ViewRange { get; private set; }

    public void Update(GameTime gameTime, bool isActive, bool processMouse, bool processKeyboard)
    {
        if (CEDGame.Closing)
            return;
        if (CEDClient.ServerState != ServerState.Running)
            return;
        
        Metrics.Start("UpdateMap");
        var mouseState = Mouse.GetState();
        Keymap.Update(Keyboard.GetState());
        var keyState = Keyboard.GetState();
        if (processMouse)
        {
            if (Client.Running)
            {
                if (NewSelected != Selected)
                {
                    if (DebugLogging)
                    {
                        Console.WriteLine($"New selected: {NewSelected?.Tile}");
                    }
                    ActiveTool.OnMouseLeave(Selected);
                    Selected = NewSelected;
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
                        CEDGame.UIManager.OpenContextMenu();
                    }
                }
                if (mouseState.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
                {
                    var scrollDelta = (mouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue) / 1200f;
                    if (Config.Instance.LegacyMouseScroll ^ (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown
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
        }
        else
        {
            ActiveTool.OnMouseLeave(Selected);
        }
        _prevMouseState = mouseState;

        if (isActive && processKeyboard)
        {
            var delta = keyState.IsKeyDown(Keys.LeftShift) ? 30 : 10;

            foreach (var key in keyState.GetPressedKeys())
            {
                if (_prevKeyState.IsKeyUp(key))
                {
                    ActiveTool.OnKeyPressed(key);
                }
            }
            foreach (var key in _prevKeyState.GetPressedKeys())
            {
                if (keyState.IsKeyUp(key))
                {
                    ActiveTool.OnKeyReleased(key);
                }
            }
            if (mouseState.LeftButton == ButtonState.Released)
            {
                foreach (var tool in Tools)
                {
                    if (keyState.IsKeyDown(tool.Shortcut) && _prevKeyState.IsKeyUp(tool.Shortcut))
                    {
                        ActiveTool = tool;
                        break;
                    }
                }
            }
            if (Keymap.IsKeyPressed(Keymap.ToggleAnimatedStatics))
            {
                AnimatedStatics = !AnimatedStatics;
            }
            if(Keymap.IsKeyPressed(Keymap.Minimap))
            {
                var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
                minimapWindow.Show = !minimapWindow.Show;
            }
            else
            {
                if (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl))
                {
                    if (IsKeyPressed(keyState, Keys.Z))
                    {
                        Client.Undo();
                    }
                    if (IsKeyPressed(keyState, Keys.R))
                    {
                        Reset();
                    }
                    if (IsKeyPressed(keyState, Keys.W))
                    {
                        WalkableSurfaces = !WalkableSurfaces;
                    }
                    if (IsKeyPressed(keyState, Keys.F))
                    {
                        FlatView = !FlatView;
                        UpdateAllTiles();
                    }
                    if (IsKeyPressed(keyState, Keys.S))
                    {
                        FlatStatics = !FlatStatics;
                        UpdateAllTiles();
                    }
                    if (IsKeyPressed(keyState, Keys.H))
                    {
                        FlatShowHeight = !FlatShowHeight;
                    }
                    if (IsKeyPressed(keyState, Keys.G))
                    {
                        ShowGrid = !ShowGrid;
                    }
                }
                else
                {
                    if (IsKeyPressed(keyState, Keys.Escape))
                    {
                        Camera.ResetZoom();
                    }
                    if(Keymap.IsKeyDown(Keymap.MoveLeft))
                    {
                        Move(-delta, delta);
                    }
                    if(Keymap.IsKeyDown(Keymap.MoveRight))
                    {
                        Move(delta, -delta);
                    }
                    if(Keymap.IsKeyDown(Keymap.MoveUp))
                    {
                        Move(-delta, -delta);
                    }
                    if(Keymap.IsKeyDown(Keymap.MoveDown))
                    {
                        Move(delta, delta);
                    }
                }
            }
            _prevKeyState = keyState;
        }

        Camera.Update();
        if (Client.Running)
        {
            CalculateViewRange(Camera, out var newViewRange);
            if (ViewRange != newViewRange)
            {
                List<PointU16> requested = new List<PointU16>();
                for (var x = newViewRange.Left / 8; x <= newViewRange.Right / 8; x++)
                {
                    for (var y = newViewRange.Top / 8; y <= newViewRange.Bottom / 8; y++)
                    {
                        requested.Add(new PointU16((ushort)x, (ushort)y));
                    }
                }

                ViewRange = newViewRange;
                Client.ResizeCache(ViewRange.Width * ViewRange.Height / 8);
                Client.RequestBlocks(requested);
            }
        }
        else
        {
            ViewRange = Rectangle.Empty;
        }
        if (Client.Running && AnimatedStatics)
        {
            _animatedStaticsManager.Process(gameTime);
            foreach (var animatedStaticTile in AnimatedStaticTiles)
            {
                animatedStaticTile.UpdateId();
            }
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

    private bool IsKeyPressed(KeyboardState keyState, Keys key)
    {
        return keyState.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);
    }

    public void Reset()
    {
        LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
        StaticTiles = new List<StaticObject>[Client.Width * 8, Client.Height * 8];
        AnimatedStaticTiles.Clear();
        GhostLandTiles.Clear();
        GhostStaticTiles.Clear();
        LightTiles.Clear();
        AllTiles.Clear();
        ViewRange = Rectangle.Empty;
        Client.ResizeCache(0);
        LandTilesCount = 0;
        StaticTilesCount = 0;
    }

    public void UpdateLights()
    {
        foreach (var light in LightTiles.Values)
        {
            light.Update();   
        }
    }

    public TileObject? Selected;
    public TileObject? NewSelected;

    private TileObject? GetMouseSelection(int x, int y)
    {
        if (!_selectionBuffer.Bounds.Contains(x, y))
        {
            return null;
        }
        if (UseVirtualLayer)
        {
            var virtualLayerPos = Unproject(x, y, VirtualLayerZ);
            var newX = (ushort)Math.Clamp(virtualLayerPos.X + 1, 0, Client.Width * 8 - 1);
            var newY = (ushort)Math.Clamp(virtualLayerPos.Y + 1, 0, Client.Height * 8 - 1);
            if (newX != Selected?.Tile.X || newY != Selected.Tile.Y)
            {
                return new VirtualLayerTile(newX, newY, (sbyte)VirtualLayerZ);
            }
            else
            {
                return Selected;
            }
        }
        Color[] pixels = new Color[1];
        _selectionBuffer.GetData(0, new Rectangle(x, y, 1, 1), pixels, 0, 1);
        var pixel = pixels[0];
        var selectedIndex = pixel.R | (pixel.G << 8) | (pixel.B << 16);
        if (selectedIndex < 1)
            return null;
        return AllTiles.GetValueOrDefault(selectedIndex);
    }

    private void CalculateViewRange(Camera camera, out Rectangle rect)
    {
        float zoom = camera.Zoom;
        int screenWidth = camera.ScreenSize.Width;
        int screenHeight = camera.ScreenSize.Height;
        
        // Calculate the size of the drawing diamond in pixels
        // Default is 2.0f, but 2.6f gives smaller viewrange without any visible problems 
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom / 2.6f;
        
        Vector3 center = camera.Position;
        
        // Render a few extra rows at the top to deal with things at lower z
        var minTileX = (int)Math.Max(0, (center.X - screenDiamondDiagonal) / TILE_SIZE - 8);
        var minTileY = (int)Math.Max(0, (center.Y - screenDiamondDiagonal) / TILE_SIZE - 8);
        
        // Render a few extra rows at the bottom to deal with things at higher z
        var maxTileX = (int)Math.Min(Client.Width * 8 - 1, (center.X + screenDiamondDiagonal) / TILE_SIZE + 8);
        var maxTileY = (int)Math.Min(Client.Height * 8 - 1, (center.Y + screenDiamondDiagonal) / TILE_SIZE + 8);
        rect = new Rectangle(minTileX, minTileY, maxTileX - minTileX, maxTileY - minTileY);
    }

    public Vector3 Unproject(int x, int y, int z)
    {
        var worldPoint = _gfxDevice.Viewport.Unproject
        (
            new Vector3(x, y, -(z / 384f) + 0.5f),
            Camera.WorldViewProj,
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

    public bool CanDrawStatic(StaticObject so)
    {
        var tile = so.StaticTile;
        var id = tile.Id;
        if (id >= TileDataLoader.Instance.StaticData.Length)
            return false;

        ref StaticTiles data = ref TileDataLoader.Instance.StaticData[id];

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
        var landTile = LandTiles[tile.X, tile.Y];
        if (!WithinZRange(tile.Z) || landTile != null && CanDrawLand(landTile) && 
            WithinZRange(landTile.Tile.Z) && landTile.AverageZ() >= tile.PriorityZ + 5)
            return false;
        
        if (!ShowStatics)
            return false;
        
        if(StaticFilterEnabled)
        {
            return !(StaticFilterInclusive ^ StaticFilterIds.Contains(id));
        }
        return so.Visible;
    }

    private static Vector4 NonWalkableHue = HuesManager.Instance.GetRGBVector(new Color(50, 0, 0));
    private static Vector4 WalkableHue = HuesManager.Instance.GetRGBVector(new Color(0, 50, 0));
    
    public bool IsWalkable(LandObject lo)
    {
        //TODO: Save value for later to avoid calculation, recalculate whole cell on operations
        if (lo.Walkable.HasValue)
            return lo.Walkable.Value;
        
        var landTile = lo.LandTile;
        bool walkable = !TileDataLoader.Instance.LandData[landTile.Id].IsImpassable;
        if (!walkable)
        {
            return false;
        }
        var staticObjects = StaticTiles[landTile.X, landTile.Y];
        if (staticObjects != null)
        {
            foreach (var so in staticObjects)
            {
                var staticTile = so.StaticTile;
                var staticTileData = TileDataLoader.Instance.StaticData[staticTile.Id];
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
        var thisTileData = TileDataLoader.Instance.StaticData[tile.Id];
        var thisCalculatedHeight = thisTileData.IsBridge ? thisTileData.Height / 2 : thisTileData.Height;
        bool walkable = !thisTileData.IsImpassable;
        if (walkable)
        {
            var staticObjects = StaticTiles[tile.X, tile.Y];
            if (staticObjects != null)
            {
                foreach (var so2 in staticObjects)
                {
                    var staticTile = so2.StaticTile;
                    var staticTileData = TileDataLoader.Instance.StaticData[staticTile.Id];
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
        if (landTile.Id > TileDataLoader.Instance.LandData.Length)
            return;
        if (!CanDrawLand(lo))
            return;

        _mapRenderer.DrawMapObject(lo, hueOverride);
    }

    public void Draw()
    {
        Metrics.Start("DrawMap");
        if (!Client.Running || CEDGame.Closing)
        {
            DrawBackground();
            return;
        }
        Metrics.Measure("DrawSelection", DrawSelectionBuffer);
        Metrics.Start("GetMouseSelection");
        NewSelected = GetMouseSelection(_prevMouseState.X, _prevMouseState.Y);
        Metrics.Stop("GetMouseSelection");
        if (DebugDrawSelectionBuffer)
            return;
        
        Metrics.Measure("DrawLights", () => DrawLights(Camera));
        if (DebugDrawLightMap)
            return;
        
        _mapRenderer.SetRenderTarget(null);
        Metrics.Measure("DrawLand", () => DrawLand(Camera, ViewRange));
        Metrics.Start("DrawLandGrid");
        if (ShowGrid)
        {
            DrawLand(Camera, ViewRange, "TerrainGrid");
        }
        Metrics.Stop("DrawLandGrid");
        Metrics.Measure("DrawLandHeight", DrawLandHeight);
        Metrics.Measure("DrawStatics", () => DrawStatics(Camera, ViewRange));
        Metrics.Measure("ApplyLights", ApplyLights);
        Metrics.Measure("DrawVirtualLayer", DrawVirtualLayer);
        Metrics.Stop("DrawMap");    
    }

    private void DrawBackground()
    {
        _mapRenderer.SetRenderTarget(null);
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.BlendState = BlendState.AlphaBlend;
        _spriteBatch.Begin();
        var windowRect = _GameWindow.ClientBounds;
        var backgroundRect = new Rectangle
        (
            windowRect.Width / 2 - _background.Width / 2,
            windowRect.Height / 2 - _background.Height / 2,
            _background.Width,
            _background.Height
        );
        _spriteBatch.Draw(_background, backgroundRect, Color.White);
        _spriteBatch.End();
    }

    private void DrawSelectionBuffer()
    {
        _mapEffect.WorldViewProj = Camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Selection"];
        _mapRenderer.SetRenderTarget(DebugDrawSelectionBuffer ? null : _selectionBuffer);
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        for (var x = ViewRange.Left; x < ViewRange.Right; x++)
        {
            for (var y = ViewRange.Top; y < ViewRange.Bottom; y++)
            {
                var landTile = LandTiles[x, y];
                if (landTile != null)
                {
                    DrawLand(landTile, landTile.ObjectIdColor);
                }

                var tiles = StaticTiles[x, y];
                if(tiles == null) continue;
                foreach (var tile in tiles)
                {
                    if (tile.CanDraw)
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
        _mapEffect.WorldViewProj = camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];
        _mapRenderer.SetRenderTarget(DebugDrawLightMap ? null : _lightMap);
        _gfxDevice.Clear(ClearOptions.Target, LightsManager.Instance.GlobalLightLevelColor, 0f, 0);
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone, 
            SamplerState.PointClamp,
            DepthStencilState.None, 
            BlendState.Additive
        );
        foreach (var kvp in LightTiles)
        {
            var staticTile = kvp.Key;
            var light = kvp.Value;
            if (light.CanDraw)
            {
                if (CanDrawStatic(staticTile))
                {
                    _mapRenderer.DrawMapObject(light, default);
                }
            }
        }
        _mapRenderer.End();
    }

    private void DrawLand(Camera camera, Rectangle viewRange, string technique = "Terrain")
    {
        if (!ShowLand)
        {
            return;
        }
        _mapEffect.WorldViewProj = camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques[technique];
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
           
        for (var x = viewRange.Left; x < viewRange.Right; x++)
        {
            for (var y = viewRange.Top; y < viewRange.Bottom; y++)
            {
                var tile = LandTiles[x, y];
                if (tile != null && tile.CanDraw)
                {
                    var hueOverride = Vector4.Zero;
                    if (WalkableSurfaces && !TileDataLoader.Instance.LandData[tile.LandTile.Id].IsWet)
                    {
                        hueOverride = IsWalkable(tile) ? WalkableHue : NonWalkableHue;

                    }
                    DrawLand(tile, hueOverride);
                }
            }
        }
        foreach (var tile in GhostLandTiles.Values)
        {
            DrawLand(tile);
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
        for (var x = ViewRange.Left; x < ViewRange.Right; x++)
        {
            for (var y = ViewRange.Top; y < ViewRange.Bottom; y++)
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
        var projected = _gfxDevice.Viewport.Project
            (tile.Vertices[0].Position, Camera.WorldViewProj, Matrix.Identity, Matrix.Identity);
        var pos = new Vector2
            (projected.X - halfTextSize.X, projected.Y + yOffset);
        var windowRect = _GameWindow.ClientBounds;
        if (pos.X > 0 && pos.X < windowRect.Width && pos.Y > 0 &&
            pos.Y < windowRect.Height)
        {
            _spriteBatch.DrawString(font, text, pos, Color.White);
        }
    }

    private void DrawStatics(Camera camera, Rectangle viewRange)
    {
        if (!ShowStatics)
        {
            return;
        }
        _mapEffect.WorldViewProj = camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        for (var x = viewRange.Left; x < viewRange.Right; x++)
        {
            for (var y = viewRange.Top; y < viewRange.Bottom; y++)
            {
                var tiles = StaticTiles[x, y];
                if(tiles == null) continue;
                foreach (var tile in tiles)
                {
                    if (tile.CanDraw)
                    {
                        var hueOverride = Vector4.Zero;
                        if (WalkableSurfaces && TileDataLoader.Instance.StaticData[tile.Tile.Id].IsSurface)
                        {
                            hueOverride = IsWalkable(tile) ? WalkableHue : NonWalkableHue;
                        }
                        DrawStatic(tile, hueOverride);
                    }
                }
            }
        }
        foreach (var tile in GhostStaticTiles.Values)
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
        _spriteBatch.Draw(_lightMap, Vector2.Zero, LightsManager.Instance.ApplyBlendColor);
        _spriteBatch.End();
    }

    public void DrawVirtualLayer()
    {
        if (!ShowVirtualLayer)
        {
            return;
        }
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["VirtualLayer"];
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _DepthStencilState,
            BlendState.AlphaBlend
        );
        VirtualLayer.Z = (sbyte)VirtualLayerZ;
        _mapRenderer.DrawMapObject(VirtualLayer, Vector4.Zero);
        _mapRenderer.End();
    }

    public void ExportImage(string path, int widthPx, int heightPx, float zoom)
    {
        var pp = _gfxDevice.PresentationParameters;
        if (widthPx != pp.BackBufferWidth || heightPx != pp.BackBufferHeight)
        {
            pp.BackBufferWidth = widthPx;
            pp.BackBufferHeight = heightPx;
            pp.DeviceWindowHandle = CEDGame.Window.Handle;
            _gfxDevice.Reset(pp);
        }
        var myRenderTarget = new RenderTarget2D(_gfxDevice, widthPx, heightPx, false, SurfaceFormat.Color, DepthFormat.Depth24);
        var prevLightMap = _lightMap;
        _lightMap = new RenderTarget2D
        (
            _gfxDevice,
            widthPx,
            heightPx,
            _lightMap.LevelCount >  1,
            _lightMap.Format,
            _lightMap.DepthStencilFormat
        );
        
        var myCamera = new Camera();
        myCamera.Position = Camera.Position;
        myCamera.Zoom = zoom;
        myCamera.ScreenSize = myRenderTarget.Bounds;
        myCamera.Update();
        
        CalculateViewRange(myCamera, out var bounds);
        List<PointU16> requested = new List<PointU16>();
        for (var x = bounds.Left / 8; x <= bounds.Right / 8; x++)
        {
            for (var y = bounds.Top / 8; y <= bounds.Bottom / 8; y++)
            {
                requested.Add(new PointU16((ushort)x, (ushort)y));
            }
        }

        Client.ResizeCache(bounds.Width * bounds.Height / 8);
        Client.RequestBlocks(requested);
        while(Client.WaitingForBlocks) 
            Client.Update();
        
        foreach (var landObject in _ToRecalculate)
        {
            landObject.Update();
        }
        _ToRecalculate.Clear();
        
        _mapEffect.WorldViewProj = myCamera.WorldViewProj;
        DrawLights(myCamera);
        _mapRenderer.SetRenderTarget(myRenderTarget, new Rectangle(0,0, widthPx, heightPx));
        DrawLand(myCamera, bounds);
        DrawStatics(myCamera, bounds);
        ApplyLights();
        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        if(path.EndsWith(".png"))
            myRenderTarget.SaveAsPng(fs, myRenderTarget.Width, myRenderTarget.Height);
        else
        {
            if (!path.EndsWith(".jpg"))
            {
                Console.WriteLine("[EXPORT], invalid file format, exporting as JPEG");
            }
            myRenderTarget.SaveAsJpeg(fs, myRenderTarget.Width, myRenderTarget.Height);
        }
        myRenderTarget.Dispose();
        OnWindowsResized(_GameWindow);
    }

    public void OnWindowsResized(GameWindow window)
    {
        var windowSize = window.ClientBounds;
        Camera.ScreenSize = windowSize;
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
    }
}