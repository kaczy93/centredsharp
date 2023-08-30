using CentrED.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools; 

public abstract class Tool {
    internal UIManager _uiManager;
    internal MapManager _mapManager;
    internal Tool(UIManager uiManager, MapManager mapManager) {
        _uiManager = uiManager;
        _mapManager = mapManager;
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

    public virtual void OnMousePressed(Object? o) {
        
    }

    public virtual void OnMouseReleased(Object? o) {
        
    }
}