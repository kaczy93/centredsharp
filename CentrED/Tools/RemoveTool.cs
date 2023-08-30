using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public class RemoveTool : Tool {
    internal RemoveTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }
    public override string Name => "RemoveTool";

    private bool _pressed;
    private StaticObject _focusObject;
    
    protected override void DrawWindowInternal() {
    }

    public override void OnMouseEnter(object? o) {
        if (o is StaticObject so) {
            so.Alpha = 0.2f;
        }
    }

    public override void OnMouseLeave(object? o) {
        if (o is StaticObject so) {
            so.Alpha = 1.0f;
        }
    }

    public override void OnMousePressed(object? o) {
        if (!_pressed && o is StaticObject so) {
            _pressed = true;
            _focusObject = so;
        }
    }
    
    public override void OnMouseReleased(object? o) {
        if (_pressed && o is StaticObject so && so == _focusObject) {
            _mapManager.Client.Remove(_focusObject.root);
        }
        _pressed = false;
    }
}