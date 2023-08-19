using System.Collections.Specialized;
using CentrED.Client;
using CentrED.Network;
using CentrED.Renderer;
using CentrED.Renderer.Effects;
using CentrED.Tools;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Map;

public class MapManager {
    struct LandRenderInfo {
        public Vector4 CornerZ;
        public Vector3 NormalTop;
        public Vector3 NormalRight;
        public Vector3 NormalLeft;
        public Vector3 NormalBottom;
    }

    private Dictionary<LandTile, LandRenderInfo> _landRenderInfos = new();

    private readonly GraphicsDevice _gfxDevice;

    private readonly MapEffect _mapEffect;
    private readonly MapRenderer _mapRenderer;

    private RenderTarget2D _shadowTarget;
    private RenderTarget2D _selectionTarget;

    public Tool? ActiveTool;

    private readonly PostProcessRenderer _postProcessRenderer;

    public CentrEDClient Client;

    public static bool IsDrawLand = true, IsDrawStatic = true, IsDrawShadows = true;

    public Camera Camera => _camera; 
    private Camera _camera = new Camera();
    private Camera _lightSourceCamera = new Camera();

    private LightingState _lightingState = new LightingState();
    private DepthStencilState _depthStencilState = new DepthStencilState()
    {
        DepthBufferEnable = true,
        DepthBufferWriteEnable = true,
        DepthBufferFunction = CompareFunction.Less,
        StencilEnable = false
    };

    public int MIN_Z = -127;
    public int MAX_Z = 127;
    public readonly float TILE_SIZE = 31.11f;
    public float TILE_Z_SCALE = 4.0f;
    public float DEPTH_OFFSET = 0.0001f;

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
        _shadowTarget = new RenderTarget2D(
                                gd,
                                gd.PresentationParameters.BackBufferWidth * 2,
                                gd.PresentationParameters.BackBufferHeight * 2,
                                false,
                                SurfaceFormat.Single,
                                DepthFormat.Depth24);
        
        _selectionTarget = new RenderTarget2D(
            gd,
            gd.PresentationParameters.BackBufferWidth,
            gd.PresentationParameters.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24);
        _postProcessRenderer = new PostProcessRenderer(gd);

        Client = CentrED.Client;
        Client.LandTileReplaced += (tile, newId) => {
            for(var x = tile.X -1; x <= tile.X + 1; x++) {
                for (var y = tile.Y - 1; y <= tile.Y + 1; y++) {
                    if(Client.isValidX(x) && Client.isValidY(y))
                        FillRenderInfo(Client.GetLandTile(x, y));
                }
            }
        };
        Client.LandTileElevated += (tile, newZ) => {
            for(var x = tile.X -1; x <= tile.X + 1; x++) {
                for (var y = tile.Y - 1; y <= tile.Y + 1; y++) {
                    if(Client.isValidX(x) && Client.isValidY(y))
                        FillRenderInfo(Client.GetLandTile(x, y));
                }
            }
        };
        Client.BlockLoaded += block => {
            StaticTiles.EnsureCapacity(StaticTiles.Count + block.StaticBlock.TotalTilesCount);
            for (ushort y = 0; y < 8; y++) {
                for (ushort x = 0; x < 8; x++) {
                    var i = 0;
                    block.StaticBlock.SortTiles(ref TileDataLoader.Instance.StaticData);
                    foreach (var staticTile in block.StaticBlock.GetTiles(x, y).Reverse()) {
                        StaticTiles.Add(staticTile);
                    }

                    foreach (var tile in block.LandBlock.Tiles) {
                        LandTiles.Add(tile);
                    }
                }
            }
        };
        Client.BlockUnloaded += block => {
             var tiles = block.StaticBlock.AllTiles();
             foreach (var tile in tiles) {
                 StaticTiles.Remove(tile);
             }
        };
        Client.StaticTileRemoved += tile => {
            StaticTiles.Remove(tile);
        };
        Client.StaticTileAdded += tile => {
            StaticTiles.Add(tile);
        };
        Client.StaticTileElevated += (tile, newZ) => {
            StaticTiles.Remove(tile);
            StaticTiles.Add(new StaticTile(tile.Id, tile.X, tile.Y, newZ, tile.Hue, tile.Block));
        };
        Client.Moved += (x, y) => {
            _camera.Position.X = x * TILE_SIZE;
            _camera.Position.Y = y * TILE_SIZE;
        };
        Client.Connected += () => {
            LandTiles.Clear();
            StaticTiles.Clear();
        };
        Client.Disconnected += () => {
            LandTiles.Clear();
            StaticTiles.Clear();
        };

