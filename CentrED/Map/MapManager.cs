using CentrED.Client;
using CentrED.Network;
using CentrED.Renderer;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDL2;
using UORenderer;

namespace CentrED.Map;

public class MapManager {
    private readonly GraphicsDevice _gfxDevice;

    private readonly MapRenderer _mapRenderer;

    private readonly ShadowRenderer _shadowRenderer;
    private readonly RenderTarget2D _shadowTarget;

    private readonly PostProcessRenderer _postProcessRenderer;

    // Currently loaded map
    private CentrEDClient _client;

    private Camera _camera = new Camera();
    public Camera Camera => _camera;
    private Camera _lightSourceCamera = new Camera();

    private LightingState _lightingState = new LightingState();
    private DepthStencilState _depthStencilState = new DepthStencilState();

    public static readonly float TILE_SIZE = 22f;
    public static readonly float TILE_Z_SCALE = 4f;

    private void DarkenTexture(ushort[] pixels) {
        for (int i = 0; i < pixels.Length; i++) {
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

    public MapManager(GraphicsDevice gd, CentrEDClient client) {
        _gfxDevice = gd;

        _mapRenderer = new MapRenderer(gd, null);

        _shadowTarget = new RenderTarget2D(
            gd,
            gd.PresentationParameters.BackBufferWidth * 2,
            gd.PresentationParameters.BackBufferHeight * 2,
            false,
            SurfaceFormat.Single,
            DepthFormat.Depth24);
        _shadowRenderer = new ShadowRenderer(gd, _shadowTarget);

        _postProcessRenderer = new PostProcessRenderer(gd);

        _client = client;
        
        _camera.LookAt = new Vector3(1455 * TILE_SIZE, 1900 * TILE_SIZE, 0);
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

    public void Dispose() {
        if(_client != null)
            _client.Dispose();
    }

    private enum MouseDirection {
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
    private MouseDirection ProcessMouseMovement(ref MouseState mouseState, out float distance) {
        Vector2 vec = new Vector2(mouseState.X - (_camera.ScreenSize.Width / 2),
            mouseState.Y - (_camera.ScreenSize.Height / 2));

        int hashf = 100 * (Math.Sign(vec.X) + 2) + 10 * (Math.Sign(vec.Y) + 2);

        distance = vec.Length();
        if (distance == 0) {
            return MouseDirection.North;
        }

        vec.X = Math.Abs(vec.X);
        vec.Y = Math.Abs(vec.Y);

        if (vec.Y * 5 <= vec.X * 2) {
            hashf = hashf + 1;
        }
        else if (vec.Y * 2 >= vec.X * 5) {
            hashf = hashf + 3;
        }
        else {
            hashf = hashf + 2;
        }

        switch (hashf) {
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
    private bool _moved = true;

    public void Update(GameTime gameTime, bool processMouse, bool processKeyboard) {
        if (processMouse) {
            var mouse = Mouse.GetState();
        
            if (mouse.RightButton == ButtonState.Pressed) {
                var direction = ProcessMouseMovement(ref mouse, out var distance);
                if (distance > 1) {
                    int increment = distance > 200 ? 10 : 5;
                    switch (direction) {
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
                    _moved = true;
                }
            }
        
            if (mouse.ScrollWheelValue != _lastScrollWheel) {
                _camera.Zoom += (mouse.ScrollWheelValue - _lastScrollWheel) / WHEEL_DELTA;
                _lastScrollWheel = mouse.ScrollWheelValue;
                _moved = true;
            }
        }
        
        if (processKeyboard) {
            var keyboard = Keyboard.GetState();
        
            foreach (var key in keyboard.GetPressedKeys()) {
                switch (key) {
                    case Keys.E:
                        _camera.Rotation += 1;
                        _moved = true;
                        break;
                    case Keys.Q:
                        _camera.Rotation -= 1;
                        _moved = true;
                        break;
                    case Keys.Escape:
                        _camera.Rotation = 0;
                        _camera.Zoom = 1;
                        _moved = true;
                        break;
                    case Keys.A:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y += 10;
                        _moved = true;
                        break;
                    case Keys.D:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y -= 10;
                        _moved = true;
                        break;
                    case Keys.W:
                        _camera.LookAt.X -= 10;
                        _camera.LookAt.Y -= 10;
                        _moved = true;
                        break;
                    case Keys.S:
                        _camera.LookAt.X += 10;
                        _camera.LookAt.Y += 10;
                        _moved = true;
                        break;
                    case Keys.Z:
                        _camera.Zoom += 0.1f;
                        _moved = true;
                        break;
                    case Keys.X:
                        _camera.Zoom -= 0.1f;
                        if (_camera.Zoom < 0.5f)
                            _camera.Zoom = 0.5f;
                        _moved = true;
                        break;
                }
            }
        }

        var t = _client.GetLandTile((int)(_camera.LookAt.X / TILE_SIZE), (int)(_camera.LookAt.Y / TILE_SIZE));
        _camera.LookAt.Z = t.Z;

        _camera.Update();

        _lightSourceCamera.LookAt = _camera.LookAt;
        _lightSourceCamera.Zoom = _camera.Zoom;
        _lightSourceCamera.Rotation = 45;
        _lightSourceCamera.ScreenSize.Width = _camera.ScreenSize.Width * 2;
        _lightSourceCamera.ScreenSize.Height = _camera.ScreenSize.Height * 2;

        _lightSourceCamera.Update();
        
        if(_moved) {
            CalculateViewRange(out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);
            _client.ResizeCache((maxTileX - minTileX) * (maxTileY - minTileY) / 24);
            List<BlockCoords> requested = new List<BlockCoords>();
            for (var x = minTileX / 8; x < maxTileX / 8 + 1; x++) {
                for (var y = minTileY / 8; y < maxTileY / 8 + 1; y++) {
                    requested.Add(new BlockCoords((ushort)x, (ushort)y));
                }
            }

            _client.LoadBlocks(requested);
            _moved = false;
        }
    }

    private void CalculateViewRange(out int minTileX, out int minTileY, out int maxTileX, out int maxTileY) {
        float zoom = _camera.Zoom;

        int screenWidth = _camera.ScreenSize.Width;
        int screenHeight = _camera.ScreenSize.Height;

        /* Calculate the size of the drawing diamond in pixels */
        float screenDiamondDiagonal = (screenWidth + screenHeight) / zoom;

        Vector3 center = _camera.LookAt;

        minTileX = (int)Math.Ceiling((center.X - screenDiamondDiagonal) / TILE_SIZE);
        minTileY = (int)Math.Ceiling((center.Y - screenDiamondDiagonal) / TILE_SIZE);

        // Render a few extra rows at the bottom to deal with things at higher z
        maxTileX = (int)Math.Ceiling((center.X + screenDiamondDiagonal) / TILE_SIZE) + 4;
        maxTileY = (int)Math.Ceiling((center.Y + screenDiamondDiagonal) / TILE_SIZE) + 4;
    }

    private static (Vector2, Vector2)[] _offsets = new[] {
        (new Vector2(1, 0), new Vector2(0, 1)),
        (new Vector2(0, 1), new Vector2(-1, 0)),
        (new Vector2(-1, 0), new Vector2(0, -1)),
        (new Vector2(0, -1), new Vector2(1, 0))
    };

    public Vector3 ComputeNormal(int tileX, int tileY) {
        var t = _client.GetLandTile(tileX, tileY);

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _offsets.Length; i++) {
            (var tu, var tv) = _offsets[i];

            var tx = _client.GetLandTile((int)(tileX + tu.X), (int)(tileY + tu.Y));
            var ty = _client.GetLandTile((int)(tileX + tv.X), (int)(tileY + tv.Y));

            if (tx.Id == 0 || ty.Id == 0)
                continue;

            Vector3 u = new Vector3(tu.X * TILE_SIZE, tu.Y * TILE_SIZE, tx.Z - t.Z);
            Vector3 v = new Vector3(tv.X * TILE_SIZE, tv.Y * TILE_SIZE, ty.Z - t.Z);

            var tmp = Vector3.Cross(u, v);
            normal = Vector3.Add(normal, tmp);
        }

        return Vector3.Normalize(normal);
    }

    public Vector4 GetCornerZ(int x, int y) {
        var top = _client.GetLandTile(x, y);
        var right = _client.GetLandTile(x + 1, y);
        var left = _client.GetLandTile(x, y + 1);
        var bottom = _client.GetLandTile(x + 1, y + 1);

        return new Vector4(
            top.Z * TILE_Z_SCALE,
            right.Z * TILE_Z_SCALE,
            left.Z * TILE_Z_SCALE,
            bottom.Z * TILE_Z_SCALE
        );
    }

    private void DrawShadowMap(int minTileX, int minTileY, int maxTileX, int maxTileY) {
        var depthStatics = new DepthStencilState() {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            DepthBufferFunction = CompareFunction.Less
        };

        _shadowRenderer.Begin(_lightSourceCamera, RasterizerState.CullNone, SamplerState.PointClamp, depthStatics,
            BlendState.AlphaBlend);

        for (int y = maxTileY; y >= minTileY; y--) {
            for (int x = maxTileX; x >= minTileX; x--) {
                foreach (var staticTile in _client.GetStaticTiles(x, y).Reverse()) {
                    
                    var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
                    var isLand = texture.Width == 44 && texture.Height == 44;

                    if (isLand) {
                        _shadowRenderer.DrawTile(
                            new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                            new Vector4(staticTile.Z * TILE_Z_SCALE),
                            texture,
                            bounds,
                            true
                        );
                    }
                    else {
                        _shadowRenderer.DrawBillboard(
                            new Vector3(x * TILE_SIZE, y * TILE_SIZE, staticTile.Z * TILE_Z_SCALE),
                            texture,
                            bounds
                        );
                    }
                }
            }
        }

        for (int y = maxTileY; y >= minTileY; y--) {
            for (int x = maxTileX; x >= minTileX; x--) {
                var tile = _client.GetLandTile(x, y);

                var tileTex = ArtLoader.Instance.GetLandTexture(tile.Id, out var bounds);

                if (tileTex == null)
                    continue;

                ref var data = ref TileDataLoader.Instance.LandData[tile.Id];

                Vector4 zCorners;
                if ((data.Flags & TileFlag.Wet) != 0) {
                    /* Water tiles are always flat */
                    zCorners = new Vector4(tile.Z * TILE_Z_SCALE);
                }
                else {
                    zCorners = GetCornerZ(x, y);
                }

                _shadowRenderer.DrawTile(
                    new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                    zCorners,
                    tileTex,
                    bounds,
                    true
                );
            }
        }

        _shadowRenderer.End();
    }

    public void DrawStatics(int minTileX, int minTileY, int maxTileX, int maxTileY) {
        var depthStatics = new DepthStencilState() {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            DepthBufferFunction = CompareFunction.Less
        };

        _mapRenderer.Begin(_camera, RasterizerState.CullNone, SamplerState.PointClamp, _lightingState, depthStatics,
            BlendState.AlphaBlend, _shadowTarget, _lightSourceCamera);

        for (int y = maxTileY; y >= minTileY; y--) {
            for (int x = maxTileX; x >= minTileX; x--) {
                foreach (var staticTile in _client.GetStaticTiles(x, y).Reverse()) {
                    var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
                    var isLand = texture.Width == 44 && texture.Height == 44;

                    if (isLand) {
                        _mapRenderer.DrawTile(
                            new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                            new Vector4(staticTile.Z * TILE_Z_SCALE),
                            Vector3.UnitZ,
                            Vector3.UnitZ,
                            Vector3.UnitZ,
                            Vector3.UnitZ,
                            texture,
                            bounds,
                            true
                        );
                    }
                    else {
                        _mapRenderer.DrawBillboard(
                            new Vector3(x * TILE_SIZE, y * TILE_SIZE, staticTile.Z * TILE_Z_SCALE),
                            Vector3.UnitZ,
                            texture,
                            bounds
                        );
                    }
                }
            }
        }

        _mapRenderer.End();
    }

    private void DrawLand(int minTileX, int minTileY, int maxTileX, int maxTileY) {
        _depthStencilState.DepthBufferEnable = true;
        _depthStencilState.DepthBufferWriteEnable = false;
        _depthStencilState.DepthBufferFunction = CompareFunction.Less;

        _mapRenderer.Begin(_camera, RasterizerState.CullNone, SamplerState.PointClamp, _lightingState,
            _depthStencilState, BlendState.AlphaBlend, _shadowTarget, _lightSourceCamera);

        for (int y = maxTileY; y >= minTileY; y--) {
            for (int x = maxTileX; x >= minTileX; x--) {
                var tile = _client.GetLandTile(x, y);

                var tileTex = TexmapsLoader.Instance.GetLandTexture(tile.Id, out var bounds);
                var diamondTexture = false;
                if (tileTex == null) {
                    tileTex = ArtLoader.Instance.GetLandTexture(tile.Id, out bounds);
                    diamondTexture = true;
                }

                ref var data = ref TileDataLoader.Instance.LandData[tile.Id];

                if ((data.Flags & TileFlag.Wet) != 0) {
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
                else {
                    _mapRenderer.DrawTile(
                        new Vector2(x * TILE_SIZE, y * TILE_SIZE),
                        GetCornerZ(x, y),
                        // ComputeNormal(x, y),
                        // ComputeNormal(x + 1, y),
                        // ComputeNormal(x, y + 1),
                        // ComputeNormal(x + 1, y + 1),
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        Vector3.UnitZ,
                        tileTex,
                        bounds,
                        diamondTexture
                    );
                }
            }
        }

        _mapRenderer.End();
    }

    public void Draw() {
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.Viewport = new Viewport(0, 0, _gfxDevice.PresentationParameters.BackBufferWidth,
            _gfxDevice.PresentationParameters.BackBufferHeight);

        CalculateViewRange(out var minTileX, out var minTileY, out var maxTileX, out var maxTileY);

        // DrawShadowMap(minTileX, minTileY, maxTileX, maxTileY);

        DrawStatics(minTileX, minTileY, maxTileX, maxTileY);

        DrawLand(minTileX, minTileY, maxTileX, maxTileY);
    }
}