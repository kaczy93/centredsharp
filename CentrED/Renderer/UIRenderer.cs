using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Renderer.ImGuiDelegates;
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

public class UIRenderer
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
    
    private readonly Platform_Window _createWindow;
    private readonly Platform_Window _destroyWindow;
    private readonly Platform_Window _showWindow;
    private readonly Platform_WindowSetVec2 _setWindowPos;
    private readonly Platform_WindowOutVec2Ptr _OutWindowPos;
    private readonly Platform_WindowSetVec2 _setWindowSize;
    private readonly Platform_WindowOutVec2Ptr _OutWindowSize;
    private readonly Platform_Window _setWindowFocus;
    private readonly Platform_WindowGetBool _getWindowFocus;
    private readonly Platform_WindowGetBool _getWindowMinimized;
    private readonly Platform_WindowSetStr _setWindowTitle;
    private readonly Platform_WindowSetFloat _setWindowAlpha;
    private readonly Platform_WindowIntPtr _renderWindow;
    private readonly Platform_WindowIntPtr _swapBuffers;

    private readonly uint _mainWindowID;
    // Event handling
    SDL_EventFilter eventFilter;
    SDL_EventFilter prevEventFilter;
    
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
        if(Config.Instance.Viewports)
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
        mainViewport.PlatformHandle = window.Handle;

        _createWindow = CreateWindow;
        _destroyWindow = DestroyWindow;
        _showWindow = ShowWindow;
        _setWindowPos = SetWindowPos;
        _OutWindowPos = GetWindowPos;
        _setWindowSize = SetWindowSize;
        _OutWindowSize = GetWindowSize;
        _setWindowFocus = SetWindowFocus;
        _getWindowFocus = GetWindowFocus;
        _getWindowMinimized = GetWindowMinimized;
        _setWindowTitle = SetWindowTitle;
        _setWindowAlpha = SetWindowAlpha;
        _renderWindow = RenderWindow;
        
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);
        platformIO.Platform_SetWindowAlpha = Marshal.GetFunctionPointerForDelegate(_setWindowAlpha);
        platformIO.Platform_RenderWindow = Marshal.GetFunctionPointerForDelegate(_renderWindow);
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_OutWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_OutWindowSize));
        io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("FNA.SDL3 Backend").DataPtr;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        // io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        _mainWindowID = SDL_GetWindowID(window.Handle);
        // Use a filter to get SDL events for your extra window
        IntPtr prevUserData;
        SDL_GetEventFilter(
            out prevEventFilter,
            out prevUserData
        );
        eventFilter = EventFilter;
        SDL_SetEventFilter(
            eventFilter,
            prevUserData
        );
    }
    
    private unsafe bool EventFilter(IntPtr userdata, SDL_Event* evt)
    {
        if (evt->type == (int)SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED &&
            evt->window.windowID != _mainWindowID)
        {
            //This event messes with Mouse.INTERNAL_WindowWidth and Mouse.INTERNAL_WindowHeight
            //Maybe we could not filter it if FNA would start handling events that targets only main GameWindow
            return false;
        }
        if (prevEventFilter != null)
        {
            return prevEventFilter(userdata, evt);
        }
        return true;
    }

    private void CreateWindow(ImGuiViewportPtr vp)
    {
        SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if ((vp.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_UTILITY;
        }
        if ((vp.Flags & ImGuiViewportFlags.NoDecoration) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
        }
        else
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        }

        if ((vp.Flags & ImGuiViewportFlags.TopMost) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
        }
            
        var window = SDL_CreateWindow(
            "No Title Yet",
            (int)vp.Size.X, (int)vp.Size.Y,
            flags);

        SDL_SetWindowPosition(window, (int)vp.Pos.X, (int)vp.Pos.Y);
        SDL_StartTextInput(window);
        
        vp.PlatformHandle = window;
    }

    private void DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformHandle != IntPtr.Zero)
        {
            SDL_DestroyWindow(vp.PlatformHandle);
            vp.PlatformHandle = IntPtr.Zero;
        }
    }

    private void ShowWindow(ImGuiViewportPtr vp)
    {
        SDL_ShowWindow(vp.PlatformHandle);
    }

    private void SetWindowPos(ImGuiViewportPtr vp, ImVec2 pos)
    {
        SDL_SetWindowPosition(vp.PlatformHandle, (int)pos.X, (int)pos.Y);
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, ImVec2* outVec)
    {
        SDL_GetWindowPosition(vp.PlatformHandle, out int x, out int y);
        *outVec = new ImVec2(x, y);
    }

    private void SetWindowSize(ImGuiViewportPtr vp, ImVec2 size)
    {
        SDL_SetWindowSize(vp.PlatformHandle, (int)size.X, (int)size.Y);
    }

    private unsafe void GetWindowSize(ImGuiViewportPtr vp, ImVec2* outVec)
    {
        SDL_GetWindowSize(vp.PlatformHandle, out int width, out int height);
        *outVec = new ImVec2(width, height);
    }

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
        SDL_RaiseWindow(vp.PlatformHandle);
    }

    private bool GetWindowFocus(ImGuiViewportPtr vp)
    {
        var flags = SDL_GetWindowFlags(vp.PlatformHandle);
        return flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);
    }

    private bool GetWindowMinimized(ImGuiViewportPtr vp)
    {
        var flags = SDL_GetWindowFlags(vp.PlatformHandle);
        return flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_MINIMIZED);
    }

    private void SetWindowTitle(ImGuiViewportPtr vp, string title)
    {
        SDL_SetWindowTitle(vp.PlatformHandle, title);
    }

    private void SetWindowAlpha(ImGuiViewportPtr vp, float alpha)
    {
        SDL_SetWindowOpacity(vp.PlatformHandle, alpha);
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
        var res = SDL_GetWindowSize(mainViewport.PlatformHandle, out var w, out var h);
        if (w > 0 && h > 0)
        {
            ImGui.GetIO().DisplaySize = new ImVec2(w, h);
            SDL_GetWindowSizeInPixels(mainViewport.PlatformHandle, out var pw, out var ph);
            ImGui.GetIO().DisplayFramebufferScale = new ImVec2(pw / (float)w, ph / (float)h);
        }
        UpdateMonitors();
    }

    public void RenderMainWindow()
    {
        RenderDrawData(ImGui.GetDrawData());
    }
    
    public void RenderWindow(ImGuiViewportPtr vp, IntPtr data)
    {
        _graphicsDevice.PresentationParameters.DeviceWindowHandle = vp.PlatformHandle;
        _graphicsDevice.Reset();
        _graphicsDevice.Clear(Color.Black);
        _graphicsDevice.Viewport = new(new Rectangle(0, 0,(int)vp.WorkSize.X, (int)vp.WorkSize.Y));
        RenderDrawData(vp.DrawData);
        _graphicsDevice.Present(new Rectangle(0, 0, (int)vp.WorkSize.X, (int)vp.WorkSize.Y), null, vp.PlatformHandle); 
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