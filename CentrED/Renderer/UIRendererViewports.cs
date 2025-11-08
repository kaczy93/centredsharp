using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using static SDL3.SDL;

namespace CentrED.Renderer;

public partial class UIRenderer
{
    private RendererRenderWindow _renderWindow;
    private RendererSwapBuffers _swapBuffers;
    
    private unsafe void InitMultiViewportSupport()
    {
        ImGuiPlatformIO* platformIO = ImGui.GetPlatformIO();
        
        _renderWindow = RendererRenderWindow; //This is so GC won't clean up the delegate
        platformIO->RendererRenderWindow = (void*)Marshal.GetFunctionPointerForDelegate(_renderWindow);
        _swapBuffers = RendererSwapBuffers;
        platformIO->RendererSwapBuffers = (void*)Marshal.GetFunctionPointerForDelegate(_swapBuffers);
    }
    
    public unsafe void RendererRenderWindow(ImGuiViewport* vp, void* data)
    {
        _graphicsDevice.Clear(Color.Black);
        _graphicsDevice.Viewport = new(new Rectangle(0, 0,(int)vp->WorkSize.X, (int)vp->WorkSize.Y));
        RenderDrawData(vp->DrawData);
    }

    public unsafe void RendererSwapBuffers(ImGuiViewport* vp, void* data)
    {
        _graphicsDevice.Present(new Rectangle(0, 0, (int)vp->WorkSize.X, (int)vp->WorkSize.Y), 
                                null, 
                                SDL_GetWindowFromID((uint)vp->PlatformHandle)); 
    }
}