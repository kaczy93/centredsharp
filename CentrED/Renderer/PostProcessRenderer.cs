using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer;

public class PostProcessRenderer
{
    private readonly GraphicsDevice _gfxDevice;

    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;

    private readonly VertexPositionTexture[] _vertexInfo = new VertexPositionTexture[4];

    private static readonly short[] _indexData = new short[6]
    {
        0, 1, 2, 3, 2, 1
    };

    private RasterizerState _rasterizerState;
    private SamplerState _samplerState;
    private LightingState _lightingState;
    private DepthStencilState _depthStencilState;
    private BlendState _blendState;

    public PostProcessRenderer(GraphicsDevice device)
    {
        _gfxDevice = device;

        _vertexBuffer = new DynamicVertexBuffer(
            device,
            typeof(VertexPositionTexture),
            4,
            BufferUsage.WriteOnly
        );

        _indexBuffer = new IndexBuffer(
            device,
            IndexElementSize.SixteenBits,
            6,
            BufferUsage.WriteOnly
        );

        _indexBuffer.SetData(_indexData);
    }

    // Draw the input texture to fill the output target. Output can be null to draw to the back buffer.
    // This is a simple point clamp scaling.
    public unsafe void Scale(
        Texture2D input,
        RenderTarget2D output
    )
    {
        var width = output == null ? _gfxDevice.PresentationParameters.BackBufferWidth : output.Width;
        var height = output == null ? _gfxDevice.PresentationParameters.BackBufferHeight : output.Height;

        _gfxDevice.SetRenderTarget(output);

        _vertexInfo[0] = new VertexPositionTexture(
            new Vector3(0, 0, 0),
            new Vector2(0, 0)
        );
        _vertexInfo[1] = new VertexPositionTexture(
            new Vector3(width, 0, 0),
            new Vector2(1, 0)
        );
        _vertexInfo[2] = new VertexPositionTexture(
            new Vector3(0, height, 0),
            new Vector2(0, 1)
        );
        _vertexInfo[3] = new VertexPositionTexture(
            new Vector3(width, height, 0),
            new Vector2(1, 1)
        );

        fixed (VertexPositionTexture* p = &_vertexInfo[0])
        {
            _vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, Unsafe.SizeOf<VertexPositionTexture>() * 4, SetDataOptions.Discard);
        }

        _gfxDevice.SetVertexBuffer(_vertexBuffer);
        _gfxDevice.Indices = _indexBuffer;

        _gfxDevice.RasterizerState = RasterizerState.CullNone;
        _gfxDevice.SamplerStates[0] = SamplerState.PointClamp;
        _gfxDevice.DepthStencilState = DepthStencilState.None;
        _gfxDevice.BlendState = BlendState.Opaque;

        BasicEffect effect = new BasicEffect(_gfxDevice);

        effect.World = Matrix.Identity;
        effect.View = Matrix.Identity;
        effect.Projection = Matrix.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
        effect.TextureEnabled = true;
        effect.Texture = input;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gfxDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
        }
    }
}