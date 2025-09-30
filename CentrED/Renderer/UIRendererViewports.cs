using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using static SDL3.SDL;
using ImVec2 = System.Numerics.Vector2;

namespace CentrED.Renderer;

public partial class UIRenderer
{
    // https://github.com/HexaEngine/Hexa.NET.ImGui/issues/9
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate ImVec2 PlatformGetWindowPosUnix(ImGuiViewport* viewport);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate ImVec2 PlatformGetWindowSizeUnix(ImGuiViewport* viewport);
    
    private RendererRenderWindow _renderWindow;
    private RendererSwapBuffers _swapBuffers;
    
    private unsafe void InitMultiViewportSupport()
    {
        ImGuiPlatformIO* platformIO = ImGui.GetPlatformIO();
        platformIO->PlatformCreateWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformCreateWindow>(CreateWindow);
        platformIO->PlatformDestroyWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformDestroyWindow>(DestroyWindow);
        platformIO->PlatformShowWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformShowWindow>(ShowWindow);
        platformIO->PlatformSetWindowPos = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowPos>(SetWindowPos);
        platformIO->PlatformSetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowSize>(SetWindowSize);
        platformIO->PlatformSetWindowFocus = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowFocus>(SetWindowFocus);
        platformIO->PlatformGetWindowFocus = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowFocus>(GetWindowFocus);
        platformIO->PlatformGetWindowMinimized = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowMinimized>(GetWindowMinimized);
        platformIO->PlatformSetWindowTitle = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowTitle>(SetWindowTitle);
        platformIO->PlatformSetWindowAlpha = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowAlpha>(SetWindowAlpha);
        
        _renderWindow = RendererRenderWindow; //This is so GC won't clean up the delegate
        platformIO->RendererRenderWindow = (void*)Marshal.GetFunctionPointerForDelegate(_renderWindow);
        _swapBuffers = RendererSwapBuffers;
        platformIO->RendererSwapBuffers = (void*)Marshal.GetFunctionPointerForDelegate(_swapBuffers);

        if (OperatingSystem.IsWindows())
        {
            platformIO->PlatformGetWindowPos = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowPos>
                (GetWindowPos);
            platformIO->PlatformGetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowSize>
                (GetWindowSize);
        }
        else
        {
            platformIO->PlatformGetWindowPos = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowPosUnix>
                (GetWindowPos_Unix);
            platformIO->PlatformGetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowSizeUnix>
                (GetWindowSize_Unix);
        }
    }
    
    private static unsafe void CreateWindow(ImGuiViewport* vp)
    {
        SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_HIDDEN;
        if ((vp->Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_UTILITY;
        }
        if ((vp->Flags & ImGuiViewportFlags.NoDecoration) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
        }
        else
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        }

        if ((vp->Flags & ImGuiViewportFlags.TopMost) != 0)
        {
            flags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
        }
            
        var window = SDL_CreateWindow(
            "No Title Yet",
            (int)vp->Size.X, (int)vp->Size.Y,
            flags);

        SDL_SetWindowPosition(window, (int)vp->Pos.X, (int)vp->Pos.Y);
        SDL_StartTextInput(window);
        
        vp->PlatformHandle = (void*)window;
    }

    private static unsafe void DestroyWindow(ImGuiViewport* vp)
    {
        if (vp->PlatformHandle != null)
        {
            SDL_DestroyWindow((IntPtr)vp->PlatformHandle);
            vp->PlatformHandle = null;
        }
    }

    private static unsafe void ShowWindow(ImGuiViewport* vp)
    {
        SDL_ShowWindow((IntPtr)vp->PlatformHandle);
    }

    private static unsafe void SetWindowPos(ImGuiViewport* vp, ImVec2 pos)
    {
        SDL_SetWindowPosition((IntPtr)vp->PlatformHandle, (int)pos.X, (int)pos.Y);
    }
    
    private static unsafe ImVec2 GetWindowPos_Unix(ImGuiViewport* vp)
    {
        SDL_GetWindowPosition((IntPtr)vp->PlatformHandle, out var x, out var y);
        return new ImVec2(x, y);
    }
    
    private static unsafe ImVec2* GetWindowPos(ImVec2* pos, ImGuiViewport* vp)
    {
        SDL_GetWindowPosition((IntPtr)vp->PlatformHandle, out var x, out var y);
        *pos = new ImVec2(x, y);
        return pos;
    }

    private static unsafe void SetWindowSize(ImGuiViewport* vp, ImVec2 size)
    {
        SDL_SetWindowSize((IntPtr)vp->PlatformHandle, (int)size.X, (int)size.Y);
    }

    private static unsafe ImVec2 GetWindowSize_Unix(ImGuiViewport* vp)
    {
        SDL_GetWindowSize((IntPtr)vp->PlatformHandle, out var width, out var height);
        return new ImVec2(width, height);
    }

    private static unsafe ImVec2* GetWindowSize(ImVec2* size, ImGuiViewport* vp)
    {
        SDL_GetWindowSize((IntPtr)vp->PlatformHandle, out var width, out var height);
        *size = new ImVec2(width, height);
        return size;
    }

    private static unsafe void SetWindowFocus(ImGuiViewport* vp)
    {
        SDL_RaiseWindow((IntPtr)vp->PlatformHandle);
    }

    private static unsafe byte GetWindowFocus(ImGuiViewport* vp)
    {
        var flags = SDL_GetWindowFlags((IntPtr)vp->PlatformHandle);
        var focused =  flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);
        return (byte)(focused ? 1 : 0);
    }

    private static unsafe byte GetWindowMinimized(ImGuiViewport* vp)
    {
        var flags = SDL_GetWindowFlags((IntPtr)vp->PlatformHandle);
        var minimized =  flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_MINIMIZED);
        return (byte)(minimized ? 1 : 0);
    }

    private static unsafe void SetWindowTitle(ImGuiViewport* vp, byte* title)
    {
        var stringTitle = Marshal.PtrToStringAnsi((IntPtr)title);
        SDL_SetWindowTitle((IntPtr)vp->PlatformHandle, stringTitle);
    }

    private static unsafe void SetWindowAlpha(ImGuiViewport* vp, float alpha)
    {
        SDL_SetWindowOpacity((IntPtr)vp->PlatformHandle, alpha);
    }
    
    public unsafe void RendererRenderWindow(ImGuiViewport* vp, void* data)
    {
        _graphicsDevice.Clear(Color.Black);
        _graphicsDevice.Viewport = new(new Rectangle(0, 0,(int)vp->WorkSize.X, (int)vp->WorkSize.Y));
        RenderDrawData(vp->DrawData);
    }

    public unsafe void RendererSwapBuffers(ImGuiViewport* vp, void* data)
    {
        _graphicsDevice.Present(new Rectangle(0, 0, (int)vp->WorkSize.X, (int)vp->WorkSize.Y), null, (IntPtr)vp->PlatformHandle); 
    }
}