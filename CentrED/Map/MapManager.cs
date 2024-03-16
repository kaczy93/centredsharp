using CentrED.Client;
using CentrED.IO;
using CentrED.Network;
using CentrED.Renderer;
using CentrED.Renderer.Effects;
using CentrED.Tools;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Map;

public class MapManager
{
    private readonly GraphicsDevice _gfxDevice;

    private MapEffect _mapEffect;
    public MapEffect MapEffect => _mapEffect;
    private readonly MapRenderer _mapRenderer;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _background;

    public bool DebugDrawSelectionBuffer;
    private RenderTarget2D _selectionBuffer;

    internal List<Tool> Tools = new();
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

    public Tool DefaultTool => Tools[0];

    public CentrEDClient Client;

    public bool ShowLand = true;
    public bool ShowStatics = true;
    public bool ShowVirtualLayer = false;
    public bool ShowNoDraw = false;
    public int VirtualLayerZ;
    public bool UseVirtualLayer = false;
    public bool WalkableSurfaces = false;
    public bool FlatView = false;
    public bool FlatStatics = false;
    public Dictionary<ushort, List<String>> tileLandBrushesNames = new();

    public readonly Camera Camera = new();

    private DepthStencilState _depthStencilState = new()
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

    public int[] ValidLandIds { get; private set; }
    public int[] ValidStaticIds { get; private set; }

