using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public class RemoveTool : Tool {
    internal RemoveTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }
    public override string Name => "RemoveTool";

    private bool _pressed;
    private StaticObject _focusObject;
    
    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            so.Alpha = 0.2f;
        }
    }

    public override void OnMouseLeave(MapObject? o) {
        if (o is StaticObject so) {
            so.Alpha = 1.0f;
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
            _mapManager.Client.Remove(_focusObject.StaticTile);
        }
        _pressed = false;
    }
}