using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Renderer.ImGuiDelegates;
using static SDL3.SDL;
using ImVec2 = System.Numerics.Vector2;

namespace CentrED.Renderer;

public partial class UIRenderer
{
    private Platform_Window _createWindow;
    private Platform_Window _destroyWindow;
    private Platform_Window _showWindow;
    private Platform_WindowSetVec2 _setWindowPos;
    private Platform_WindowOutVec2Ptr _OutWindowPos;
    private Platform_WindowSetVec2 _setWindowSize;
    private Platform_WindowOutVec2Ptr _OutWindowSize;
    private Platform_Window _setWindowFocus;
    private Platform_WindowGetBool _getWindowFocus;
    private Platform_WindowGetBool _getWindowMinimized;
    private Platform_WindowSetStr _setWindowTitle;
    private Platform_WindowSetFloat _setWindowAlpha;
    private Platform_WindowIntPtr _renderWindow;
    private Platform_WindowIntPtr _swapBuffers;
    
    private unsafe void InitMultiViewportSupport(GameWindow window)
    {
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
    
    public void RenderWindow(ImGuiViewportPtr vp, IntPtr data)
    {
        _graphicsDevice.PresentationParameters.DeviceWindowHandle = vp.PlatformHandle;
        _graphicsDevice.Reset();
        _graphicsDevice.Clear(Color.Black);
        _graphicsDevice.Viewport = new(new Rectangle(0, 0,(int)vp.WorkSize.X, (int)vp.WorkSize.Y));
        RenderDrawData(vp.DrawData);
        _graphicsDevice.Present(new Rectangle(0, 0, (int)vp.WorkSize.X, (int)vp.WorkSize.Y), null, vp.PlatformHandle); 
    }
}