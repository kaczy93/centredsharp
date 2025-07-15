using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using static SDL3.SDL;
using ImVec2 = System.Numerics.Vector2;

namespace CentrED.Renderer;

public partial class UIRenderer
{
    private PlatformRenderWindow _renderWindow;
    private unsafe void InitMultiViewportSupport(GameWindow window)
    {
        ImGuiPlatformIO* platformIO = ImGui.GetPlatformIO();
        platformIO->PlatformCreateWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformCreateWindow>(CreateWindow);
        platformIO->PlatformDestroyWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformDestroyWindow>(DestroyWindow);
        platformIO->PlatformShowWindow = (void*)Marshal.GetFunctionPointerForDelegate<PlatformShowWindow>(ShowWindow);
        platformIO->PlatformSetWindowPos = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowPos>(SetWindowPos);
        platformIO->PlatformGetWindowPos = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowPos>(GetWindowPos);
        platformIO->PlatformSetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowSize>(SetWindowSize);
        platformIO->PlatformGetWindowSize = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowSize>(GetWindowSize);
        platformIO->PlatformSetWindowFocus = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowFocus>(SetWindowFocus);
        platformIO->PlatformGetWindowFocus = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowFocus>(GetWindowFocus);
        platformIO->PlatformGetWindowMinimized = (void*)Marshal.GetFunctionPointerForDelegate<PlatformGetWindowMinimized>(GetWindowMinimized);
        platformIO->PlatformSetWindowTitle = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowTitle>(SetWindowTitle);
        platformIO->PlatformSetWindowAlpha = (void*)Marshal.GetFunctionPointerForDelegate<PlatformSetWindowAlpha>(SetWindowAlpha);
        _renderWindow = RenderWindow;
        platformIO->PlatformRenderWindow = (void*)Marshal.GetFunctionPointerForDelegate(_renderWindow);
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
    
    public unsafe void RenderWindow(ImGuiViewport* vp, void* data)
    {
        _graphicsDevice.PresentationParameters.DeviceWindowHandle = (IntPtr)vp->PlatformHandle;
        _graphicsDevice.Reset();
        _graphicsDevice.Clear(Color.Black);
        _graphicsDevice.Viewport = new(new Rectangle(0, 0,(int)vp->WorkSize.X, (int)vp->WorkSize.Y));
        RenderDrawData(vp->DrawData);
        _graphicsDevice.Present(new Rectangle(0, 0, (int)vp->WorkSize.X, (int)vp->WorkSize.Y), null, (IntPtr)vp->PlatformHandle); 
    }
}