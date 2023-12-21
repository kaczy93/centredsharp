using CentrED.Client;
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

    private RenderTarget2D _selectionBuffer;

    public Tool? ActiveTool;

    public CentrEDClient Client;

    public bool ShowLand = true;
    public bool ShowStatics = true;
    public bool ShowVirtualLayer = false;
    public int VirtualLayerZ = 0;
    public Vector3 VirtualLayerTilePos = Vector3.Zero;

    public readonly Camera Camera = new();

    private DepthStencilState _depthStencilState = new()
    {
        DepthBufferEnable = true,
        DepthBufferWriteEnable = true,
        DepthBufferFunction = CompareFunction.Less,
        StencilEnable = false
    };

    public readonly float TILE_SIZE = 31.11f;

    public int minZ = -127;
    public int maxZ = 127;

    public bool StaticFilterEnabled;
    public bool StaticFilterInclusive = true;
    public SortedSet<int> StaticFilterIds = new();

    public int[] ValidLandIds { get; private set; }
    public int[] ValidStaticIds { get; private set; }

    private void DarkenTexture(ushort[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            ushort c = pixels[i];

            int red = (c >> 10) & 0x1F;
            int green = (c >> 5) & 0x1F;
            int blue = c & 0x1F;

            red = (int)(red * 0.85355339f);
            green = (int)(green * 0.85355339f);
            blue = (int)(blue * 0.85355339f);

            pixels[i] = (ushort)((1 << 15) | (red << 10) | (green << 5) | blue);
        }
    }

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
        _background = CEDGame.Content.Load<Texture2D>("background");

        Client = CEDClient;
        Client.LandTileReplaced += (tile, newId) => { LandTiles[tile.X, tile.Y].UpdateId(newId); };
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
                var landObject = LandTiles[tile.X + offset.X, tile.Y + offset.Y];
                landObject.Vertices[i].Position.Z = newZ * MapObject.TILE_Z_SCALE;
                landObject.UpdateId(landObject.LandTile.Id); //Just refresh ID to refresh if it's flat
            }
        };
        Client.BlockLoaded += block =>
        {
            block.StaticBlock.SortTiles(ref TileDataLoader.Instance.StaticData);
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
                var newZ = landBlock.Tiles[LandBlock.GetTileId(x, minTileY)].Z;
                LandTiles?[x - 1, minTileY - 1]?.UpdateBottomCorner(newZ);
                LandTiles?[x, minTileY - 1]?.UpdateLeftCorner(newZ);
            }
            for (ushort y = minTileY; y < minTileY + 8; y++)
            {
                var newZ = landBlock.Tiles[LandBlock.GetTileId(minTileX, y)].Z;
                LandTiles?[minTileX - 1, y - 1]?.UpdateBottomCorner(newZ);
                LandTiles?[minTileX - 1, y]?.UpdateRightCorner(newZ);
            }
        };
        Client.BlockUnloaded += block =>
        {
            var landTiles = block.LandBlock.Tiles;
            foreach (var landTile in landTiles)
            {
                RemoveTile(landTile);
            }
            foreach (var staticTile in block.StaticBlock.AllTiles())
            {
                RemoveTile(staticTile);
            }
        };
        Client.StaticTileRemoved += RemoveTile;
        Client.StaticTileAdded += tile =>
        {
            tile.Block?.SortTiles(ref TileDataLoader.Instance.StaticData);
            AddTile(tile);
        };
        //We probably can do these 3 by recalculating instead of remove and add
        Client.StaticTileMoved += (tile, newX, newY) =>
        {
            RemoveTile(tile);
            var newTile = new StaticTile(tile.Id, newX, newY, tile.Z, tile.Hue, tile.Block);
            AddTile(newTile);
        };
        Client.StaticTileElevated += (tile, newZ) =>
        {
            RemoveTile(tile);
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, newZ, tile.Hue, tile.Block);
            AddTile(newTile);
        };
        Client.StaticTileHued += (tile, newHue) =>
        {
            RemoveTile(tile);
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, tile.Z, newHue, tile.Block);
            AddTile(newTile);
        };
        Client.Moved += (x, y) => Position = new Point(x,y);
        Client.Connected += () =>
        {
            LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
            StaticTiles = new List<StaticObject>[Client.Width * 8, Client.Height * 8];
            VirtualLayer.Width = (ushort)(Client.Width * 8);
            VirtualLayer.Height = (ushort)(Client.Height * 8);
        };
        Client.Disconnected += () =>
        {
            LandTiles = new LandObject[0,0];
            StaticTiles = new List<StaticObject>[0,0];
            AllTiles.Clear();
        };

        Camera.Position.X = 0;
        Camera.Position.Y = 0;
        Camera.ScreenSize.X = 0;
        Camera.ScreenSize.Y = 0;
        Camera.ScreenSize.Width = gd.PresentationParameters.BackBufferWidth;
        Camera.ScreenSize.Height = gd.PresentationParameters.BackBufferHeight;
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
                }
            ).Wait(TimeSpan.FromSeconds(10.0)))
            Log.Panic("Loading files timeout.");

        TextureAtlas.InitializeSharedTexture(_gfxDevice);
        HuesManager.Initialize(_gfxDevice);
        RadarMap.Initialize(_gfxDevice);

        var landIds = new List<int>();
        for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++)
        {
            if (!ArtLoader.Instance.GetValidRefEntry(i).Equals(UOFileIndex.Invalid))
            {
                landIds.Add(i);
            }
        }
        ValidLandIds = landIds.ToArray();
        var staticIds = new List<int>();
        for (int i = 0; i < TileDataLoader.Instance.StaticData.Length; i++)
        {
            if (!ArtLoader.Instance.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals
                    (UOFileIndex.Invalid))
            {
                staticIds.Add(i);
            }
        }
        ValidStaticIds = staticIds.ToArray();
    }

    public Point Position
    {
        get => new((int)(Camera.Position.X / TILE_SIZE), (int)(Camera.Position.Y / TILE_SIZE));
        set {
            if(value != Position) Camera.Moved = true;
            Camera.Position.X = value.X * TILE_SIZE;
            Camera.Position.Y = value.Y * TILE_SIZE;
        }
    }

    private enum MouseDirection
    {
        North,
        Northeast,
        East,
        Southeast,
        South,
        Southwest,
        West,
        Northwest
    }

    // This is all just a fast math way to figure out what the direction of the mouse is.
    private MouseDirection ProcessMouseMovement(ref MouseState mouseState, out float distance)
    {
        Vector2 vec = new Vector2
            (mouseState.X - (Camera.ScreenSize.Width / 2), mouseState.Y - (Camera.ScreenSize.Height / 2));

        int hashf = 100 * (Math.Sign(vec.X) + 2) + 10 * (Math.Sign(vec.Y) + 2);

        distance = vec.Length();
        if (distance == 0)
        {
            return MouseDirection.North;
        }

        vec.X = Math.Abs(vec.X);
        vec.Y = Math.Abs(vec.Y);

        if (vec.Y * 5 <= vec.X * 2)
        {
            hashf += 1;
        }
        else if (vec.Y * 2 >= vec.X * 5)
        {
            hashf += 3;
        }
        else
        {
            hashf += 2;
        }

        switch (hashf)
        {
            case 111: return MouseDirection.Southwest;
            case 112: return MouseDirection.West;
            case 113: return MouseDirection.Northwest;
            case 120: return MouseDirection.Southwest;
            case 131: return MouseDirection.Southwest;
            case 132: return MouseDirection.South;
            case 133: return MouseDirection.Southeast;
            case 210: return MouseDirection.Northwest;
            case 230: return MouseDirection.Southeast;
            case 311: return MouseDirection.Northeast;
            case 312: return MouseDirection.North;
            case 313: return MouseDirection.Northwest;
            case 320: return MouseDirection.Northeast;
            case 331: return MouseDirection.Northeast;
            case 332: return MouseDirection.East;
            case 333: return MouseDirection.Southeast;
        }

        return MouseDirection.North;
    }

    private readonly float WHEEL_DELTA = 1200f;
    
    public List<TileObject> AllTiles = new();
    public LandObject?[,] LandTiles;
    public int LandTilesCount;
    public List<LandObject> GhostLandTiles = new();
    public List<StaticObject>?[,] StaticTiles;
    public int StaticTilesCount;
    public List<StaticObject> GhostStaticTiles = new();
    public VirtualLayerObject VirtualLayer = VirtualLayerObject.Instance;
    
    
    public void AddTile(LandTile landTile)
    {
        var lo = new LandObject(landTile);
        LandTiles[landTile.X, landTile.Y] = lo;
        AllTiles.Add(lo);
        LandTilesCount++;
    }
    public void RemoveTile(LandTile landTile)
    {
        var lo = LandTiles[landTile.X, landTile.Y];
        if (lo != null)
        {
            LandTiles[landTile.X, landTile.Y] = null;
            AllTiles.Remove(lo);
            LandTilesCount--;
        }
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
        AllTiles.Add(so);
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
            AllTiles.Remove(found);
        }
        StaticTilesCount--;
    }
    
    private MouseState _prevMouseState = Mouse.GetState();
    private KeyboardState _prevKeyState = Keyboard.GetState();
    public Rectangle ViewRange { get; private set; }

    public void Update(GameTime gameTime, bool isActive, bool processMouse, bool processKeyboard)
    {
        if (CEDGame.Closing)
            return;
        Metrics.Start("UpdateMap");
        if (isActive && processMouse)
        {
            var mouseState = Mouse.GetState();

            // if (mouse.RightButton == ButtonState.Pressed)
            // {
            //     var direction = ProcessMouseMovement(ref mouse, out var distance);
            //
            //     int delta = distance > 200 ? 10 : 5;
            //     switch (direction)
            //     {
            //         case MouseDirection.North:
            //             Camera.Move(0, -delta);
            //             break;
            //         case MouseDirection.Northeast:
            //             Camera.Move(delta, -delta);
            //             break;
            //         case MouseDirection.East:
            //             Camera.Move(delta, 0);
            //             break;
            //         case MouseDirection.Southeast:
            //             Camera.Move(delta, delta);
            //             break;
            //         case MouseDirection.South:
            //             Camera.Move(0, delta);
            //             break;
            //         case MouseDirection.Southwest:
            //             Camera.Move(-delta, delta);
            //             break;
            //         case MouseDirection.West:
            //             Camera.Move(-delta, 0);
            //             break;
            //         case MouseDirection.Northwest:
            //             Camera.Move(-delta, -delta);
            //             break;
            //     }
            // }

            if (mouseState.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                var delta = (mouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue) / WHEEL_DELTA;
                Camera.ZoomIn(delta);
                //It can get buggy when zooming in due to how BlockCache is working :(
                //Just resetting is a safe way in cost of small performance spike and higher network traffic
                //TODO: FreeBlocks that are out of view range
                if(delta > 0)
                    Reset();
            }

            if (Client.Running && _gfxDevice.Viewport.Bounds.Contains(new Point(mouseState.X, mouseState.Y)))
            {
                var newTilePos = Unproject(mouseState.X, mouseState.Y, VirtualLayerZ);
                if (newTilePos != VirtualLayerTilePos)
                {
                    VirtualLayerTilePos = newTilePos;
                    ActiveTool?.OnVirtualLayerTile(VirtualLayerTilePos);
                }
                Metrics.Start("GetMouseSelection");
                var newSelected = GetMouseSelection(mouseState.X, mouseState.Y);
                Metrics.Stop("GetMouseSelection");
                if (newSelected != Selected)
                {
                    ActiveTool?.OnMouseLeave(Selected);
                    Selected = newSelected;
                    ActiveTool?.OnMouseEnter(Selected);
                }
                if (Selected != null)
                {
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        ActiveTool?.OnMousePressed(Selected);
                    }
                    if (mouseState.LeftButton == ButtonState.Released)
                    {
                        ActiveTool?.OnMouseReleased(Selected);
                    }
                }
            }
            _prevMouseState = mouseState;
        }
        else
        {
            ActiveTool?.OnMouseLeave(Selected);
        }

        if (isActive && processKeyboard)
        {
            var keyState = Keyboard.GetState();

            foreach (var key in keyState.GetPressedKeys())
            {
                switch (key)
                {
                    case Keys.Escape:
                        Camera.Zoom = 1;
                        Camera.Moved = true;
                        break;
                    case Keys.A:
                        Camera.Move(-10, 10);
                        break;
                    case Keys.D:
                        Camera.Move(10, -10);
                        break;
                    case Keys.W:
                        Camera.Move(-10, -10);
                        break;
                    case Keys.S:
                        Camera.Move(10, 10);
                        break;
                }
            }
            if((keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl)) && keyState.IsKeyDown(Keys.Z) && _prevKeyState.IsKeyUp(Keys.Z))
            {
                Client.Undo();
            }
            _prevKeyState = keyState;
        }

        Camera.Update();
        CalculateViewRange(Camera, out var newViewRange);
        if (ViewRange != newViewRange)
        {
            List<BlockCoords> requested = new List<BlockCoords>();
            for (var x = newViewRange.Left / 8; x < newViewRange.Right / 8; x++)
            {
                for (var y = newViewRange.Top / 8; y < newViewRange.Bottom / 8; y++)
                {
                    requested.Add(new BlockCoords((ushort)x, (ushort)y));
                }
            }

            if (Client.Initialized)
            {
                Client.ResizeCache(newViewRange.Width * newViewRange.Height / 8);
                Client.LoadBlocks(requested);
            }
            ViewRange = newViewRange;
        }
        Metrics.Stop("UpdateMap");
    }

    public void Reset()
    {
        LandTiles = new LandObject[Client.Width * 8, Client.Height * 8];
        StaticTiles = new List<StaticObject>[Client.Width * 8, Client.Height * 8];
        AllTiles.Clear();
        ViewRange = Rectangle.Empty;
        Client.ResizeCache(0);
        LandTilesCount = 0;
        StaticTilesCount = 0;
    }

    public TileObject? Selected;

    private TileObject? GetMouseSelection(int x, int y)
    {
        Color[] pixels = new Color[1];
        _selectionBuffer.GetData(0, new Rectangle(x, y, 1, 1), pixels, 0, 1);
        var pixel = pixels[0];
        var selectedIndex = pixel.R | (pixel.G << 8) | (pixel.B << 16);
        if (selectedIndex < 1)
            return null;
        return AllTiles.Find(t => t.ObjectId == selectedIndex);
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
            new Vector3(x, y, -(z / 256f) + 0.5f),
            Camera.WorldViewProj,
            Matrix.Identity,
            Matrix.Identity
        );
        return new Vector3
        (
            worldPoint.X / MapObject.TILE_SIZE,
            worldPoint.Y / MapObject.TILE_SIZE,
            worldPoint.Z / MapObject.TILE_Z_SCALE
        );
    }

    private bool CanDrawStatic(ushort id)
    {
        if (id >= TileDataLoader.Instance.StaticData.Length)
            return false;

        ref StaticTiles data = ref TileDataLoader.Instance.StaticData[id];

        // Outlands specific?
        // if ((data.Flags & TileFlag.NoDraw) != 0)
        //     return false;

        switch (id)
        {
            case 0x0001:
            case 0x21BC:
            case 0x63D3: return false;

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
            case 0x21A4: return false;
        }
        
        if(StaticFilterEnabled)
        {
            return !(StaticFilterInclusive ^ StaticFilterIds.Contains(id));
        }

        return true;
    }

    private bool WithinZRange(short z)
    {
        return z >= minZ && z <= maxZ;
    }

    private void DrawStatic(StaticObject so, Vector3 hueOverride = default)
    {
        var tile = so.Tile;
        if (!CanDrawStatic(tile.Id))
            return;

        var landTile = LandTiles[so.Tile.X, so.Tile.Y]?.Tile;
        if (!WithinZRange(tile.Z) || landTile != null && WithinZRange(landTile.Z) && landTile.Z > tile.Z + 5)
            return;

        _mapRenderer.DrawMapObject(so, hueOverride);
    }

    private void DrawLand(LandObject lo, Vector3 hueOverride = default)
    {
        if (lo.Tile.Id > TileDataLoader.Instance.LandData.Length)
            return;
        if (!WithinZRange(lo.Tile.Z))
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
        _mapRenderer.SetRenderTarget(_selectionBuffer);
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
                    var i = landTile.ObjectId;
                    var color = new Color(i & 0xFF, (i >> 8) & 0xFF, (i >> 16) & 0xFF);
                    DrawLand(landTile, color.ToVector3());
                }

                var tiles = StaticTiles[x, y];
                if(tiles == null) continue;
                foreach (var tile in tiles)
                {
                    if (tile.Visible)
                    {
                        var i = tile.ObjectId;
                        var color = new Color(i & 0xFF, (i >> 8) & 0xFF, (i >> 16) & 0xFF);
                        DrawStatic(tile, color.ToVector3());
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
                if (tile != null && tile.Visible)
                    DrawLand(tile);
            }
        }
        foreach (var tile in GhostLandTiles)
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
                    if (tile.Visible)
                        DrawStatic(tile);
                }
            }
        }
        foreach (var tile in GhostStaticTiles)
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
        _mapRenderer.DrawMapObject(VirtualLayer, Vector3.Zero);
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