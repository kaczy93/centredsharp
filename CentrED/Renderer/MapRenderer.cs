using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CentrED.Lights;
using CentrED.Map;
using CentrED.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

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
    public Vector3 Texture;
    public Vector4 Hue;
    public Vector3 Normal;

    public static readonly VertexDeclaration VertexDeclaration;

    static MapVertex()
    {
        VertexDeclaration = new VertexDeclaration
        (
            [
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(40, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            ]
        );
    }

    public MapVertex(Vector3 position, Vector3 texture, Vector4 hue, Vector3 normal)
    {
        Position = position;
        Texture = texture;
        Hue = hue;
        Normal = normal;
    }
}

public class MapRenderer
{
    public readonly struct FrameStats
    {
        public FrameStats(int flushes, int drawCalls, int cachedDrawCalls, int textureEvictions, int vertexUploads, long verticesUploaded)
        {
            Flushes = flushes;
            DrawCalls = drawCalls;
            CachedDrawCalls = cachedDrawCalls;
            TextureEvictions = textureEvictions;
            VertexUploads = vertexUploads;
            VerticesUploaded = verticesUploaded;
        }

        public int Flushes { get; }
        public int DrawCalls { get; }
        public int CachedDrawCalls { get; }
        public int TextureEvictions { get; }
        public int VertexUploads { get; }
        public long VerticesUploaded { get; }
    }

    #region Draw Batcher

    private class DrawBatcher
    {
        private const int MAX_TILES_PER_BATCH = 8192;
        private const int MAX_VERTICES = MAX_TILES_PER_BATCH * 4;
        private const int MAX_INDICES = MAX_TILES_PER_BATCH * 6;

        private readonly MapRenderer _owner;
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

        private bool _beginCalled = false;
        private int _vertexCount = 0;
        public int PendingVertexCount => _vertexCount;

        public DrawBatcher(MapRenderer owner, GraphicsDevice device)
        {
            _owner = owner;
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
            BlendState blendState
        )
        {
            if (_beginCalled)
                throw new InvalidOperationException("Mismatched Begin and End calls");

            _beginCalled = true;
            _vertexCount = 0;

            _effect = effect;
            _texture = texture;
            _rasterizerState = rasterizerState;
            _samplerState = samplerState;
            _depthStencilState = depthStencilState;
            _blendState = blendState;
        }

        private unsafe void Flush()
        {
            if (_vertexCount == 0)
                return;

            fixed (MapVertex* p = &_vertexInfo[0])
            {
                _vertexBuffer.SetDataPointerEXT
                    (0, (IntPtr)p, Unsafe.SizeOf<MapVertex>() * _vertexCount, SetDataOptions.Discard);
            }
            _owner._flushes++;
            _owner._vertexUploads++;
            _owner._verticesUploaded += _vertexCount;

            _gfxDevice.SetVertexBuffer(_vertexBuffer);
            _gfxDevice.Indices = _indexBuffer;

            _gfxDevice.RasterizerState = _rasterizerState;
            _gfxDevice.Textures[0] = _texture;
            _gfxDevice.SamplerStates[0] = _samplerState;
            _gfxDevice.Textures[1] = HuesManager.Instance.Texture;
            _gfxDevice.SamplerStates[1] = SamplerState.PointClamp; //TODO: pass this from huesManager
            _gfxDevice.Textures[2] = LightsManager.Instance.LightColorsTexture;
            _gfxDevice.SamplerStates[2] = SamplerState.PointClamp; 
            _gfxDevice.DepthStencilState = _depthStencilState;
            _gfxDevice.BlendState = _blendState;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _vertexCount / 2);
                _owner._drawCalls++;
            }

