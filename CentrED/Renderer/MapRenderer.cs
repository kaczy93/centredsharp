using System.Runtime.CompilerServices;
using CentrED.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UORenderer;

namespace CentrED.Renderer;

public class LightingState
{
    public Vector3 LightDirection;
    public Vector3 LightDiffuseColor;
    public Vector3 LightSpecularColor;
    public Vector3 AmbientLightColor;
}

public class MapRenderer
{
    #region Draw Batcher
    private class DrawBatcher
    {
        private const int MAX_TILES_PER_BATCH = 4096;
        private const int MAX_VERTICES = MAX_TILES_PER_BATCH * 4;
        private const int MAX_INDICES = MAX_TILES_PER_BATCH * 6;

        private readonly GraphicsDevice _gfxDevice;
        private readonly RenderTarget2D _renderTarget;

        private readonly VertexBuffer _vertexBuffer;
        private readonly IndexBuffer _indexBuffer;

        private readonly VertexPositionNormalTexture[] _vertexInfo;
        private static readonly short[] _indexData = GenerateIndexArray();

        private MapEffect _effect;
        private Texture2D _texture;
        private Texture2D _shadowMap;
        private RasterizerState _rasterizerState;
        private SamplerState _samplerState;
        private LightingState _lightingState;
        private DepthStencilState _depthStencilState;
        private BlendState _blendState;

        private static short[] GenerateIndexArray()
        {
            short[] result = new short[MAX_INDICES];
            for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4)
            {
                result[i] = (short)(j);
                result[i + 1] = (short)(j + 1);
                result[i + 2] = (short)(j + 2);
                result[i + 3] = (short)(j + 3);
                result[i + 4] = (short)(j + 2);
                result[i + 5] = (short)(j + 1);
            }
            return result;
        }

        private float TILE_SIZE = 22f;

        private bool _beginCalled = false;
        private int _numTiles = 0;

        public DrawBatcher(GraphicsDevice device, RenderTarget2D target)
        {
            _gfxDevice = device;
            _renderTarget = target; _effect = new MapEffect(_gfxDevice);

            _vertexInfo = new VertexPositionNormalTexture[MAX_VERTICES];

            _vertexBuffer = new DynamicVertexBuffer(
                device,
                typeof(VertexPositionNormalTexture),
                MAX_VERTICES,
                BufferUsage.WriteOnly
            );

            _indexBuffer = new IndexBuffer(
                device,
                IndexElementSize.SixteenBits,
                MAX_INDICES,
                BufferUsage.WriteOnly
            );

            _indexBuffer.SetData(_indexData);
        }

        public void Begin(
            MapEffect effect,
            Texture2D texture,
            RasterizerState rasterizerState,
            SamplerState samplerState,
            LightingState lightingState,
            DepthStencilState depthStencilState,
            BlendState blendState,
            Texture2D shadowMap
        )
        {
            if (_beginCalled)
                throw new InvalidOperationException("Mismatched Begin and End calls");

            _beginCalled = true;
            _numTiles = 0;

            _effect = effect;
            _texture = texture;
            _rasterizerState = rasterizerState;
            _samplerState = samplerState;
            _lightingState = lightingState;
            _depthStencilState = depthStencilState;
            _blendState = blendState;
            _shadowMap = shadowMap;
        }

        private unsafe void Flush()
        {
            if (_numTiles == 0)
                return;

            _gfxDevice.SetRenderTarget(_renderTarget);

            fixed (VertexPositionNormalTexture* p = &_vertexInfo[0])
            {
                _vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, Unsafe.SizeOf<VertexPositionNormalTexture>() * _numTiles * 4, SetDataOptions.Discard);
            }

            _gfxDevice.SetVertexBuffer(_vertexBuffer);
            _gfxDevice.Indices = _indexBuffer;

