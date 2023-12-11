using CentrED.IO.Models;
using ImGuiNET;

namespace CentrED.UI.Windows;

public abstract class Window
{
    public Window()
    {
        if(Config.Layout.TryGetValue(Name, out var state))
            Show = state.IsOpen;
        else
        {
            Config.Layout.Add(Name, new WindowState());
        }
    }
    public abstract string Name { get; }

    public virtual string Shortcut => "";
    public virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    protected bool _show;

    public bool Show
    {
        get => _show;
        set => _show = value;
    }

    public virtual void DrawMenuItem()
    {
        
        ImGui.MenuItem(Name, Shortcut, ref _show);
    }

    public void Draw()
    {
        if (Show != Config.Layout[Name].IsOpen)
            Config.Layout[Name].IsOpen = Show;
        if (Show)
        {
            ImGui.Begin(Name, ref _show, WindowFlags);
            InternalDraw();
            ImGui.End();
        }
    }

    protected abstract void InternalDraw();
}