    public MapManager(GraphicsDevice gd)
    {
        _gfxDevice = gd;
        _mapEffect = new MapEffect(gd);
        _mapRenderer = new MapRenderer(gd);

        _selectionBuffer = new RenderTarget2D
        (
            gd,
            gd.PresentationParameters.BackBufferWidth,
            gd.PresentationParameters.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24
        );
        _spriteBatch = new SpriteBatch(gd);
        
        using (var fileStream = File.OpenRead("background.png"))
        {
            _background = Texture2D.FromStream(gd, fileStream);
        }
        
        Client = CEDClient;
        Client.LandTileReplaced += (tile, newId) =>
        {
            var landTile = LandTiles[tile.X, tile.Y];
            landTile.UpdateCorners(newId);
            landTile.UpdateId(newId);
        };
        Client.LandTileElevated += (tile, newZ) =>
        {
            Point[] offsets =
            {
                new(0, 0),  //top
                new(-1, 0), //right
                new(0, -1), //left
                new(-1, -1) //bottom
            };

            for (var i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i];
                var newX = tile.X + offset.X;
                var newY = tile.Y + offset.Y;
                if(!Client.IsValidX(newX) || !Client.IsValidY(newY))
                    continue;
                
                var landObject = LandTiles[newX, newY];
                landObject.Vertices[i].Position.Z = newZ * TileObject.TILE_Z_SCALE;
                landObject.UpdateId(landObject.LandTile.Id); //Just refresh ID to refresh if it's flat
            }
        };
        Client.BlockLoaded += block =>
        {
            foreach (var landTile in block.LandBlock.Tiles)
            {
                AddTile(landTile);
            }
            foreach (var staticTile in block.StaticBlock.AllTiles())
            {
                AddTile(staticTile);
            }
            var landBlock = block.LandBlock;
            ushort minTileX = (ushort)(landBlock.X * 8);
            ushort minTileY = (ushort)(landBlock.Y * 8);
            for (ushort x = minTileX; x < minTileX + 8; x++)
            {
                if(x == 0 || minTileY == 0) continue;
                
                var newZ = landBlock.Tiles[LandBlock.GetTileId(x, minTileY)].Z;
                LandTiles?[x - 1, minTileY - 1]?.UpdateBottomCorner(newZ);
                LandTiles?[x, minTileY - 1]?.UpdateLeftCorner(newZ);
            }
            for (ushort y = minTileY; y < minTileY + 8; y++)
            {
                if(y == 0 || minTileX == 0) continue;
                
                var newZ = landBlock.Tiles[LandBlock.GetTileId(minTileX, y)].Z;
                LandTiles?[minTileX - 1, y - 1]?.UpdateBottomCorner(newZ);
                LandTiles?[minTileX - 1, y]?.UpdateRightCorner(newZ);
            }
        };
        Client.BlockUnloaded += block =>
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
        };
        Client.StaticTileRemoved += RemoveTile;
        Client.StaticTileAdded += tile =>
        {
            AddTile(tile);
        };
        Client.StaticTileMoved += (tile, newX, newY) =>
        {
            MoveTile(tile, newX, newY);
        };
        Client.StaticTileElevated += (tile, newZ) =>
        {
            GetTile(tile)?.UpdatePos(tile.X, tile.Y, newZ);
        };
        Client.AfterStaticChanged += tile =>
        {
            foreach (var staticObject in StaticTiles[tile.X, tile.Y])
            {
                staticObject.UpdateDepthOffset();
            }
        };
        Client.StaticTileHued += (tile, newHue) =>
        {
            GetTile(tile)?.UpdateHue(newHue);
        };
        Client.Moved += (x, y) => Position = new Point(x,y);
        Client.Connected += () =>
        {
            LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
            StaticTiles = new List<StaticObject>[Client.Width * 8, Client.Height * 8];
            VirtualLayer.Width = (ushort)(Client.Width * 8);
            VirtualLayer.Height = (ushort)(Client.Height * 8);
            InitLandBrushes();
        };
        Client.Disconnected += () =>
        {
            LandTiles = new LandObject[0,0];
            StaticTiles = new List<StaticObject>[0,0];
            AllTiles.Clear();
        };

        Camera.ScreenSize.Width = gd.PresentationParameters.BackBufferWidth;
        Camera.ScreenSize.Height = gd.PresentationParameters.BackBufferHeight;
        
        Tools.Add(new SelectTool()); //Select tool have to be first!
        Tools.Add(new DrawTool());
        Tools.Add(new MoveTool());
        Tools.Add(new ElevateTool());
        Tools.Add(new RemoveTool());
        Tools.Add(new HueTool());
        Tools.Add(new LandBrushTool());

        _activeTool = DefaultTool;
    }

    public void InitLandBrushes()
    {
        tileLandBrushesNames.Clear();
        var landBrushes = ProfileManager.ActiveProfile.LandBrush;
        foreach (var keyValuePair in landBrushes)
        {
            var name = keyValuePair.Key;
            var brush = keyValuePair.Value;
            var fullTiles = brush.Tiles;
            foreach (var fullTile in fullTiles)
            {
                AddLandBrushEntry(fullTile, name);
            }
            var transitions = brush.Transitions;
            foreach (var valuePair in transitions)
            {
                var toName = valuePair.Key;
                var tiles = valuePair.Value;
                foreach (var tile in tiles)
                {
                    AddLandBrushEntry(tile.TileID, name);
                }
            }
        }
    }

    private void AddLandBrushEntry(ushort tileId, string name)
    {
        if (!tileLandBrushesNames.ContainsKey(tileId))
        {
            tileLandBrushesNames.Add(tileId, new List<string>());
        }
        tileLandBrushesNames[tileId].Add(name);
    }

    public void ReloadShader()
    {
        if(File.Exists("MapEffect.fxc")) 
            _mapEffect = new MapEffect(_gfxDevice, File.ReadAllBytes("MapEffect.fxc"));
        _mapEffect.HueCount = HuesManager.Instance.HuesCount;
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
                }
            ).Wait(TimeSpan.FromSeconds(10.0)))
            Log.Panic("Loading files timeout.");

        TextureAtlas.InitializeSharedTexture(_gfxDevice);
        HuesManager.Load(_gfxDevice);
        _mapEffect.HueCount = HuesManager.Instance.HuesCount;

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
        Client.InitTileData(ref tdl.LandData, ref tdl.StaticData);
    }

    public Point Position
    {
        get => new((int)(Camera.Position.X / TileObject.TILE_SIZE), (int)(Camera.Position.Y / TileObject.TILE_SIZE));
        set {
            Camera.Position.X = value.X * TileObject.TILE_SIZE;
            Camera.Position.Y = value.Y * TileObject.TILE_SIZE;
            Client.SetPos((ushort)value.X, (ushort)value.Y);
        }
    }

    //Math.Cos(MathHelper.ToRadians(-45)), Math.Sin is negative
    private const float RotationConst = 0.70710676573223719f;

    public static Vector2 ScreenToMapCoordinates(float x, float y)
    {
        return new Vector2(x * RotationConst - y * -RotationConst, x * -RotationConst + y * RotationConst);
    }
    
    public Dictionary<int, TileObject> AllTiles = new();
    public LandObject?[,] LandTiles;
    public int LandTilesCount;
    public Dictionary<LandObject, LandObject> GhostLandTiles = new();
    public List<StaticObject>?[,] StaticTiles;
    public int StaticTilesCount;
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
    
    public void AddTile(LandTile landTile)
    {
        var lo = new LandObject(landTile);
        LandTiles[landTile.X, landTile.Y] = lo;
        AllTiles.Add(lo.ObjectId, lo);
        LandTilesCount++;
    }

    public void MoveTile(StaticTile staticTile, ushort newX, ushort newY)
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
        }
    }

    public StaticObject? GetTile(StaticTile staticTile)
    {
        var list = StaticTiles[staticTile.X, staticTile.Y];
        if (list == null || list.Count == 0)
            return null;
        return list.FirstOrDefault(so => so.StaticTile.Equals(staticTile));
    }
    
    public void AddTile(StaticTile staticTile)
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
        AllTiles.Add(so.ObjectId, so);
        StaticTilesCount++;
    }

    public void RemoveTile(StaticTile staticTile)
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
            AllTiles.Remove(found.ObjectId);
        }
        StaticTilesCount--;
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
            }
        }
    }

    public IEnumerable<TileObject> GetTopTiles(TileObject? t1, TileObject? t2)
    {
        var landOnly = ActiveTool.Name == "Draw" && CEDGame.UIManager.TilesWindow.LandMode;
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
                    var tiles = StaticTiles[x, y]?.Where(so => IsTileVisible(so.Tile.Id));
                    var landTile = LandTiles[x, y];
                    if (tiles != null && tiles.Any() && !landOnly)
                    {
                        yield return tiles.Last();
                    }
                    else if (landTile != null)
                    {
                        yield return landTile;
                    }
                }
            }
        }
    }
    
    private MouseState _prevMouseState = Mouse.GetState();
    private bool _mouseDrag;
    private KeyboardState _prevKeyState = Keyboard.GetState();
    public Rectangle ViewRange { get; private set; }

    public void Update(GameTime gameTime, bool isActive, bool processMouse, bool processKeyboard)
    {
        if (CEDGame.Closing)
            return;
        Metrics.Start("UpdateMap");
        var mouseState = Mouse.GetState();
        var keyState = Keyboard.GetState();
        if (isActive && processMouse)
        {
            if (mouseState.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                var delta = (mouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue) / 1200f;
                if(Config.Instance.LegacyMouseScroll ^ (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl)))
                {
                    if (Selected != null)
                        Selected.Tile.Z += (sbyte)(delta * 10);
                }
                else {
                    Camera.ZoomIn(delta);
                }
            }
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                var oldPos = new Vector2(_prevMouseState.X - mouseState.X, _prevMouseState.Y - mouseState.Y);
                if (oldPos != Vector2.Zero)
                {
                    var newPos = ScreenToMapCoordinates(oldPos.X, oldPos.Y);
                    Camera.Move(newPos.X, newPos.Y);
                    _mouseDrag = true;
                }
            }
            if (!_mouseDrag && mouseState.RightButton == ButtonState.Released && _prevMouseState.RightButton == ButtonState.Pressed)
            {
                CEDGame.UIManager.OpenContextMenu();
            }
            if (Client.Running)
            {
                Metrics.Start("GetMouseSelection");
                var newSelected = GetMouseSelection(mouseState.X, mouseState.Y);
                Metrics.Stop("GetMouseSelection");
                if (newSelected != Selected)
                {
                    ActiveTool.OnMouseLeave(Selected);
                    Selected = newSelected;
                    ActiveTool.OnMouseEnter(Selected);
                }
                if (Selected != null)
                {
                    if (_prevMouseState.LeftButton == ButtonState.Released && mouseState.LeftButton == ButtonState.Pressed)
                    {
                        ActiveTool.OnMousePressed(Selected);
                    }
                }
            }
            if (_mouseDrag && mouseState.RightButton == ButtonState.Released)
            {
                _mouseDrag = false;
            }
            if ( _prevMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released)
            {
                ActiveTool.OnMouseReleased(Selected);
                ActiveTool.OnMouseLeave(Selected); //Make sure that we leave tile to clear any ghosts
                Selected = null; //Very dirty way to retrigger OnMouseEnter() after something presumably changed
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
            if (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl))
            {
                if(IsKeyPressed(keyState, Keys.Z))
                {
                    Client.Undo();
                }
                if(IsKeyPressed(keyState, Keys.R))
                {
                    Reset();
                }
                if(IsKeyPressed(keyState, Keys.W))
                {
                    WalkableSurfaces = !WalkableSurfaces;
                }
                if(IsKeyPressed(keyState, Keys.F))
                {
                    FlatView = !FlatView;
                    UpdateAllTiles();
                }
                if(IsKeyPressed(keyState, Keys.S))
                {
                    FlatStatics = !FlatStatics;
                    UpdateAllTiles();
                }
            }
            else
            {
                if(IsKeyPressed(keyState, Keys.Escape))
                {
                    Camera.ResetZoom();
                }
                if(keyState.IsKeyDown(Keys.A))
                {
                    Camera.Move(-delta, delta);
                }
                if(keyState.IsKeyDown(Keys.D))
                {
                    Camera.Move(delta, -delta);
                }
                if(keyState.IsKeyDown(Keys.W))
                {
                    Camera.Move(-delta, -delta);
                }
                if(keyState.IsKeyDown(Keys.S))
                {
                    Camera.Move(delta, delta);
                }
            }
            _prevKeyState = keyState;
        }

        Camera.Update();
        CalculateViewRange(Camera, out var newViewRange);
        if (ViewRange != newViewRange)
        {
            List<BlockCoords> requested = new List<BlockCoords>();
            for (var x = newViewRange.Left / 8; x <= newViewRange.Right / 8; x++)
            {
                for (var y = newViewRange.Top / 8; y <= newViewRange.Bottom / 8; y++)
                {
                    requested.Add(new BlockCoords((ushort)x, (ushort)y));
                }
            }

            ViewRange = newViewRange;
            if (Client.Initialized)
            {
                Client.ResizeCache(ViewRange.Width * ViewRange.Height / 8);
                Client.LoadBlocks(requested);
            }
        }
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
        GhostLandTiles.Clear();
        GhostStaticTiles.Clear();
        AllTiles.Clear();
        ViewRange = Rectangle.Empty;
        Client.ResizeCache(0);
        LandTilesCount = 0;
        StaticTilesCount = 0;
    }

    public TileObject? Selected;

    private TileObject? GetMouseSelection(int x, int y)
    {
        if (!_gfxDevice.Viewport.Bounds.Contains(x, y))
        {
            return null;
        }
        if (UseVirtualLayer)
        {
            var virtualLayerPos = Unproject(x, y, VirtualLayerZ);
            var newX = (ushort)Math.Clamp(virtualLayerPos.X + 1, ushort.MinValue, ushort.MaxValue);
            var newY = (ushort)Math.Clamp(virtualLayerPos.Y + 1, ushort.MinValue, ushort.MaxValue);
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
        var minTileX = (int)Math.Max(0, (center.X - screenDiamondDiagonal) / TileObject.TILE_SIZE - 8);
        var minTileY = (int)Math.Max(0, (center.Y - screenDiamondDiagonal) / TileObject.TILE_SIZE - 8);
        
        // Render a few extra rows at the bottom to deal with things at higher z
        var maxTileX = (int)Math.Min(Client.Width * 8 - 1, (center.X + screenDiamondDiagonal) / TileObject.TILE_SIZE + 8);
        var maxTileY = (int)Math.Min(Client.Height * 8 - 1, (center.Y + screenDiamondDiagonal) / TileObject.TILE_SIZE + 8);
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
            worldPoint.X / TileObject.TILE_SIZE,
            worldPoint.Y / TileObject.TILE_SIZE,
            worldPoint.Z / TileObject.TILE_Z_SCALE
        );
    }

    private bool CanDrawLand(ushort id)
    {
        return ShowNoDraw | id > 2;
    }

    private bool CanDrawStatic(ushort id)
    {
        if (id >= TileDataLoader.Instance.StaticData.Length)
            return false;

        ref StaticTiles data = ref TileDataLoader.Instance.StaticData[id];

        // Outlands specific
        // if ((data.Flags & TileFlag.NoDraw) != 0)
        //     return false;

        switch (id)
        {
            case 0x0001:
            case 0x21BC:
            case 0x63D3: return ShowNoDraw;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
                return ((data.Flags & TileFlag.Background) == 0 && (data.Flags & TileFlag.Surface) == 0
                        // && (data.Flags & TileFlag.NoDraw) == 0
                    );

            case 0x2198:
            case 0x2199:
            case 0x21A0:
            case 0x21A1:
            case 0x21A2:
            case 0x21A3:
            case 0x21A4: return ShowNoDraw;
        }
        
        return IsTileVisible(id);
    }

    public bool IsTileVisible(ushort id)
    {
        if(StaticFilterEnabled)
        {
            return !(StaticFilterInclusive ^ StaticFilterIds.Contains(id));
        }
        return true;
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

    private void DrawStatic(StaticObject so, Vector4 hueOverride = default)
    {
        var tile = so.StaticTile;
        if (!CanDrawStatic(tile.Id))
            return;

        var landTile = LandTiles[tile.X, tile.Y];
        if (!WithinZRange(tile.Z) 
            || landTile != null && CanDrawLand(landTile.Tile.Id) && WithinZRange(landTile.Tile.Z) && landTile.AverageZ() >= tile.PriorityZ + 5
            )
            return;

        _mapRenderer.DrawMapObject(so, hueOverride);
    }

    private void DrawLand(LandObject lo, Vector4 hueOverride = default)
    {
        var landTile = lo.LandTile;
        if (!CanDrawLand(landTile.Id))
            return;
        if (landTile.Id > TileDataLoader.Instance.LandData.Length)
            return;
        if (!WithinZRange(landTile.Z))
            return;

        _mapRenderer.DrawMapObject(lo, hueOverride);
    }

    public void Draw()
    {
        Metrics.Start("DrawMap");
        if (!Client.Initialized || CEDGame.Closing)
        {
            DrawBackground();
            return;
        }
        _gfxDevice.Viewport = new Viewport
        (
            0,
            0,
            _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight
        );
        Metrics.Start("DrawSelection");
        DrawSelectionBuffer();
        Metrics.Stop("DrawSelection");
        if (DebugDrawSelectionBuffer)
            return;
        
        _mapRenderer.SetRenderTarget(null);
        Metrics.Start("DrawLand");
        DrawLand();
        Metrics.Stop("DrawLand");
        Metrics.Start("DrawStatics");
        DrawStatics();
        Metrics.Stop("DrawStatics");
        Metrics.Start("DrawVirtualLayer");
        DrawVirtualLayer();
        Metrics.Stop("DrawVirtualLayer");
        Metrics.Stop("DrawMap");    }

    private void DrawBackground()
    {
        _gfxDevice.SetRenderTarget(null);
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.BlendState = BlendState.AlphaBlend;
        _spriteBatch.Begin();
        var backgroundRect = new Rectangle
        (
            _gfxDevice.PresentationParameters.BackBufferWidth / 2 - _background.Width / 2,
            _gfxDevice.PresentationParameters.BackBufferHeight / 2 - _background.Height / 2,
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
            _depthStencilState,
            BlendState.AlphaBlend,
            null
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

    private void DrawLand()
    {
        if (!ShowLand)
        {
            return;
        }
        _mapEffect.WorldViewProj = Camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _depthStencilState,
            BlendState.AlphaBlend,
            HuesManager.Instance.Texture
        );
           
        for (var x = ViewRange.Left; x < ViewRange.Right; x++)
        {
            for (var y = ViewRange.Top; y < ViewRange.Bottom; y++)
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

    private void DrawStatics()
    {
        if (!ShowStatics)
        {
            return;
        }
        _mapEffect.WorldViewProj = Camera.WorldViewProj;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];
        _mapRenderer.Begin
        (
            _mapEffect,
            RasterizerState.CullNone,
            SamplerState.PointClamp,
            _depthStencilState,
            BlendState.AlphaBlend,
            HuesManager.Instance.Texture
        );
        for (var x = ViewRange.Left; x < ViewRange.Right; x++)
        {
            for (var y = ViewRange.Top; y < ViewRange.Bottom; y++)
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
            _depthStencilState,
            BlendState.AlphaBlend,
            null
        );
        VirtualLayer.Z = (sbyte)VirtualLayerZ;
        _mapRenderer.DrawMapObject(VirtualLayer, Vector4.Zero);
        _mapRenderer.End();
    }

    //TODO: Bring me back!
    public void DrawHighRes()
    {
        // Console.WriteLine("HIGH RES!");
        // var myRenderTarget = new RenderTarget2D(_gfxDevice, 15360, 8640, false, SurfaceFormat.Color, DepthFormat.Depth24);
        //
        // var myCamera = new Camera();
        // myCamera.Position = Camera.Position;
        // myCamera.Zoom = Camera.Zoom;
        // myCamera.ScreenSize = myRenderTarget.Bounds;
        // myCamera.Update();
        //
        // CalculateViewRange(myCamera, out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
        // List<BlockCoords> requested = new List<BlockCoords>();
        // for (var x = minTileX / 8; x < maxTileX / 8 + 1; x++) {
        //     for (var y = minTileY / 8; y < maxTileY / 8 + 1; y++) {
        //         requested.Add(new BlockCoords((ushort)x, (ushort)y));
        //     }
        // }
        // Client.ResizeCache(requested.Count * 4);
        // Client.LoadBlocks(requested);
        //
        // _mapEffect.WorldViewProj = myCamera.WorldViewProj;
        // _mapEffect.LightSource.Enabled = false;
        //
        // _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];
        // _mapRenderer.Begin(myRenderTarget, _mapEffect, myCamera, RasterizerState.CullNone, SamplerState.PointClamp,
        //     _depthStencilState, BlendState.AlphaBlend, null, _huesManager.Texture, true);
        // if (IsDrawStatic) {
        //     foreach (var tile in StaticTiles) {
        //         DrawStatic(tile, Vector3.Zero);
        //     }
        // }
        // _mapRenderer.End();
        //
        // _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];
        // _mapRenderer.Begin(myRenderTarget, _mapEffect, myCamera, RasterizerState.CullNone, SamplerState.PointClamp,
        //     _depthStencilState, BlendState.AlphaBlend, null, _huesManager.Texture, false);
        // if (IsDrawLand) {
        //     foreach (var tile in LandTiles) {
        //         DrawLand(tile, Vector3.Zero);
        //     }
        // }
        // _mapRenderer.End();
        //
        // using var fs = new FileStream(@"C:\git\CentrEDSharp\render.jpg", FileMode.OpenOrCreate);
        // myRenderTarget.SaveAsJpeg(fs, myRenderTarget.Width, myRenderTarget.Height);
        // Console.WriteLine("HIGH RES DONE!");
    }

    public void OnWindowsResized(GameWindow window)
    {
        Camera.ScreenSize = window.ClientBounds;
        Camera.Update();

        _selectionBuffer = new RenderTarget2D
        (
            _gfxDevice,
            _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None
        );
    }
}