            _gfxDevice.RasterizerState = _rasterizerState;
            _gfxDevice.SamplerStates[0] = _samplerState;
            _gfxDevice.Textures[0] = _texture;
            _gfxDevice.Textures[1] = _shadowMap;
            _gfxDevice.DepthStencilState = _depthStencilState;
            _gfxDevice.BlendState = _blendState;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _numTiles * 4, 0, _numTiles * 2);
            }

            _numTiles = 0;
        }

        public unsafe void End()
        {
            Flush();

            _beginCalled = false;
        }

        private static readonly float EPSILON = GetMachineEpsilonFloat();

        private static float GetMachineEpsilonFloat()
        {
            float machineEpsilon = 1.0f;
            float comparison;

            /* Keep halving the working value of machineEpsilon until we get a number that
             * when added to 1.0f will still evaluate as equal to 1.0f.
             */
            do
            {
                machineEpsilon *= 0.5f;
                comparison = 1.0f + machineEpsilon;
            }
            while (comparison > 1.0f);

            return machineEpsilon;
        }

        public void DrawTile(
            Vector2 tilePos,
            Vector4 cornerZ,
            Vector3 normal0,
            Vector3 normal1,
            Vector3 normal2,
            Vector3 normal3,
            Rectangle texCoords,
            bool diamondTex)
        {
            if ((_numTiles + 1) >= MAX_TILES_PER_BATCH)
                Flush();

            int cur = _numTiles * 4;
            var texture = _texture;

            float onePixel = Math.Max(1.0f / texture.Width, EPSILON);

            var texX = texCoords.X / (float)texture.Width + (onePixel / 2f);
            var texY = texCoords.Y / (float)texture.Height + (onePixel / 2f);
            var texWidth = (texCoords.Width / (float)texture.Width) - onePixel;
            var texHeight = (texCoords.Height / (float)texture.Height) - onePixel;

            var posX = tilePos.X - 1;
            var posY = tilePos.Y - 1;

            if (diamondTex)
            {
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX, posY, cornerZ.X),
                    normal0,
                    new Vector2(texX + (texWidth / 2f), texY));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX + TILE_SIZE, posY, cornerZ.Y),
                    normal1,
                    new Vector2(texX + texWidth, texY + (texHeight / 2f)));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX, posY + TILE_SIZE, cornerZ.Z),
                    normal2,
                    new Vector2(texX, texY + (texHeight / 2f)));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W),
                    normal3,
                    new Vector2(texX + (texWidth / 2f), texY + texHeight));
            }
            else
            {

                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX, posY, cornerZ.X),
                    normal0,
                    new Vector2(texX, texY));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX + TILE_SIZE, posY, cornerZ.Y),
                    normal1,
                    new Vector2(texX + texWidth, texY));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX, posY + TILE_SIZE, cornerZ.Z),
                    normal2,
                    new Vector2(texX, texY + texHeight));
                _vertexInfo[cur++] = new VertexPositionNormalTexture(
                    new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W),
                    normal3,
                    new Vector2(texX + texWidth, texY + texHeight));
            }

            _numTiles++;
        }

        private const float INVERSE_SQRT2 = 0.70711f;

        public void DrawBillboard(
            Vector3 tilePos,
            Vector3 normal,
            Rectangle texCoords)
        {
            if ((_numTiles + 1) >= MAX_TILES_PER_BATCH)
                Flush();

            int cur = _numTiles * 4;
            var texture = _texture;

            float onePixel = Math.Max(1.0f / texture.Width, EPSILON);

            var texX = texCoords.X / (float)texture.Width + (onePixel / 2f);
            var texY = texCoords.Y / (float)texture.Height + (onePixel / 2f);
            var texWidth = (texCoords.Width / (float)texture.Width) - onePixel;
            var texHeight = (texCoords.Height / (float)texture.Height) - onePixel;

            var projectedWidth = (texCoords.Width / 2f) * INVERSE_SQRT2;

            var posX = tilePos.X + TILE_SIZE;
            var posY = tilePos.Y + TILE_SIZE;

            _vertexInfo[cur++] = new VertexPositionNormalTexture(
                new Vector3(posX - projectedWidth, posY + projectedWidth, tilePos.Z + texCoords.Height),
                normal,
                new Vector2(texX, texY));
            _vertexInfo[cur++] = new VertexPositionNormalTexture(
                new Vector3(posX + projectedWidth, posY - projectedWidth, tilePos.Z + texCoords.Height),
                normal,
                new Vector2(texX + texWidth, texY));
            _vertexInfo[cur++] = new VertexPositionNormalTexture(
                new Vector3(posX - projectedWidth, posY + projectedWidth, tilePos.Z),
                normal,
                new Vector2(texX, texY + texHeight));
            _vertexInfo[cur++] = new VertexPositionNormalTexture(
                new Vector3(posX + projectedWidth, posY - projectedWidth, tilePos.Z),
                normal,
                new Vector2(texX + texWidth, texY + texHeight));

            _numTiles++;
        }
    }