            _vertexCount = 0;
        }

        public void End()
        {
            Flush();
            _beginCalled = false;
        }

        public void DrawMapObject(MapObject o, Vector4 hueOverride)
        {
            DrawVertices(o.Vertices, o.Vertices.Length, hueOverride);
        }

        public void DrawVertices(MapVertex[] vertices, int vertexCount, Vector4 hueOverride)
        {
            if (_vertexCount + vertexCount >= MAX_VERTICES)
                Flush();
            
            for (var i = 0; i < vertexCount; i++)
            {
                _vertexInfo[_vertexCount] = vertices[i];
                if (hueOverride != default)
                {
                    _vertexInfo[_vertexCount].Hue = hueOverride;
                }
                _vertexCount++;
            }
        }
    }

    #endregion

    private readonly GraphicsDevice _gfxDevice;
    private readonly GameWindow _window;

    private readonly DrawBatcher[] _batchers = new DrawBatcher[32];
    private readonly Texture2D[] _textures = new Texture2D[32];
    private readonly long[] _batcherLastUsed = new long[32];
    private long _batcherUseCounter;

    private MapEffect _effect;
    private RasterizerState _rasterizerState;
    private SamplerState _samplerState;
    private DepthStencilState _depthStencilState;
    private BlendState _blendState;
    private int _flushes;
    private int _drawCalls;
    private int _cachedDrawCalls;
    private int _textureEvictions;
    private int _vertexUploads;
    private long _verticesUploaded;

    public FrameStats Stats => new(_flushes, _drawCalls, _cachedDrawCalls, _textureEvictions, _vertexUploads, _verticesUploaded);

    public void ResetFrameStats()
    {
        _flushes = 0;
        _drawCalls = 0;
        _cachedDrawCalls = 0;
        _textureEvictions = 0;
        _vertexUploads = 0;
        _verticesUploaded = 0;
    }

    private DrawBatcher GetBatcher(Texture2D texture)
    {
        for (int i = 0; i < _batchers.Length; i++)
        {
            if (_textures[i] == texture)
            {
                _batcherLastUsed[i] = ++_batcherUseCounter;
                return _batchers[i];
            }
        }

        for (int i = 0; i < _batchers.Length; i++)
        {
            if (_textures[i] == null)
            {
                _textures[i] = texture;
                _batcherLastUsed[i] = ++_batcherUseCounter;
                _batchers[i].Begin
                (
                    _effect,
                    texture,
                    _rasterizerState,
                    _samplerState,
                    _depthStencilState,
                    _blendState
                );
                return _batchers[i];
            }
        }

        var evictIndex = GetEvictionIndex();
        _textureEvictions++;
        _batchers[evictIndex].End();
        _textures[evictIndex] = texture;
        _batcherLastUsed[evictIndex] = ++_batcherUseCounter;
        _batchers[evictIndex].Begin
        (
            _effect,
            texture,
            _rasterizerState,
            _samplerState,
            _depthStencilState,
            _blendState
        );
        return _batchers[evictIndex];
    }

    private int GetEvictionIndex()
    {
        var bestIndex = 0;
        var bestVertexCount = _batchers[0].PendingVertexCount;
        var bestLastUsed = _batcherLastUsed[0];

        for (int i = 1; i < _batchers.Length; i++)
        {
            var vertexCount = _batchers[i].PendingVertexCount;
            var lastUsed = _batcherLastUsed[i];
            if (vertexCount < bestVertexCount || vertexCount == bestVertexCount && lastUsed < bestLastUsed)
            {
                bestIndex = i;
                bestVertexCount = vertexCount;
                bestLastUsed = lastUsed;
            }
        }

        return bestIndex;
    }

    private bool _beginCalled = false;

    public MapRenderer(GraphicsDevice device, GameWindow window)
    {
        _gfxDevice = device;
        _window = window;

        for (int i = 0; i < _batchers.Length; i++)
        {
            _batchers[i] = new DrawBatcher(this, device);
        }
    }

    public void Begin
    (
        MapEffect effect,
        RasterizerState rasterizerState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        BlendState blendState
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

        for (int i = 0; i < _batchers.Length; i++)
        {
            _textures[i] = null;
            _batcherLastUsed[i] = 0;
        }
    }

    public void SetRenderTarget(RenderTarget2D output)
    {
        SetRenderTarget(output, _window.ClientBounds);
    }
    
    public void SetRenderTarget(RenderTarget2D output, Rectangle bounds)
    {
        _gfxDevice.SetRenderTarget(output);
        _gfxDevice.Clear(Color.Black);
        _gfxDevice.Viewport = new Viewport(0, 0, bounds.Width, bounds.Height);
        _gfxDevice.ScissorRectangle = new Rectangle(0, 0, bounds.Width, bounds.Height);
    }

    private unsafe void Flush()
    {
        for (int i = 0; i < _batchers.Length; i++)
        {
            _batchers[i].End();
            _textures[i] = null;
        }
    }

    public void FlushPending()
    {
        Flush();
    }

    public unsafe void End()
    {
        Flush();

        _beginCalled = false;
    }

    public void DrawMapObject(MapObject mapObject, Vector4 hueOverride)
    {
        var batcher = GetBatcher(mapObject.Texture);
        batcher.DrawMapObject(mapObject, hueOverride);
    }

    private IndexBuffer _quadIndexBuffer;
    private int _quadIndexCapacityQuads;

    public void EnsureQuadIndexCapacity(int quads)
    {
        if (_quadIndexBuffer != null && quads <= _quadIndexCapacityQuads)
            return;
        int newCap = Math.Max(quads, Math.Max(2048, _quadIndexCapacityQuads * 2));
        _quadIndexBuffer?.Dispose();
        var indices = new int[newCap * 6];
        for (int q = 0, i = 0, v = 0; q < newCap; q++, v += 4)
        {
            indices[i++] = v;
            indices[i++] = v + 1;
            indices[i++] = v + 2;
            indices[i++] = v + 3;
            indices[i++] = v + 2;
            indices[i++] = v + 1;
        }
        _quadIndexBuffer = new IndexBuffer(_gfxDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
        _quadIndexBuffer.SetData(indices);
        _quadIndexCapacityQuads = newCap;
    }

    public void DrawCachedVertices(Texture2D texture, VertexBuffer vertexBuffer, int vertexCount, int primitiveCount)
    {
        _gfxDevice.SetVertexBuffer(vertexBuffer);
        _gfxDevice.Indices = _quadIndexBuffer;

        _gfxDevice.RasterizerState = _rasterizerState;
        _gfxDevice.Textures[0] = texture;
        _gfxDevice.SamplerStates[0] = _samplerState;
        _gfxDevice.Textures[1] = HuesManager.Instance.Texture;
        _gfxDevice.SamplerStates[1] = SamplerState.PointClamp;
        _gfxDevice.Textures[2] = LightsManager.Instance.LightColorsTexture;
        _gfxDevice.SamplerStates[2] = SamplerState.PointClamp;
        _gfxDevice.DepthStencilState = _depthStencilState;
        _gfxDevice.BlendState = _blendState;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, primitiveCount);
            _drawCalls++;
            _cachedDrawCalls++;
        }
    }
}
