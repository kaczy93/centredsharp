using Hexa.NET.ImGui;
using static CentrED.CentrED;

namespace CentrED.UI.Windows;

public abstract class Window
{
    protected readonly ConfigValue<bool> IsOpen;

    protected Window(bool defaultIsOpen = false)
    {
        IsOpen = App.Config.Init($"layout.{WindowId}.open", defaultIsOpen);
    }
    
    public abstract string Name { get; }

    private string WindowId => Name.Split("###").Last();

    public virtual string Shortcut => "";
    public virtual ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;
    public virtual bool Enabled => true;

    public bool Show
    {
        get => IsOpen.Value;
        set  {
            IsOpen.Value = value;
            if(value)
                OnShow();
        }
    }

    public virtual void OnShow() { }

    public virtual void DrawMenuItem()
    {
        ImGui.BeginDisabled(!Enabled);
        if (ImGui.MenuItem(Name, Shortcut, ref IsOpen.Value))
        {
            OnShow();
        }
        ImGui.EndDisabled();
    }

    public void Draw()
    {
        if (IsOpen.Value)
        {
            if (ImGui.Begin(Name, ref IsOpen.Value, WindowFlags))
            {
                InternalDraw();
                App.UIManager.AddCurrentWindowRect();
            }
            ImGui.End();
        }
    }

    protected abstract void InternalDraw();
}