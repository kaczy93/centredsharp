using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SDL2.SDL;

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
    
    private readonly List<IntPtr> _windows = new(); //Hoard windows so GC won't harrass them
    private readonly Platform_CreateWindow _createWindow;
    private readonly Platform_DestroyWindow _destroyWindow;
    private readonly Platform_GetWindowPos _getWindowPos;
    private readonly Platform_ShowWindow _showWindow;
    private readonly Platform_SetWindowPos _setWindowPos;
    private readonly Platform_SetWindowSize _setWindowSize;
    private readonly Platform_GetWindowSize _getWindowSize;
    private readonly Platform_SetWindowFocus _setWindowFocus;
    private readonly Platform_GetWindowFocus _getWindowFocus;
    private readonly Platform_GetWindowMinimized _getWindowMinimized;
    private readonly Platform_SetWindowTitle _setWindowTitle;

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
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        ImGuiViewportPtr mainViewport = platformIO.Viewports[0];
        mainViewport.PlatformHandle = window.Handle;
        _windows.Add(window.Handle);

        Platform_CreateWindow _createWindow = CreateWindow;
        _destroyWindow = DestroyWindow;
        _getWindowPos = GetWindowPos;
        _showWindow = ShowWindow;
        _setWindowPos = SetWindowPos;
        _setWindowSize = SetWindowSize;
        _getWindowSize = GetWindowSize;
        _setWindowFocus = SetWindowFocus;
        _getWindowFocus = GetWindowFocus;
        _getWindowMinimized = GetWindowMinimized;
        _setWindowTitle = SetWindowTitle;
        platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos
            (platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize
            (platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));
        unsafe
        {
            io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("Veldrid.SDL2 Backend").DataPtr;
        }
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
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
    
    public readonly SDL_WindowEventID[] IgnoredEvents = new SDL_WindowEventID[]
    {
        SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN,
        SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED,
        SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED,
        SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED,
        SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED,
        SDL_WindowEventID.SDL_WINDOWEVENT_ENTER,
        SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE,
        SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED,
        SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST,
    };
    
    private unsafe int EventFilter(IntPtr userdata, IntPtr evtPtr)
    {
        SDL_Event* evt = (SDL_Event*) evtPtr;
        if (evt->type == SDL_EventType.SDL_WINDOWEVENT)
        {
            if (evt->window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE && evt->window.windowID == _mainWindowID)
            {
                // Lazy hack, just exit when any window is closed
                Application.CEDGame.Exit();
                return 0;
            }
            else if (evt->window.windowID != _mainWindowID && !IgnoredEvents.Contains(evt->window.windowEvent))
            {
                // Filter these out so Game doesn't get weird
                return 0;
            }
        }
        if (prevEventFilter != null)
        {
            return prevEventFilter(userdata, evtPtr);
        }
        return 1;
    }

    private void CreateWindow(ImGuiViewportPtr vp)
    {
        SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if ((vp.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_SKIP_TASKBAR;
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
            (int)vp.Pos.X, (int)vp.Pos.Y,
            (int)vp.Size.X, (int)vp.Size.Y,
            flags);
        
        _windows.Add(window);
        vp.PlatformUserData = window;
    }

    private void DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformUserData != IntPtr.Zero)
        {
            _windows.Remove(vp.PlatformUserData);
            SDL_DestroyWindow(vp.PlatformUserData);
        
            vp.PlatformUserData = IntPtr.Zero;
        }
    }

    private void ShowWindow(ImGuiViewportPtr vp)
    {
        SDL_ShowWindow(vp.PlatformUserData);
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, System.Numerics.Vector2* outPos)
    {
        SDL_GetWindowBordersSize(vp.PlatformUserData, out int top, out int left, out int _, out int _);
        *outPos = new System.Numerics.Vector2(top, left);
    }

    private void SetWindowPos(ImGuiViewportPtr vp, System.Numerics.Vector2 pos)
    {
        SDL_SetWindowPosition(vp.PlatformUserData, (int)pos.X, (int)pos.Y);
    }

    private void SetWindowSize(ImGuiViewportPtr vp, System.Numerics.Vector2 size)
    {
        SDL_SetWindowSize(vp.PlatformUserData, (int)size.X, (int)size.Y);
    }

    private unsafe void GetWindowSize(ImGuiViewportPtr vp, System.Numerics.Vector2* outSize)
    {
        SDL_GetWindowSize(vp.PlatformUserData, out int width, out int height);
        *outSize = new System.Numerics.Vector2(width, height);
    }

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
        SDL_RaiseWindow(vp.PlatformUserData);
    }

    private byte GetWindowFocus(ImGuiViewportPtr vp)
    {
        var flags = (SDL_WindowFlags)SDL_GetWindowFlags(vp.PlatformUserData);
        return (flags & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0 ? (byte)1 : (byte)0;
    }

    private byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
        var flags = (SDL_WindowFlags)SDL_GetWindowFlags(vp.PlatformUserData);
        return (flags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0 ? (byte)1 : (byte)0;
    }

    private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
        byte* titlePtr = (byte*)title;
        int count = 0;
        while (titlePtr[count] != 0)
        {
            count += 1;
        }
        SDL_SetWindowTitle(vp.PlatformUserData, System.Text.Encoding.ASCII.GetString(titlePtr, count));
    }
    
    public unsafe void UpdateMonitors()
    {
        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
        int numMonitors =  SDL_GetNumVideoDisplays();
        IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
        platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
        for (int i = 0; i < numMonitors; i++)
        {
            SDL_GetDisplayUsableBounds(i, out var r);
            ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];
            monitor.DpiScale = 1f;
            monitor.MainPos = new System.Numerics.Vector2(r.x, r.y);
            monitor.MainSize = new System.Numerics.Vector2(r.w, r.h);
            monitor.WorkPos = new System.Numerics.Vector2(r.x, r.y);
            monitor.WorkSize = new System.Numerics.Vector2(r.w, r.h);
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
        _effect = _effect ?? new BasicEffect(_graphicsDevice);

        var io = ImGui.GetIO();

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

    public void Render()
    {
        RenderDrawData(ImGui.GetDrawData());
    }

    public void RenderOtherWindows()
    {
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
            for (int i = 1; i < platformIO.Viewports.Size; i++)
            {
                ImGuiViewportPtr vp = platformIO.Viewports[i];
                IntPtr window = vp.PlatformUserData;
                SDL_GetWindowSize(window, out var wx, out var wy);
                _graphicsDevice.Clear(Color.Black);
                var bounds = new Rectangle(0, 0, wx, wy);
                _graphicsDevice.Viewport = new Viewport(bounds);
                RenderDrawData(vp.DrawData);
                _graphicsDevice.Present(bounds, null, window);

            }
        }
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

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                    _graphicsDevice.DrawIndexedPrimitives
                    (
                        PrimitiveType.TriangleList,
                        (int)drawCmd.VtxOffset + vtxOffset,
                        0,
                        cmdList.VtxBuffer.Size,
                        (int)drawCmd.IdxOffset + idxOffset,
                        (int)drawCmd.ElemCount / 3
                    );
#pragma warning restore CS0618
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }
}