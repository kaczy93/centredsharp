using System.Runtime.CompilerServices;
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
    private List<Texture2D> _loadedTextures;

    private IntPtr? _fontTextureId;
    
    public unsafe UIRenderer(GraphicsDevice graphicsDevice, GameWindow window)
    {
        _graphicsDevice = graphicsDevice;

        _loadedTextures = new List<Texture2D>();

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
        
        io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("FNA.SDL3 Backend").DataPtr;
        
        UpdateMonitors();
        
        if (io.BackendFlags.HasFlag(ImGuiBackendFlags.PlatformHasViewports))
        {
            InitMultiViewportSupport(window);
        }
    }

    private unsafe void UpdateMonitors()
    {
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
        var displayIds = (uint*)SDL_GetDisplays(out int numMonitors);
        IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
        platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
        for (int i = 0; i < numMonitors; i++)
        {
            uint displayId = *displayIds;
            SDL_GetDisplayBounds(displayId, out var bounds);
            ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];
            monitor.MainPos = monitor.WorkPos = new ImVec2(bounds.x, bounds.y);
            monitor.MainSize = monitor.WorkSize = new ImVec2(bounds.w, bounds.h);
            if (SDL_GetDisplayUsableBounds(displayId, out var workBounds) && workBounds.w > 0 && workBounds.h > 0)
            {
                monitor.WorkPos = new ImVec2(workBounds.x, workBounds.y);
                monitor.WorkSize = new ImVec2(workBounds.w, workBounds.h);
            }
            monitor.DpiScale = SDL_GetDisplayContentScale(displayId);
            monitor.PlatformHandle = new IntPtr(i);
            displayIds++;
        }
    }

    /// <summary>
    /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
    /// </summary>
    public virtual unsafe void RebuildFontAtlas()
    {
        // Get font texture from ImGui
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        // Copy the data to a managed array
        var pixels = new byte[width * height * bytesPerPixel];
        unsafe
        {
            Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);
        }

        // Create and register the texture as an XNA texture
        var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        tex2d.SetData(pixels);

        // Should a texture already have been build previously, unbind it first so it can be deallocated
        if (_fontTextureId.HasValue)
            UnbindTexture(_fontTextureId.Value);

        // Bind the new texture to an ImGui-friendly id
        _fontTextureId = BindTexture(tex2d);

        // Let ImGui know where to find the texture
        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData(); // Clears CPU side texture data
    }

    /// <summary>
    /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
    /// </summary>
    public virtual IntPtr BindTexture(Texture2D texture)
    {
        var id = _loadedTextures.IndexOf(texture);
        if (id == -1)
        {
            _loadedTextures.Add(texture);
            id = _loadedTextures.Count - 1;
        }

        return new IntPtr(id);
    }

    /// <summary>
    /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
    /// </summary>
    public virtual void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.RemoveAt(textureId.ToInt32());
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

    public void NewFrame()
    {
        var mainViewport = ImGui.GetMainViewport();
        var mainWindowHandle = mainViewport.PlatformHandle;
        SDL_GetWindowSize(mainWindowHandle, out var w, out var h);
        if (SDL_GetWindowFlags(mainWindowHandle).HasFlag(SDL_WindowFlags.SDL_WINDOW_MINIMIZED))
        {
            w = h = 0;
        }
        if (w > 0 && h > 0)
        {
            ImGui.GetIO().DisplaySize = new ImVec2(w, h);
            SDL_GetWindowSizeInPixels(mainViewport.PlatformHandle, out var pw, out var ph);
            ImGui.GetIO().DisplayFramebufferScale = new ImVec2(pw / (float)w, ph / (float)h);
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UpdateMonitors();
        }
    }

    public void RenderMainWindow()
    {
        RenderDrawData(ImGui.GetDrawData());
    }

    /// <summary>
    /// Gets the geometry as set up by ImGui and sends it to the graphics device
    /// </summary>
    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);
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
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                if (_loadedTextures.Count < drawCmd.TextureId.ToInt32())
                {
                    throw new InvalidOperationException
                        ($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                }

                _graphicsDevice.ScissorRectangle = new Rectangle
                (
                    (int)(drawCmd.ClipRect.X - drawData.DisplayPos.X),
                    (int)(drawCmd.ClipRect.Y - drawData.DisplayPos.Y),
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId.ToInt32()], drawData);

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