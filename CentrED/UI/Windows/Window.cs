using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.CentrED;

namespace CentrED.UI.Windows;

public abstract class Window
{
    public Window()
    {
        if (ConfigLegacy.Instance.Layout.TryGetValue(WindowId, out var state))
        {
            Show = state.IsOpen;
        }
    }
    
    public abstract string Name { get; }

    private string WindowId => Name.Split("###").Last();

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
        if(!ConfigLegacy.Instance.Layout.ContainsKey(WindowId))
        {
            ConfigLegacy.Instance.Layout.Add(WindowId, DefaultState);
            Show = ConfigLegacy.Instance.Layout[WindowId].IsOpen;
        }
        if (Show != ConfigLegacy.Instance.Layout[WindowId].IsOpen)
            ConfigLegacy.Instance.Layout[WindowId].IsOpen = Show;
        if (Show)
        {
            if (ImGui.Begin(Name, ref _show, WindowFlags))
            {
                InternalDraw();
                App.UIManager.AddCurrentWindowRect();
            }
            ImGui.End();
        }
    }

    protected abstract void InternalDraw();
}