        _camera.Position.X = 0;
        _camera.Position.Y = 0;
        _camera.ScreenSize.X = 0;
        _camera.ScreenSize.Y = 0;
        _camera.ScreenSize.Width = gd.PresentationParameters.BackBufferWidth;
        _camera.ScreenSize.Height = gd.PresentationParameters.BackBufferHeight;

        // This has to match the LightDirection below
        _lightSourceCamera.Position = _camera.Position;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightingState.LightDirection = new Vector3(0, -1, -1f);
        _lightingState.LightDiffuseColor = Vector3.Normalize(new Vector3(1, 1, 1));
        _lightingState.LightSpecularColor = Vector3.Zero;
        _lightingState.AmbientLightColor = new Vector3(
            1f - _lightingState.LightDiffuseColor.X,
            1f - _lightingState.LightDiffuseColor.Y,
            1f - _lightingState.LightDiffuseColor.Z
        );
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
        Vector2 vec = new Vector2(mouseState.X - (_camera.ScreenSize.Width / 2), mouseState.Y - (_camera.ScreenSize.Height / 2));

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
            hashf = hashf + 1;
        }
        else if (vec.Y * 2 >= vec.X * 5)
        {
            hashf = hashf + 3;
        }
        else
        {
            hashf = hashf + 2;
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


    private int _lastScrollWheel;
    private readonly float WHEEL_DELTA = 1200f;

    public List<LandTile> LandTiles = new();
    public List<StaticTile> StaticTiles = new();
    private MouseState prevMouseState = Mouse.GetState();

    public void Update(GameTime gameTime, bool processMouse, bool processKeyboard)
    {
        if (processMouse)
        {
            var mouse = Mouse.GetState();

            if (mouse.RightButton == ButtonState.Pressed)
            {
                var direction = ProcessMouseMovement(ref mouse, out var distance);

                int increment = distance > 200 ? 10 : 5;
                switch (direction)
                {
                    case MouseDirection.North:
                        _camera.Position.Y -= increment;
                        break;
                    case MouseDirection.Northeast:
                        _camera.Position.Y -= increment;
                        _camera.Position.X += increment;
                        break;
                    case MouseDirection.East:
                        _camera.Position.X += increment;
                        break;
                    case MouseDirection.Southeast:
                        _camera.Position.X += increment;
                        _camera.Position.Y += increment;
                        break;
                    case MouseDirection.South:
                        _camera.Position.Y += increment;
                        break;
                    case MouseDirection.Southwest:
                        _camera.Position.X -= increment;
                        _camera.Position.Y += increment;
                        break;
                    case MouseDirection.West:
                        _camera.Position.X -= increment;
                        break;
                    case MouseDirection.Northwest:
                        _camera.Position.X -= increment;
                        _camera.Position.Y -= increment;
                        break;
                }
            }

            if (mouse.ScrollWheelValue != prevMouseState.ScrollWheelValue)
            {
                _camera.Zoom += (mouse.ScrollWheelValue - prevMouseState.ScrollWheelValue) / WHEEL_DELTA;
            }
            
            if (_gfxDevice.Viewport.Bounds.Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y))) {
                UpdateMouseSelection();
                if (Mouse.GetState().LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released) {
                    if (Selected != null) {
                        Console.WriteLine($"$Modifying! {Selected}");
                        if (ActiveTool != null) {
                            // ActiveTool.Action(Selected);
                        }
                    }
                }
            }
        }

        if (processKeyboard)
        {
            var keyboard = Keyboard.GetState();

            foreach (var key in keyboard.GetPressedKeys())
            {
                switch (key)
                {
                    case Keys.Escape:
                        _camera.Zoom = 1;
                        break;
                    case Keys.A:
                        _camera.Position.X -= 10;
                        _camera.Position.Y += 10;
                        break;
                    case Keys.D:
                        _camera.Position.X += 10;
                        _camera.Position.Y -= 10;
                        break;
                    case Keys.W:
                        _camera.Position.X -= 10;
                        _camera.Position.Y -= 10;
                        break;
                    case Keys.S:
                        _camera.Position.X += 10;
                        _camera.Position.Y += 10;
                        break;
                    // case Keys.Z:
                    //     _camera.Zoom = 0.1f;
                    //     break;
                    // case Keys.X:
                    //     _camera.Zoom -= 0.1f;
                    //     break;

                }
            }
        }
        // Client.SetPos((ushort)(_camera.Position.X / TILE_SIZE), (ushort)(_camera.Position.Y / TILE_SIZE));

        CalculateViewRange(_camera, out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
        List<BlockCoords> requested = new List<BlockCoords>();
        for (var x = minTileX / 8; x < maxTileX / 8 + 1; x++) {
            for (var y = minTileY / 8; y < maxTileY / 8 + 1; y++) {
                requested.Add(new BlockCoords((ushort)x, (ushort)y));
            }
        }

        if (Client.Running) {
            Client.ResizeCache(requested.Count * 4);
            Client.LoadBlocks(requested);
        }

        _camera.Update();

        _lightSourceCamera.Position = _camera.Position;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightSourceCamera.Update();
        
        prevMouseState = Mouse.GetState();
    }

    public Object? Selected;
    
    private void UpdateMouseSelection() {
        var mouse = Mouse.GetState();
        Color[] pixels = new Color[1];
        _selectionTarget.GetData(0, new Rectangle(mouse.X, mouse.Y, 1, 1), pixels, 0, 1);
        var pixel = pixels[0];
        var selectedIndex = pixel.R | (pixel.G << 8) | (pixel.B << 16) | (pixel.A << 24);
        if (selectedIndex < 1 || selectedIndex > LandTiles.Count + StaticTiles.Count) 
            Selected = null;
        else if(selectedIndex > LandTiles.Count)
            Selected = StaticTiles[selectedIndex - 1 - LandTiles.Count];
        else {
            Selected = LandTiles[selectedIndex - 1];
        }
    }

    private void CalculateViewRange(Camera camera, out int minTileX, out int minTileY, out int maxTileX, out int maxTileY)
    {
        float zoom = camera.Zoom;

        int screenWidth = camera.ScreenSize.Width;
        int screenHeight = camera.ScreenSize.Height;

        /* Calculate the size of the drawing diamond in pixels */
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom / 2f;

        Vector3 center = camera.Position;
 
        // Render a few extra rows at the top to deal with things at lower z
        minTileX = Math.Max(0, (int)Math.Ceiling((center.X - screenDiamondDiagonal) / TILE_SIZE) - 8);
        minTileY = Math.Max(0, (int)Math.Ceiling((center.Y - screenDiamondDiagonal) / TILE_SIZE) - 8);

        // Render a few extra rows at the bottom to deal with things at higher z
        maxTileX = Math.Min(Client.Width * 8 - 1, (int)Math.Ceiling((center.X + screenDiamondDiagonal) / TILE_SIZE) + 8);
        maxTileY = Math.Min(Client.Height * 8 - 1, (int)Math.Ceiling((center.Y + screenDiamondDiagonal) / TILE_SIZE) + 8);
    }

    private static (Vector2, Vector2)[] _offsets = new[]
    {
        (new Vector2(1, 0), new Vector2(0, 1)),
        (new Vector2(0, 1), new Vector2(-1, 0)),
        (new Vector2(-1, 0), new Vector2(0, -1)),
        (new Vector2(0, -1), new Vector2(1, 0))
    };

    public Vector3 ComputeNormal(int tileX, int tileY)
    {
        var t = Client.GetLandTile(Math.Clamp(tileX, 0, Client.Width * 8 - 1), Math.Clamp(tileY, 0, Client.Height * 8 - 1));

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _offsets.Length; i++)
        {
            (var tu, var tv) = _offsets[i];

            var tx = Client.GetLandTile(Math.Clamp((int)(tileX + tu.X), 0, Client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, Client.Height * 8 - 1));
            var ty = Client.GetLandTile(Math.Clamp((int)(tileX + tv.X), 0, Client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, Client.Height * 8 - 1));

            if (tx.Id == 0 || ty.Id == 0)
                continue;

            Vector3 u = new Vector3(tu.X * TILE_SIZE, tu.Y * TILE_SIZE, tx.Z - t.Z);
            Vector3 v = new Vector3(tv.X * TILE_SIZE, tv.Y * TILE_SIZE, ty.Z - t.Z);

            var tmp = Vector3.Cross(u, v);
            normal = Vector3.Add(normal, tmp);
        }

        return Vector3.Normalize(normal);
    }

    public Vector4 GetCornerZ(int x, int y)
    {
        var top = Client.GetLandTile(x, y);
        var right = Client.GetLandTile(Math.Min(Client.Width * 8 - 1, x + 1), y);
        var left = Client.GetLandTile(x, Math.Min(Client.Height * 8 - 1, y + 1));
        var bottom = Client.GetLandTile(Math.Min(Client.Width * 8 - 1, x + 1), Math.Min(Client.Height * 8 - 1, y + 1));

        return new Vector4(
            top.Z * TILE_Z_SCALE,
            right.Z * TILE_Z_SCALE,
            left.Z * TILE_Z_SCALE,
            bottom.Z * TILE_Z_SCALE
        );
    }

    private bool IsRock(ushort id)
    {
        switch (id)
        {
            case 4945:
            case 4948:
            case 4950:
            case 4953:
            case 4955:
            case 4958:
            case 4959:
            case 4960:
            case 4962:
                return true;

            default:
                return id >= 6001 && id <= 6012;
        }
    }

    private bool IsTree(ushort id)
    {
        switch (id)
        {
            case 3274:
            case 3275:
            case 3276:
            case 3277:
            case 3280:
            case 3283:
            case 3286:
            case 3288:
            case 3290:
            case 3293:
            case 3296:
            case 3299:
            case 3302:
            case 3394:
            case 3395:
            case 3417:
            case 3440:
            case 3461:
            case 3476:
            case 3480:
            case 3484:
            case 3488:
            case 3492:
            case 3496:
            case 3230:
            case 3240:
            case 3242:
            case 3243:
            case 3273:
            case 3320:
            case 3323:
            case 3326:
            case 3329:
            case 4792:
            case 4793:
            case 4794:
            case 4795:
            case 12596:
            case 12593:
            case 3221:
            case 3222:
            case 12602:
            case 12599:
            case 3238:
            case 3225:
            case 3229:
            case 12881:
            case 3228:
            case 3227:
            case 39290:
            case 39280:
            case 39219:
            case 39215:
            case 39223:
            case 39288:
            case 39217:
            case 39225:
            case 39284:
            case 46822:
            case 14492:
                return true;
        }

        return false;
    }

    private void DrawShadowMap(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--) {
                var i = 0;
                foreach (var s in Client.GetStaticTiles(x,y)) {

                    ref var data = ref TileDataLoader.Instance.StaticData[s.Id];

                    if (!IsRock(s.Id) && !IsTree(s.Id) && !data.Flags.HasFlag(TileFlag.Foliage))
                        continue;

                    DrawStatic(s, Vector3.Zero);
                }
            }
        }

        DrawLand(minTileX, minTileY, maxTileX, maxTileY);
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
            case 0x63D3:
                return false;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
                return ((data.Flags & TileFlag.Background) == 0 &&
                        (data.Flags & TileFlag.Surface) == 0
            // && (data.Flags & TileFlag.NoDraw) == 0
                        );

            case 0x2198:
            case 0x2199:
            case 0x21A0:
            case 0x21A1:
            case 0x21A2:
            case 0x21A3:
            case 0x21A4:
                return false;
        }

        return true;
    }

    private enum HueMode
    {
        NONE = 0,
        HUED = 1,
        PARTIAL = 2
    }
    
    private Vector3 GetHueVector(StaticTile s) {
        var hue = s.Hue;
        var partial = TileDataLoader.Instance.StaticData[s.Id].IsPartialHue;
        HueMode mode;
        
        if ((s.Hue & 0x8000) != 0)
        {
            partial = true;
            hue &= 0x7FFF;
        }

        if (hue == 0)
        {
            partial = false;
        }
        
        if (hue != 0) {
            mode = partial ? HueMode.PARTIAL : HueMode.HUED;
        }
        else
        {
            mode = HueMode.NONE;
        }

        return new Vector3(hue, (int)mode, 0);
    }

    private void DrawStatic(StaticTile s, Vector3 hueOverride)
    {
        if (!CanDrawStatic(s.Id))
            return;

        ref var data = ref TileDataLoader.Instance.StaticData[s.Id];

        var texture = ArtLoader.Instance.GetStaticTexture(s.Id, out var bounds);

        bool cylindrical = data.Flags.HasFlag(TileFlag.Foliage) || IsRock(s.Id) || IsTree(s.Id);

        var hueVec = GetHueVector(s);
        if (s.Equals(Selected)) {
            hueVec.X = 0;
            hueVec.Y = 1;
        }
        if (hueOverride != Vector3.Zero)
            hueVec = hueOverride;

        _mapRenderer.DrawBillboard(
            new Vector3(s.X * TILE_SIZE, s.Y * TILE_SIZE, s.Z * TILE_Z_SCALE),
            s.CellIndex * DEPTH_OFFSET,
            texture,
            bounds,
            hueVec,
            cylindrical
        );
    }
    
    private void DrawStaticObject(StaticObject s, Vector3 hueOverride)
    {
        if (!CanDrawStatic(s.root.Id))
            return;
        
        var landTile = Client.GetLandTile(s.root.X, s.root.Y);
        if (!ShouldRender(s.root.Z) || (ShouldRender(landTile.Z) && landTile.Z > s.root.Z + 5))
            return;


        var hueVec = GetHueVector(s.root);
        if (Selected == s) {
            hueVec.X = 0;
            hueVec.Y = 1;
        }
        if (hueOverride != Vector3.Zero)
            hueVec = hueOverride;

        _mapRenderer.DrawStaticObject(s, hueVec);
    }


    private bool ShouldRender(short Z) {
        return Z >= MIN_Z && Z <= MAX_Z;
    }
    private void DrawLand(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--)
            {
                var tile = Client.GetLandTile(x, y);
                if (tile.Id > TileDataLoader.Instance.LandData.Length) continue;
                if(tile.Z < MIN_Z || tile.Z > MAX_Z) continue;


                Texture2D tileTex = null;
                Rectangle bounds = new Rectangle();
                try {
                    tileTex = TexmapsLoader.Instance.GetLandTexture(tile.Id, out bounds);
                } catch (Exception e) {}
                var diamondTexture = false;
                if (tileTex == null) {
                    tileTex = ArtLoader.Instance.GetLandTexture(tile.Id, out bounds);
                    diamondTexture = true;
                }

                
                ref var data = ref TileDataLoader.Instance.LandData[tile.Id];

                if ((data.Flags & TileFlag.Wet) != 0)
                {
                    /* Water tiles are always flat */
                    _mapRenderer.DrawTile(
                        new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                        new Vector4(tile.Z * TILE_Z_SCALE),
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        tileTex,
                        bounds,
                        diamondTexture
                    );
                }
                else
                {
                    if (!_landRenderInfos.ContainsKey(tile)) {
                        FillRenderInfo(tile);
                    }

                    var lri = _landRenderInfos[tile];
                    _mapRenderer.DrawTile(
                        new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                        lri.CornerZ,
                        lri.NormalTop,
                        lri.NormalRight,
                        lri.NormalLeft,
                        lri.NormalBottom,
                        tileTex,
                        bounds,
                        diamondTexture
                    );
                }


            }
        }
    }

    private void FillRenderInfo(LandTile tile) {
        _landRenderInfos[tile] = new LandRenderInfo {
            CornerZ = GetCornerZ(tile.X, tile.Y),
            NormalTop = ComputeNormal(tile.X, tile.Y),
            NormalRight = ComputeNormal(tile.X + 1, tile.Y),
            NormalLeft = ComputeNormal(tile.X, tile.Y + 1),
            NormalBottom = ComputeNormal(tile.X + 1, tile.Y + 1),
        };
    }

    public void Draw() {
        _gfxDevice.Viewport = new Viewport(0, 0, _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight);

        CalculateViewRange(_camera, out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
        
        _mapEffect.WorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.LightSource.Enabled = false;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["ShadowMap"];

        _mapRenderer.Begin(_shadowTarget, _mapEffect, _lightSourceCamera, RasterizerState.CullNone,
            SamplerState.PointClamp, _depthStencilState, BlendState.AlphaBlend, null, true);
        if (IsDrawShadows) {
            DrawShadowMap(minTileX, minTileY, maxTileX, maxTileY);
        }
        _mapRenderer.End();
        
        _mapEffect.WorldViewProj = _camera.WorldViewProj;
        
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Selection"];
        _mapRenderer.Begin(_selectionTarget, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp, 
            _depthStencilState, BlendState.AlphaBlend, null, true);
        //0 is no tile in selection buffer
        for (var i = 1; i <= StaticTiles.Count; i++) { 
            var color = new Color(i & 0xFF, (i >> 8) & 0xFF, (i >> 16) & 0xFF, (i >> 24) & 0xFF);
            DrawStatic(StaticTiles[i - 1], color.ToVector3());
        }
        _mapRenderer.End();
        
        _mapEffect.LightWorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.AmbientLightColor = _lightingState.AmbientLightColor;
        _mapEffect.LightSource.Direction = _lightingState.LightDirection;
        _mapEffect.LightSource.DiffuseColor = _lightingState.LightDiffuseColor;
        _mapEffect.LightSource.SpecularColor = _lightingState.LightSpecularColor;
        _mapEffect.LightSource.Enabled = true;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, _shadowTarget, true);
        if (IsDrawStatic) {
            foreach (var tile in StaticTiles) {
                DrawStatic(tile, Vector3.Zero);
            }
        }
        _mapRenderer.End();
        
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, _shadowTarget, false);
        if (IsDrawLand) {
            DrawLand(minTileX, minTileY, maxTileX, maxTileY);
        }
        _mapRenderer.End();
    }

    public void DrawHighRes() {
        Console.WriteLine("HIGH RES!");
        var myRenderTarget = new RenderTarget2D(_gfxDevice, 15360, 8640, false, SurfaceFormat.Color, DepthFormat.Depth24);
        
        var myCamera = new Camera();
        myCamera.Position = _camera.Position;
        myCamera.Zoom = _camera.Zoom;
        myCamera.ScreenSize = myRenderTarget.Bounds;
        myCamera.Update();
        
        CalculateViewRange(myCamera, out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
        List<BlockCoords> requested = new List<BlockCoords>();
        for (var x = minTileX / 8; x < maxTileX / 8 + 1; x++) {
            for (var y = minTileY / 8; y < maxTileY / 8 + 1; y++) {
                requested.Add(new BlockCoords((ushort)x, (ushort)y));
            }
        }
        Client.ResizeCache(requested.Count * 4);
        Client.LoadBlocks(requested);
        
        _mapEffect.WorldViewProj = myCamera.WorldViewProj;
        _mapEffect.LightSource.Enabled = false;
        
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];
        _mapRenderer.Begin(myRenderTarget, _mapEffect, myCamera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, null, true);
        if (IsDrawStatic) {
            foreach (var tile in StaticTiles) {
                DrawStatic(tile, Vector3.Zero);
            }
        }
        _mapRenderer.End();
        
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];
        _mapRenderer.Begin(myRenderTarget, _mapEffect, myCamera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, null, false);
        if (IsDrawLand) {
            DrawLand(minTileX, minTileY, maxTileX, maxTileY);
        }
        _mapRenderer.End();

        using var fs = new FileStream(@"C:\git\CentrEDSharp\render.jpg", FileMode.OpenOrCreate);
        myRenderTarget.SaveAsJpeg(fs, myRenderTarget.Width, myRenderTarget.Height);
        Console.WriteLine("HIGH RES DONE!");
    }

    public void OnWindowsResized(GameWindow window) {
        _camera.ScreenSize = window.ClientBounds;
        _camera.Update();
        
        _shadowTarget = new RenderTarget2D(
            _gfxDevice,
            _gfxDevice.PresentationParameters.BackBufferWidth * 2,
            _gfxDevice.PresentationParameters.BackBufferHeight * 2,
            false,
            SurfaceFormat.Single,
            DepthFormat.Depth24);
        
        _selectionTarget = new RenderTarget2D(
            _gfxDevice,
            _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24);
    }
}