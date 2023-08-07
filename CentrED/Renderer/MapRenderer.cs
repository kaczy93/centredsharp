using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CentrED.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer;

public class LightingState
{
    public Vector3 LightDirection;
    public Vector3 LightDiffuseColor;
    public Vector3 LightSpecularColor;
    public Vector3 AmbientLightColor;
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MapVertex : IVertexType
{
    VertexDeclaration IVertexType.VertexDeclaration {
        get {
            return VertexDeclaration;
        }
    }

    public Vector3 Position;
    public Vector3 Normal;
    public Vector3 TextureCoordinate;
    public Vector3 HueVec;

    public static readonly VertexDeclaration VertexDeclaration;

    static MapVertex()
    {
        VertexDeclaration = new VertexDeclaration(
            new VertexElement[]
            {
                new VertexElement(
                    0,
                    VertexElementFormat.Vector3,
                    VertexElementUsage.Position,
                    0
                ),
                new VertexElement(
                    12,
                    VertexElementFormat.Vector3,
                    VertexElementUsage.Normal,
                    0
                ),
                new VertexElement(
                    24,
                    VertexElementFormat.Vector3,
                    VertexElementUsage.TextureCoordinate,
                    0
                ),
                new VertexElement(
                    36,
                    VertexElementFormat.Vector3,
                    VertexElementUsage.TextureCoordinate,
                    0
                )
            }
        );
    }

    public MapVertex(
        Vector3 position,
        Vector3 normal,
        Vector3 textureCoordinate,
        Vector3 hueVec
    )
    {
        Position = position;
        Normal = normal;
        TextureCoordinate = textureCoordinate;
        HueVec = hueVec;
    }
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

        private readonly VertexBuffer _vertexBuffer;
        private readonly IndexBuffer _indexBuffer;

        private readonly MapVertex[] _vertexInfo;
        private static readonly short[] _indexData = GenerateIndexArray();

        private RenderTarget2D _renderTarget;
        private MapEffect _effect;
        private Camera _camera;
        private Texture2D _texture;
        private Texture2D _shadowMap;
        private RasterizerState _rasterizerState;
        private SamplerState _samplerState;
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

        private float TILE_SIZE = 31.11f;

        private bool _beginCalled = false;
        private int _numTiles = 0;

        public DrawBatcher(GraphicsDevice device)
        {
            _gfxDevice = device;

            _vertexInfo = new MapVertex[MAX_VERTICES];

            _vertexBuffer = new DynamicVertexBuffer(
                device,
                typeof(MapVertex),
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
            RenderTarget2D output,
            MapEffect effect,
            Camera camera,
            Texture2D texture,
            RasterizerState rasterizerState,
            SamplerState samplerState,
            DepthStencilState depthStencilState,
            BlendState blendState,
            Texture2D shadowMap
        )
        {
            if (_beginCalled)
                throw new InvalidOperationException("Mismatched Begin and End calls");

            _beginCalled = true;
            _numTiles = 0;

            _renderTarget = output;

            _effect = effect;
            _camera = camera;
            _texture = texture;
            _rasterizerState = rasterizerState;
            _samplerState = samplerState;
            _depthStencilState = depthStencilState;
            _blendState = blendState;
            _shadowMap = shadowMap;
        }

        private unsafe void Flush()
        {
            if (_numTiles == 0)
                return;

            _gfxDevice.SetRenderTarget(_renderTarget);

            fixed (MapVertex* p = &_vertexInfo[0])
            {
                _vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, Unsafe.SizeOf<MapVertex>() * _numTiles * 4, SetDataOptions.Discard);
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
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX, posY, cornerZ.X),
                    normal0,
                    new Vector3(texX + (texWidth / 2f), texY, 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX + TILE_SIZE, posY, cornerZ.Y),
                    normal1,
                    new Vector3(texX + texWidth, texY + (texHeight / 2f), 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX, posY + TILE_SIZE, cornerZ.Z),
                    normal2,
                    new Vector3(texX, texY + (texHeight / 2f), 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W),
                    normal3,
                    new Vector3(texX + (texWidth / 2f), texY + texHeight, 0),
                    Vector3.Zero);
            }
            else
            {

                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX, posY, cornerZ.X),
                    normal0,
                    new Vector3(texX, texY, 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX + TILE_SIZE, posY, cornerZ.Y),
                    normal1,
                    new Vector3(texX + texWidth, texY, 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX, posY + TILE_SIZE, cornerZ.Z),
                    normal2,
                    new Vector3(texX, texY + texHeight, 0),
                    Vector3.Zero);
                _vertexInfo[cur++] = new MapVertex(
                    new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W),
                    normal3,
                    new Vector3(texX + texWidth, texY + texHeight, 0),
                    Vector3.Zero);
            }

            _numTiles++;
        }

        private const float INVERSE_SQRT2 = 0.70711f;
        
        public void DrawBillboard(
            Vector3 tilePos,
            float depthOffset,
            Rectangle texCoords,
            Vector3 hueVec,
            bool cylindrical)

        {
            if (_numTiles + 1 >= MAX_TILES_PER_BATCH)
                Flush();

            int cur = _numTiles * 4;
            var texture = _texture;

            float onePixel = Math.Max(1.0f / texture.Width, EPSILON);

            var texX = texCoords.X / (float)texture.Width + (onePixel / 2f);
            var texY = texCoords.Y / (float)texture.Height + (onePixel / 2f);
            var texWidth = (texCoords.Width / (float)texture.Width) - onePixel;
            var texHeight = (texCoords.Height / (float)texture.Height) - onePixel;

            var posX = tilePos.X + TILE_SIZE;
            var posY = tilePos.Y + TILE_SIZE;
            
            var projectedWidth = (texCoords.Width / 2f) * INVERSE_SQRT2;

            Vector3 v1 = new Vector3(posX - projectedWidth, posY + projectedWidth, tilePos.Z + texCoords.Height);
            Vector3 v2 = new Vector3(posX + projectedWidth, posY - projectedWidth, tilePos.Z + texCoords.Height);
            Vector3 v3 = new Vector3(posX - projectedWidth, posY + projectedWidth, tilePos.Z);
            Vector3 v4 = new Vector3(posX + projectedWidth, posY - projectedWidth, tilePos.Z);
            
            Vector3 t1 = new Vector3(texX, texY, depthOffset);
            Vector3 t2 = new Vector3(texX + texWidth, texY, depthOffset);
            Vector3 t3 = new Vector3(texX, texY + texHeight, depthOffset);
            Vector3 t4 = new Vector3(texX + texWidth, texY + texHeight, depthOffset);
            
            _vertexInfo[cur++] = new MapVertex(
                v1,
                Vector3.UnitZ,
                t1,
                hueVec);
            _vertexInfo[cur++] = new MapVertex(
                v2,
                Vector3.UnitZ,
                t2,
                hueVec);
            _vertexInfo[cur++] = new MapVertex(
                v3,
                Vector3.UnitZ,
                t3,
                hueVec);
            _vertexInfo[cur] = new MapVertex(
                v4,
                Vector3.UnitZ,
                t4,
                hueVec);

            _numTiles++;
        }
    }
