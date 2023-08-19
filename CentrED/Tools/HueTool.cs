using CentrED.Map;
using ImGuiNET;

namespace CentrED.Tools; 

public class HueTool : Tool {
    private static HueTool? _instance;
    public static HueTool Instance => _instance ?? (_instance = new HueTool());
    
    private HueTool(){}
    
    private int selectedHue;
    
    public override void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver);
        ImGui.Begin("HueTool", ref Active);
        ImGui.InputInt("Hue id", ref selectedHue);
        ImGui.End();
    }

    public override void Action(StaticObject so) {
        so.root.Hue = (ushort)selectedHue;
    }
}