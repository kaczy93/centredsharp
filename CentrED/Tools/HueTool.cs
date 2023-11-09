using CentrED.Map;
using static CentrED.Application;

namespace CentrED.Tools;

public class HueTool : Tool {

    public override string Name => "HueTool";
    
    private bool _pressed;
    private StaticObject _focusObject;

    public override void OnActivated(MapObject? o) {
        CEDGame.UIManager._huesWindow.Show = true;
    }

    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            so.Hue = (ushort)CEDGame.UIManager._huesWindow.SelectedId;
        }
    }
    
    public override void OnMouseLeave(MapObject? o) {
        if (o is StaticObject so) {
            so.Hue = so.StaticTile.Hue;
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
            if(CEDGame.UIManager._huesWindow.SelectedId != -1)
                so.StaticTile.Hue = (ushort)CEDGame.UIManager._huesWindow.SelectedId;
        }
        _pressed = false;
    }
}