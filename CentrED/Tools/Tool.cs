using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools; 

public abstract class Tool {
    internal UIManager _uiManager;
    internal Tool(UIManager uiManager) {
        _uiManager = uiManager;
    }
    public abstract string Name { get; }
    
    internal void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver );
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        DrawWindowInternal();        
        ImGui.End();
    }

    protected abstract void DrawWindowInternal();

    public virtual void OnActivated(Object? o) {
        
    }

    public virtual void OnDeactivated(Object? o) {
        
    }

    public virtual void OnMouseEnter(Object? o) {
        
    }

    public virtual void OnMouseLeave(Object? o) {
        
    }

    public virtual void OnClick(Object? o) {
        
    }
}