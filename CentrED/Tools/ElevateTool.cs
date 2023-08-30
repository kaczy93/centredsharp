using CentrED.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools; 

public class ElevateTool : Tool {
    internal ElevateTool(UIManager uiManager) : base(uiManager) { }
    
    private bool inc;
    private bool dec;
    private bool set;
    private int value;

    public override string Name => "ElevateTool";

    protected override void DrawWindowInternal() {
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
    }

    public override void OnClick(Object? selected) {
        // if(inc)
        //     so.root.Z += (sbyte)value;
        // else if (dec)
        //     so.root.Z -= (sbyte)value;
        // else {
        //     so.root.Z = (sbyte)value;
        // }
    }
}