using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools; 

public abstract class Tool {
    internal UIManager _uiManager;
    internal Tool(UIManager uiManager) {
        _uiManager = uiManager;
    }
    public abstract string Name { get; }
    
    public bool Active;
    private bool noClose = false;
    
    internal void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver );
        ImGui.Begin(Name, ref Active, ImGuiWindowFlags.NoTitleBar);
        DrawWindowInternal();        
        ImGui.End();
    }

    protected abstract void DrawWindowInternal();

    public virtual void Action(Object? selected) {
        
    }

    public virtual ushort HueOverride { get; } = 0;
    public virtual ushort IdOverride { get; } = 0;
    public virtual sbyte ZOverride { get; } = 0;
}