#endregion
    private readonly GraphicsDevice _gfxDevice;

    private readonly MapEffect _effect;

    private readonly RenderTarget2D _mapTarget;

    private readonly DrawBatcher[] _batchers = new DrawBatcher[8];
    private readonly Texture2D[] _textures = new Texture2D[8];

    private Camera _camera;
    private RasterizerState _rasterizerState;
    private SamplerState _samplerState;
    private LightingState _lightingState;
    private DepthStencilState _depthStencilState;
    private BlendState _blendState;
    private Texture2D _shadowMap;

    private DrawBatcher GetBatcher(Texture2D texture)
    {
        for (int i = 0; i < _batchers.Length; i++)
        {
            if (_textures[i] == texture)
            {
                return _batchers[i];
            }
        }

        for (int i = 0; i  < _batchers.Length; i++)
        {
            if (_textures[i] == null)
            {
                _textures[i] = texture;
                _batchers[i].Begin(_effect, texture, _rasterizerState, _samplerState, _lightingState, _depthStencilState, _blendState, _shadowMap);
                return _batchers[i];
            }
        }

        /* TODO: Don't always evict the first one */
        _batchers[0].End();
        _textures[0] = texture;
        _batchers[0].Begin(_effect, texture, _rasterizerState, _samplerState, _lightingState, _depthStencilState, _blendState, _shadowMap);
        return _batchers[0];
    }

    private bool _beginCalled = false;

    public MapRenderer(GraphicsDevice device, RenderTarget2D output)
    {
        _gfxDevice = device;
        _mapTarget = output;

        _effect = new MapEffect(device);

        for (int i = 0; i < _batchers.Length; i++)
        {
            _batchers[i] = new DrawBatcher(device, _mapTarget);
        }
    }

    public void Begin(
        Camera camera,
        RasterizerState rasterizerState,
        SamplerState samplerState,
        LightingState lightingState,
        DepthStencilState depthStencilState,
        BlendState blendState,
        Texture2D shadowMap,
        Camera lightSource
    )
    {
        if (_beginCalled)
            throw new InvalidOperationException("Mismatched Begin and End calls");

        _beginCalled = true;

        _gfxDevice.Textures[0] = null;

        _camera = camera;
        _rasterizerState = rasterizerState;
        _samplerState = samplerState;
        _lightingState = lightingState;
        _depthStencilState = depthStencilState;
        _blendState = blendState;
        _shadowMap = shadowMap;


        _effect.WorldViewProj = _camera.WorldViewProj;
        _effect.LightWorldViewProj = lightSource.WorldViewProj;

        _effect.AmbientLightColor = _lightingState.AmbientLightColor;
        _effect.LightSource.Direction = _lightingState.LightDirection;
        _effect.LightSource.DiffuseColor = _lightingState.LightDiffuseColor;
        _effect.LightSource.SpecularColor = _lightingState.LightSpecularColor;
        _effect.LightSource.Enabled = true;
    }

    private unsafe void Flush()
    {
        for (int i = 0; i < _batchers.Length; i++)
        {
            _batchers[i].End();
            _textures[i] = null;
        }
    }

    public unsafe void End()
    {
        Flush();

        _beginCalled = false;
    }

    public void DrawTile(
        Vector2 tilePos,
        Vector4 cornerZ,
        Vector3 normal0,
        Vector3 normal1,
        Vector3 normal2,
        Vector3 normal3,
        Texture2D texture,
        Rectangle texCoords,
        bool diamondTex)
    {
        var batcher = GetBatcher(texture);
        batcher.DrawTile(tilePos, cornerZ, normal0, normal1, normal2, normal3, texCoords, diamondTex);
    }

    public void DrawBillboard(
        Vector3 tilePos,
        Vector3 normal,
        Texture2D texture,
        Rectangle texCoords)
    {
        var batcher = GetBatcher(texture);
        batcher.DrawBillboard(tilePos, normal, texCoords);
    }
}
