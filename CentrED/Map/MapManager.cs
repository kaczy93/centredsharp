using CentrED.Client;
using CentrED.Network;
using CentrED.Renderer;
using CentrED.Renderer.Effects;
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

    private readonly RenderTarget2D _shadowTarget;

    private readonly PostProcessRenderer _postProcessRenderer;

    private CentrEDClient _client;

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

    public MapManager(GraphicsDevice gd, CentrEDClient client)
    {
        _gfxDevice = gd;

        _mapEffect = new MapEffect(gd);
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];

        _mapRenderer = new MapRenderer(gd);

        _shadowTarget = new RenderTarget2D(
                                gd,
                                gd.PresentationParameters.BackBufferWidth * 2,
                                gd.PresentationParameters.BackBufferHeight * 2,
                                false,
                                SurfaceFormat.Single,
                                DepthFormat.Depth24);

        _postProcessRenderer = new PostProcessRenderer(gd);

        _client = client;

        var focus = _client.GetLandTile(1455, 1900);

        _camera.LookAt = new Vector3(focus.X * TILE_SIZE, focus.Y * TILE_SIZE, focus.Z * TILE_Z_SCALE);
        _camera.ScreenSize.X = 0;
        _camera.ScreenSize.Y = 0;
        _camera.ScreenSize.Width = gd.PresentationParameters.BackBufferWidth;
        _camera.ScreenSize.Height = gd.PresentationParameters.BackBufferHeight;

        // This has to match the LightDirection below
        _lightSourceCamera.LookAt = _camera.LookAt;
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
                        _camera.LookAt.Y -= increment;
                        break;
                    case MouseDirection.Northeast:
                        _camera.LookAt.Y -= increment;
                        _camera.LookAt.X += increment;
                        break;
                    case MouseDirection.East:
                        _camera.LookAt.X += increment;
                        break;
                    case MouseDirection.Southeast:
                        _camera.LookAt.X += increment;
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.South:
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.Southwest:
                        _camera.LookAt.X -= increment;
                        _camera.LookAt.Y += increment;
                        break;
                    case MouseDirection.West:
                        _camera.LookAt.X -= increment;
                        break;
                    case MouseDirection.Northwest:
                        _camera.LookAt.X -= increment;
                        _camera.LookAt.Y -= increment;
                        break;
                }
            }

            if (mouse.ScrollWheelValue != _lastScrollWheel)
            {
                _camera.Zoom += (mouse.ScrollWheelValue - _lastScrollWheel) / WHEEL_DELTA;
                _lastScrollWheel = mouse.ScrollWheelValue;
            }
        }

        if (processKeyboard)
        {
            var keyboard = Keyboard.GetState();

            foreach (var key in keyboard.GetPressedKeys())
            {
                switch (key)
                {
                    case Keys.E:
                        _camera.Rotation += 1;
                        break;
                    case Keys.Q:
                        _camera.Rotation -= 1;
                        break;
                    case Keys.Escape:
                        _camera.Rotation = 0;
                        _camera.Zoom = 1;
                        break;
                    case Keys.A:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y += 10;
                        break;
                    case Keys.D:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y -= 10;
                        break;
                    case Keys.W:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y -= 10;
                        break;
                    case Keys.S:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y += 10;
                        break;
                    case Keys.Z:
                        _camera.Zoom = 0.1f;
                        break;
                    case Keys.X:
                        _camera.Zoom -= 0.1f;
                        if (_camera.Zoom < 0.5f)
                            _camera.Zoom = 0.5f;
                        break;

                }
            }
            
            CalculateViewRange(out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
            _client.ResizeCache((maxTileX - minTileX) * (maxTileY - minTileY) / 24);
            List<BlockCoords> requested = new List<BlockCoords>();
            for (var x = minTileX / 8; x < maxTileX / 8 + 1; x++) {
                for (var y = minTileY / 8; y < maxTileY / 8 + 1; y++) {
                    requested.Add(new BlockCoords((ushort)x, (ushort)y));
                }
            }

            _client.LoadBlocks(requested);
        }

        _camera.Update();

        _lightSourceCamera.LookAt = _camera.LookAt;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightSourceCamera.Update();
    }

    private void CalculateViewRange(out int minTileX, out int minTileY, out int maxTileX, out int maxTileY)
    {
        float zoom = _camera.Zoom;

        int screenWidth = _camera.ScreenSize.Width;
        int screenHeight = _camera.ScreenSize.Height;

        /* Calculate the size of the drawing diamond in pixels */
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom / 2f;

        Vector3 center = _camera.LookAt;

        minTileX = Math.Max(0, (int)Math.Ceiling((center.X - screenDiamondDiagonal) / TILE_SIZE));
        minTileY = Math.Max(0, (int)Math.Ceiling((center.Y - screenDiamondDiagonal) / TILE_SIZE));

        // Render a few extra rows at the bottom to deal with things at higher z
        maxTileX = Math.Min(_client.Width * 8 - 1, (int)Math.Ceiling((center.X + screenDiamondDiagonal) / TILE_SIZE) + 4);
        maxTileY = Math.Min(_client.Height * 8 - 1, (int)Math.Ceiling((center.Y + screenDiamondDiagonal) / TILE_SIZE) + 4);
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
        var t = _client.GetLandTile(Math.Clamp(tileX, 0, _client.Width * 8 - 1), Math.Clamp(tileY, 0, _client.Height * 8 - 1));

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _offsets.Length; i++)
        {
            (var tu, var tv) = _offsets[i];

            var tx = _client.GetLandTile(Math.Clamp((int)(tileX + tu.X), 0, _client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, _client.Height * 8 - 1));
            var ty = _client.GetLandTile(Math.Clamp((int)(tileX + tv.X), 0, _client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, _client.Height * 8 - 1));

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
        var top = _client.GetLandTile(x, y);
        var right = _client.GetLandTile(Math.Min(_client.Width * 8 - 1, x + 1), y);
        var left = _client.GetLandTile(x, Math.Min(_client.Height * 8 - 1, y + 1));
        var bottom = _client.GetLandTile(Math.Min(_client.Width * 8 - 1, x + 1), Math.Min(_client.Height * 8 - 1, y + 1));

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
                foreach (var s in _client.GetStaticTiles(x,y)) {

                    ref var data = ref TileDataLoader.Instance.StaticData[s.Id];

                    if (!IsRock(s.Id) && !IsTree(s.Id) && !data.Flags.HasFlag(TileFlag.Foliage))
                        continue;

                    DrawStatic(s, x, y, i++ * DEPTH_OFFSET);
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

    private void DrawStatic(StaticTile s, int x, int y, float depthOffset)
    {
        if (!CanDrawStatic(s.Id))
            return;

        ref var data = ref TileDataLoader.Instance.StaticData[s.Id];

        var texture = ArtLoader.Instance.GetStaticTexture(s.Id, out var bounds);
        var isLand = texture.Width == 44 && texture.Height == 44;

        bool cylindrical = data.Flags.HasFlag(TileFlag.Foliage) || IsRock(s.Id) || IsTree(s.Id);

        _mapRenderer.DrawBillboard(
            new Vector3(x * TILE_SIZE, y * TILE_SIZE, s.Z * TILE_Z_SCALE),
            depthOffset,
            texture,
            bounds,
            cylindrical
        );
    }

    private bool ShouldRender(short Z) {
        return Z >= MIN_Z && Z <= MAX_Z;
    }

    public void DrawStatics(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--) {

                var i = 0;
                var landTile = _client.GetLandTile(x, y);
                foreach (var s in _client.GetStaticTiles(x, y).Reverse()) {
                    if(!ShouldRender(s.Z) || (ShouldRender(landTile.Z) && landTile.Z > s.Z + 5)) continue;
                    DrawStatic(s, x, y, i++ * DEPTH_OFFSET);
                }
            }
        }
    }

    private void DrawLand(int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (int y = maxTileY; y >= minTileY; y--)
        {
            for (int x = maxTileX; x >= minTileX; x--)
            {
                var tile = _client.GetLandTile(x, y);
                if(tile.Z < MIN_Z || tile.Z > MAX_Z) continue;

                var tileTex = TexmapsLoader.Instance.GetLandTexture(tile.Id, out var bounds);
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
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.Viewport = new Viewport(0, 0, _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight);

        CalculateViewRange(out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
        


        _mapEffect.WorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.LightSource.Enabled = false;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["ShadowMap"];

        _mapRenderer.Begin(_shadowTarget, _mapEffect, _lightSourceCamera, RasterizerState.CullNone,
            SamplerState.PointClamp, _depthStencilState, BlendState.AlphaBlend, null);
        if (IsDrawShadows) {
            DrawShadowMap(minTileX, minTileY, maxTileX, maxTileY);
        }

        _mapRenderer.End();


        _mapEffect.WorldViewProj = _camera.WorldViewProj;
        _mapEffect.LightWorldViewProj = _lightSourceCamera.WorldViewProj;
        _mapEffect.AmbientLightColor = _lightingState.AmbientLightColor;
        _mapEffect.LightSource.Direction = _lightingState.LightDirection;
        _mapEffect.LightSource.DiffuseColor = _lightingState.LightDiffuseColor;
        _mapEffect.LightSource.SpecularColor = _lightingState.LightSpecularColor;
        _mapEffect.LightSource.Enabled = true;
        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Statics"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, _shadowTarget);
        if (IsDrawStatic) {
            DrawStatics(minTileX, minTileY, maxTileX, maxTileY);
        }

        _mapRenderer.End();


        _mapEffect.CurrentTechnique = _mapEffect.Techniques["Terrain"];

        _mapRenderer.Begin(null, _mapEffect, _camera, RasterizerState.CullNone, SamplerState.PointClamp,
            _depthStencilState, BlendState.AlphaBlend, _shadowTarget);
        if (IsDrawLand) {
            DrawLand(minTileX, minTileY, maxTileX, maxTileY);
        }

        _mapRenderer.End();
    }
}