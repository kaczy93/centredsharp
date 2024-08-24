using CentrED.IO.Models;
using ImGuiNET;

namespace CentrED.UI.Windows;

public abstract class Window
{
    public Window()
    {
        if (Config.Instance.Layout.TryGetValue(Name, out var state))
        {
            Show = state.IsOpen;
        }
    }
    
    public abstract string Name { get; }

    public virtual string Shortcut => "";
    public virtual WindowState DefaultState => new();
    public virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;
    public virtual bool Enabled => true;

    protected bool _show;

    public bool Show
    {
        get => _show;
        set  {
            _show = value;
            if(_show)
                OnShow();
        }
    }

    public virtual void OnShow()
    {
        
    }

    public virtual void DrawMenuItem()
    {
        if(!Enabled)
            ImGui.BeginDisabled();
        if (ImGui.MenuItem(Name, Shortcut, ref _show))
        {
            OnShow();
        }
        if(!Enabled)
            ImGui.EndDisabled();
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