using ImGuiNET;

namespace CentrED.UI.Windows; 

public abstract class Window {
    protected UIManager _uiManager;
    internal Window(UIManager uiManager) {
        _uiManager = uiManager;
    }
    public abstract string Name {
        get;
    }

    public virtual string Shortcut => "";

    protected bool _show;
    public bool Show => _show;

    public virtual void DrawMenuItem() {
        ImGui.MenuItem(Name, Shortcut, ref _show);
    }
    
    public abstract void Draw();
}