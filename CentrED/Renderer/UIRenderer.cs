using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SDL3.SDL;
using ImVec2 = System.Numerics.Vector2;

namespace CentrED.Renderer;

public static class DrawVertDeclaration
{
    public static readonly VertexDeclaration Declaration;

    public static readonly int Size;

    static DrawVertDeclaration()
    {
        unsafe
        {
            Size = sizeof(ImDrawVert);
        }

        Declaration = new VertexDeclaration
        (
            Size,

            // Position
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

            // UV
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

            // Color
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
    }
}

public partial class UIRenderer
{
    // Graphics
    private GraphicsDevice _graphicsDevice;

    private BasicEffect? _effect;
    private RasterizerState _rasterizerState;

    private byte[]? _vertexData;
    private VertexBuffer? _vertexBuffer;
    private int _vertexBufferSize;

    private byte[]? _indexData;
    private IndexBuffer? _indexBuffer;
    private int _indexBufferSize;

    // Textures
    private Texture2D?[] _LoadedTextures;

    public unsafe UIRenderer(GraphicsDevice graphicsDevice, bool initViewports)
    {
        _graphicsDevice = graphicsDevice;

        _LoadedTextures = new Texture2D[2];

        _rasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0
        };
        
        var io = ImGui.GetIO();
        
        io.BackendPlatformName = (byte*)new FixedAsciiString("FNA.SDL3 Backend").DataPtr;
        
        if (initViewports)
        {
            InitMultiViewportSupport();
        }
    }
    
    /// <summary>
    /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
    /// </summary>
    public virtual ImTextureID BindTexture(Texture2D texture)
    {
        var id = Array.IndexOf(_LoadedTextures, texture);
        if (id == -1)
        {
            //Zero index is null ImTextureID, so we just keep this one empty.
            for (var i = 1; i < _LoadedTextures.Length; i++) 
            {
                if (_LoadedTextures[i] == null)
                {
                    _LoadedTextures[i] = texture;
                    id = i;
                    break;
                }
            }
            if (id == -1)
            {
                id = _LoadedTextures.Length;
                Array.Resize(ref _LoadedTextures, _LoadedTextures.Length * 2);
                _LoadedTextures[id] = texture;
            }
        }

        return new ImTextureID(id);
    }

    /// <summary>
    /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
    /// </summary>
    public virtual void UnbindTexture(ImTextureID textureId)
    {
        _LoadedTextures[(int)textureId.Handle] = null;
    }

    /// <summary>
    /// Updates the <see cref="Effect" /> to the current matrices and texture
    /// </summary>
    protected virtual Effect UpdateEffect(Texture2D texture, ImDrawDataPtr drawData)
    {
        _effect ??= new BasicEffect(_graphicsDevice);

        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            drawData.DisplayPos.X,
            drawData.DisplayPos.X + drawData.DisplaySize.X,
            drawData.DisplayPos.Y + drawData.DisplaySize.Y,
            drawData.DisplayPos.Y, -1f, 1f);
        _effect.TextureEnabled = true;
        _effect.Texture = texture;
        _effect.VertexColorEnabled = true;

        return _effect;
    }

    public unsafe void RenderMainWindow()
    {
        RenderDrawData(ImGui.GetDrawData());
    }

    /// <summary>
    /// Gets the geometry as set up by ImGui and sends it to the graphics device
    /// </summary>
    private unsafe void RenderDrawData(ImDrawData* drawData)
    {
        if (drawData->Textures != ImTextureDataPtr.Null)
        {
            for (var i = 0; i < drawData->Textures->Size; i++)
            {
                var tex = drawData->Textures->Data[i];
                if (tex.Status != ImTextureStatus.Ok)
                {
                    UpdateTexture(tex);
                }
            }
        }
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData->ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);
    }

    private unsafe void UpdateTexture(ImTextureDataPtr tex)
    {
        if (tex.Status == ImTextureStatus.WantCreate)
        {
            var texData = tex.GetPixels();
            var pixels = new byte[tex.GetSizeInBytes()];
            Marshal.Copy(new IntPtr(texData), pixels, 0, pixels.Length);
            var tex2D = new Texture2D(_graphicsDevice, tex.Width, tex.Height, false, SurfaceFormat.Color);
            tex2D.SetData(pixels);
            var textureId = BindTexture(tex2D);
            tex.SetTexID(textureId);
            tex.SetStatus(ImTextureStatus.Ok);
        }
        if (tex.Status == ImTextureStatus.WantUpdates)
        {
            var tex2d = _LoadedTextures[(int)tex.TexID.Handle];
            var x = tex.UpdateRect.X;
            var y = tex.UpdateRect.Y;
            var w = tex.UpdateRect.W;
            var h = tex.UpdateRect.H;
            var pixels = new byte[w * h * tex.BytesPerPixel];
            var pos = 0;
            var rowLength = w * tex.BytesPerPixel;
            for(var i = y; i < y + h; i++){
                var updateData = tex.GetPixelsAt(x, i);
                Marshal.Copy(new IntPtr(updateData), pixels, pos, rowLength);
                pos += rowLength;
            }
            tex2d.SetData(0, new Rectangle(x,y,w,h), pixels, 0, pixels.Length);
            tex.SetStatus(ImTextureStatus.Ok);
        }
        if (tex.Status == ImTextureStatus.WantDestroy)
        {
            var tex2d = _LoadedTextures[(int)tex.TexID.Handle];
            tex2d.Dispose();
            UnbindTexture(tex.TexID);
            tex.SetTexID(ImTextureID.Null);
            tex.SetStatus(ImTextureStatus.Destroyed);
        }
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();

            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new VertexBuffer
                (_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
            _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();

            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new IndexBuffer
                (_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
            _indexData = new byte[_indexBufferSize * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
            {
                Buffer.MemoryCopy
                (
                    (void*)cmdList.VtxBuffer.Data,
                    vtxDstPtr,
                    _vertexData.Length,
                    cmdList.VtxBuffer.Size * DrawVertDeclaration.Size
                );
                Buffer.MemoryCopy
                (
                    (void*)cmdList.IdxBuffer.Data,
                    idxDstPtr,
                    _indexData.Length,
                    cmdList.IdxBuffer.Size * sizeof(ushort)
                );
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        fixed (byte* p = &_vertexData[0])
        {
            _vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, drawData.TotalVtxCount * DrawVertDeclaration.Size, SetDataOptions.Discard);
        }
        
        fixed (byte* p = &_indexData[0])
        {
            _indexBuffer.SetDataPointerEXT(0, (IntPtr)p, drawData.TotalIdxCount * sizeof(ushort), SetDataOptions.Discard);
        }
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        if (drawData.DisplaySize.X == 0 || drawData.DisplaySize.Y == 0)
        {
            return;
        } 
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                ImDrawCmd drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                var textureIdx = (int)drawCmd.GetTexID();

                _graphicsDevice.ScissorRectangle = new Rectangle
                (
                    (int)(drawCmd.ClipRect.X - drawData.DisplayPos.X),
                    (int)(drawCmd.ClipRect.Y - drawData.DisplayPos.Y),
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                var effect = UpdateEffect(_LoadedTextures[textureIdx], drawData);

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawIndexedPrimitives
                    (
                        PrimitiveType.TriangleList,
                        (int)drawCmd.VtxOffset + vtxOffset,
                        0,
                        cmdList.VtxBuffer.Size,
                        (int)drawCmd.IdxOffset + idxOffset,
                        (int)drawCmd.ElemCount / 3
                    );
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }
}