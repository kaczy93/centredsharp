using CentrED.IO.Models;
using ImGuiNET;

namespace CentrED.UI.Windows;

public abstract class Window
{
    public abstract string Name { get; }

    public virtual string Shortcut => "";
    public virtual WindowState DefaultState => new();
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
        if(!Config.Instance.Layout.ContainsKey(Name))
        {
            Config.Instance.Layout.Add(Name, DefaultState);
            Show = Config.Instance.Layout[Name].IsOpen;
        }
        if (Show != Config.Instance.Layout[Name].IsOpen)
            Config.Instance.Layout[Name].IsOpen = Show;
        if (Show)
        {
            ImGui.Begin(Name, ref _show, WindowFlags);
            InternalDraw();
            ImGui.End();
        }
    }

    protected abstract void InternalDraw();
}