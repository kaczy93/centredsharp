using System.Numerics;
using ImGuiNET;

namespace CentrED.Renderer;

public class ImGuiDelegates
{
    //We maintain our own delegats since imgui.net ones are not complete
    public delegate void Platform_Window(ImGuiViewportPtr vp);
    public delegate Vector2 Platform_WindowGetVec2(ImGuiViewportPtr vp);
    public delegate void Platform_WindowSetVec2(ImGuiViewportPtr vp, Vector2 vec);
    public delegate void Platform_WindowSetStr(ImGuiViewportPtr vp, string str);
    public delegate bool Platform_WindowGetBool(ImGuiViewportPtr vp);
    public delegate void Platform_WindowSetFloat(ImGuiViewportPtr vp, float f);
    public delegate void Platform_WindowIntPtr(ImGuiViewportPtr vp, IntPtr data);
}