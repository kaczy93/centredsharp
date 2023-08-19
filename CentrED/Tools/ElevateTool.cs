using CentrED.Map;
using ImGuiNET;

namespace CentrED.Tools; 

public class ElevateTool : Tool {
    private static ElevateTool? _instance;
    public static ElevateTool Instance => _instance ?? (_instance = new ElevateTool());
    
    private bool inc;
    private bool dec;
    private bool set;
    private int value;
    
    public override void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 200), ImGuiCond.FirstUseEver);
        ImGui.Begin("ElevateTool", ref Active);
        if (ImGui.RadioButton("Inc", inc)) {
            inc = true;
            dec = false;
            set = false;
        }
        if (ImGui.RadioButton("Dec", dec)) {
            inc = false;
            dec = true;
            set = false;
        }
        if (ImGui.RadioButton("Set", set)) {
            inc = false;
            dec = false;
            set = true;
        }

        ImGui.InputInt("Value", ref value);
        ImGui.End();
    }

    public override void Action(StaticObject so) {
        if(inc)
            so.root.Z += (sbyte)value;
        else if (dec)
            so.root.Z -= (sbyte)value;
        else {
            so.root.Z = (sbyte)value;
        }
    }
}