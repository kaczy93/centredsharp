using ImGuiNET;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace CentrED.Renderer;

public class ImGuiWindow : IDisposable
{
        private readonly GCHandle _gcHandle;
        private readonly GraphicsDevice _gd;
        private readonly ImGuiViewportPtr _vp;
        private readonly IntPtr _window;

        public IntPtr Window => _window;

        public ImGuiWindow(GraphicsDevice gd, ImGuiViewportPtr vp)
        {
            _gcHandle = GCHandle.Alloc(this);
            _gd = gd;
            _vp = vp;

            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN;
            if ((vp.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0)
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_SKIP_TASKBAR;
            }
            if ((vp.Flags & ImGuiViewportFlags.NoDecoration) != 0)
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }
            else
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
            }

            if ((vp.Flags & ImGuiViewportFlags.TopMost) != 0)
            {
                flags |= SDL.SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
            }
            
            _window = SDL.SDL_CreateWindow(
                "No Title Yet",
                (int)vp.Pos.X, (int)vp.Pos.Y,
                (int)vp.Size.X, (int)vp.Size.Y,
                flags);
            
            // _window.Resized += () => _vp.PlatformRequestResize = true;
            // _window.Moved += p => _vp.PlatformRequestMove = true;
            // _window.Closed += () => _vp.PlatformRequestClose = true;
            // _window.ClientSizeChanged += (object? sender, EventArgs e) => _sc.Resize((uint)_window.Width, (uint)_window.Height);
            
            vp.PlatformUserData = (IntPtr)_gcHandle;
        }
        
        public ImGuiWindow(GraphicsDevice gd, ImGuiViewportPtr vp, GameWindow window)
        {
            _gcHandle = GCHandle.Alloc(this);
            _gd = gd;
            _vp = vp;
            _window = window.Handle;
            vp.PlatformUserData = (IntPtr)_gcHandle;
        }

        public void Dispose()
        {
            // _gd.WaitForIdle(); // TODO: Shouldn't be necessary, but Vulkan backend trips a validation error (swapchain in use when disposed).
            SDL.SDL_DestroyWindow(Window);
            _gcHandle.Free();
        }
}