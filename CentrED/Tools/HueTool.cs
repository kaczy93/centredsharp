using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools;

public class HueTool : Tool {
    internal HueTool(UIManager uiManager) : base(uiManager) { }

    public override string Name => "HueTool";

    protected override void DrawWindowInternal() {
        // ImGui.InputInt("Hue id", ref selectedHue);
    }

    public override void OnMouseEnter(Object? o) {
        if (o is StaticObject so) {
            so.HueOverride = (short)_uiManager.HuesSelectedId;
        }
    }
    
    public override void OnMouseLeave(Object? o) {
        if (o is StaticObject so) {
            so.HueOverride = -1;
        }
    }
    
    public override void OnClick(Object? o) {
        if (o is StaticObject so) {
            var hueId = _uiManager.HuesSelectedId;
            if(hueId != -1)
                so.root.Hue = (ushort)hueId;
        }
    }
}