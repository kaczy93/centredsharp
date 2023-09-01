using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools;

public class HueTool : Tool {
    internal HueTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }

    public override string Name => "HueTool";
    
    private bool _pressed;
    private StaticObject _focusObject;
    
    protected override void DrawWindowInternal() {
        // ImGui.InputInt("Hue id", ref selectedHue);
    }

    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            so.HueOverride = _uiManager.HuesSelectedId + 1;
        }
    }
    
    public override void OnMouseLeave(MapObject? o) {
        if (o is StaticObject so) {
            so.HueOverride = -1;
        }
    }

    public override void OnMousePressed(MapObject? o) {
        if (!_pressed && o is StaticObject so) {
            _pressed = true;
            _focusObject = so;
        }
    }
    
    public override void OnMouseReleased(MapObject? o) {
        if (_pressed && o is StaticObject so && so == _focusObject) {
            if(_uiManager.HuesSelectedId != -1)
                so.root.Hue = (ushort)(_uiManager.HuesSelectedId + 1);
        }
        _pressed = false;
    }
}