using CentrED.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools;

public class HueTool : Tool {
    internal HueTool(UIManager uiManager) : base(uiManager) { }

    private int selectedHue;

    public override ushort HueOverride => (ushort)selectedHue;

    public override string Name => "HueTool";

    protected override void DrawWindowInternal() {
        ImGui.InputInt("Hue id", ref selectedHue);
    }

    public override void Action(Object? selected) {
        if (selected is StaticTile tile) {
            tile.Hue = (ushort)selectedHue;
        }
    }
}