#endregion

    private readonly GraphicsDevice _gfxDevice;

    private readonly DrawBatcher[] _batchers = new DrawBatcher[8];
    private readonly Texture2D[] _textures = new Texture2D[8];

    private RenderTarget2D _mapTarget;
    private MapEffect _effect;
    private Camera _camera;
    private RasterizerState _rasterizerState;
    private SamplerState _samplerState;
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
                _batchers[i].Begin(_mapTarget, _effect, _camera, texture, _rasterizerState, _samplerState, _depthStencilState, _blendState, _shadowMap);
                return _batchers[i];
            }
        }

        /* TODO: Don't always evict the first one */
        _batchers[0].End();
        _textures[0] = texture;
        _batchers[0].Begin(_mapTarget, _effect, _camera, texture, _rasterizerState, _samplerState, _depthStencilState, _blendState, _shadowMap);
        return _batchers[0];
    }

    private bool _beginCalled = false;

    public MapRenderer(GraphicsDevice device)
    {
        _gfxDevice = device;

        for (int i = 0; i < _batchers.Length; i++)
        {
            _batchers[i] = new DrawBatcher(device);
        }
    }

    public void Begin(
        RenderTarget2D output,
        MapEffect effect,
        Camera camera,
        RasterizerState rasterizerState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        BlendState blendState,
        Texture2D shadowMap
    )
    {
        if (_beginCalled)
            throw new InvalidOperationException("Mismatched Begin and End calls");

        _beginCalled = true;

        _mapTarget = output;
        _effect = effect;

        _gfxDevice.Textures[0] = null;

        _camera = camera;
        _rasterizerState = rasterizerState;
        _samplerState = samplerState;
        _depthStencilState = depthStencilState;
        _blendState = blendState;
        _shadowMap = shadowMap;

        for (int i = 0; i < _batchers.Length; i++)
        {
            _textures[i] = null;
        }
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
        float depthOffset,
        Texture2D texture,
        Rectangle texCoords,
        Vector3 hueVec,
        bool cylindrical)
    {
        var batcher = GetBatcher(texture);
        batcher.DrawBillboard(tilePos, depthOffset, texCoords, hueVec, cylindrical);
    }

}
