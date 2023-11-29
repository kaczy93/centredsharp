using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CentrED.Map;
using CentrED.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MapVertex : IVertexType
{
    VertexDeclaration IVertexType.VertexDeclaration
    {
        get { return VertexDeclaration; }
    }

    public Vector3 Position;
    public Vector3 TextureCoordinate;
    public Vector3 HueVec;

    public static readonly VertexDeclaration VertexDeclaration;

    static MapVertex()
    {
        VertexDeclaration = new VertexDeclaration
        (
            new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            }
        );
    }

    public MapVertex(Vector3 position, Vector3 textureCoordinate, Vector3 hueVec)
    {
        Position = position;
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

        private MapEffect _effect;
        private Texture2D _texture;
        private Texture2D _huesTexture;
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

            _vertexBuffer = new DynamicVertexBuffer(device, typeof(MapVertex), MAX_VERTICES, BufferUsage.WriteOnly);

            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);

            _indexBuffer.SetData(_indexData);
        }

        public void Begin
        (
            MapEffect effect,
            Texture2D texture,
            RasterizerState rasterizerState,
            SamplerState samplerState,
            DepthStencilState depthStencilState,
            BlendState blendState,
            Texture2D huesTexture
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
            _depthStencilState = depthStencilState;
            _blendState = blendState;
            _huesTexture = huesTexture;
        }

        private unsafe void Flush()
        {
            if (_numTiles == 0)
                return;

            fixed (MapVertex* p = &_vertexInfo[0])
            {
                _vertexBuffer.SetDataPointerEXT
                    (0, (IntPtr)p, Unsafe.SizeOf<MapVertex>() * _numTiles * 4, SetDataOptions.Discard);
            }

            _gfxDevice.SetVertexBuffer(_vertexBuffer);
            _gfxDevice.Indices = _indexBuffer;

            _gfxDevice.RasterizerState = _rasterizerState;
            _gfxDevice.Textures[0] = _texture;
            _gfxDevice.SamplerStates[0] = _samplerState;
            _gfxDevice.Textures[1] = _huesTexture;
            _gfxDevice.SamplerStates[1] = SamplerState.PointClamp; //TODO: pass this from huesManager
            _gfxDevice.DepthStencilState = _depthStencilState;
            _gfxDevice.BlendState = _blendState;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _numTiles * 4, 0, _numTiles * 2);
            }

            _numTiles = 0;
        }

        public void End()
        {
            Flush();
            _beginCalled = false;
        }

        public void DrawMapObject(MapObject o, Vector3 hueOverride)
        {
            if (_numTiles + 1 >= MAX_TILES_PER_BATCH)
                Flush();

            int cur = _numTiles * 4;

            for (var i = 0; i < 4; i++)
            {
                _vertexInfo[cur + i] = o.Vertices[i];
                if (hueOverride != default)
                {
                    _vertexInfo[cur + i].HueVec = hueOverride;
                }
            }
            _numTiles++;
        }
    }

    #endregion

    private readonly GraphicsDevice _gfxDevice;

    private readonly DrawBatcher[] _batchers = new DrawBatcher[8];
    private readonly Texture2D[] _textures = new Texture2D[8];

    private MapEffect _effect;
    private RasterizerState _rasterizerState;
    private SamplerState _samplerState;
    private DepthStencilState _depthStencilState;
    private BlendState _blendState;
    private Texture2D _huesTexture;

    private DrawBatcher GetBatcher(Texture2D texture)
    {
        for (int i = 0; i < _batchers.Length; i++)
        {
            if (_textures[i] == texture)
            {
                return _batchers[i];
            }
        }

        for (int i = 0; i < _batchers.Length; i++)
        {
            if (_textures[i] == null)
            {
                _textures[i] = texture;
                _batchers[i].Begin
                (
                    _effect,
                    texture,
                    _rasterizerState,
                    _samplerState,
                    _depthStencilState,
                    _blendState,
                    _huesTexture
                );
                return _batchers[i];
            }
        }

        /* TODO: Don't always evict the first one */
        _batchers[0].End();
        _textures[0] = texture;
        _batchers[0].Begin
        (
            _effect,
            texture,
            _rasterizerState,
            _samplerState,
            _depthStencilState,
            _blendState,
            _huesTexture
        );
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

    public void Begin
    (
        MapEffect effect,
        RasterizerState rasterizerState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        BlendState blendState,
        Texture2D huesTexture
    )
    {
        if (_beginCalled)
            throw new InvalidOperationException("Mismatched Begin and End calls");

        _beginCalled = true;

        _effect = effect;

        _gfxDevice.Textures[0] = null;

        _rasterizerState = rasterizerState;
        _samplerState = samplerState;
        _depthStencilState = depthStencilState;
        _blendState = blendState;
        _huesTexture = huesTexture;

        for (int i = 0; i < _batchers.Length; i++)
        {
            _textures[i] = null;
        }
    }

    public void SetRenderTarget(RenderTarget2D output)
    {
        _gfxDevice.SetRenderTarget(output);
        _gfxDevice.Clear(Color.Black);
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

    public void DrawMapObject(MapObject mapObject, Vector3 hueOverride)
    {
        var batcher = GetBatcher(mapObject.Texture);
        batcher.DrawMapObject(mapObject, hueOverride);
    }
}