using CentrED.Map;
using static CentrED.Application;

namespace CentrED.Tools;

public class HueTool : Tool
{
    public override string Name => "HueTool";

    private bool _pressed;
    private StaticObject _focusObject;

    public override void OnActivated(TileObject? o)
    {
        CEDGame.UIManager.HuesWindow.Show = true;
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Hue = (ushort)CEDGame.UIManager.HuesWindow.SelectedId;
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Hue = so.StaticTile.Hue;
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        if (!_pressed && o is StaticObject so)
        {
            _pressed = true;
            _focusObject = so;
        }
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_pressed && o is StaticObject so && so == _focusObject)
        {
            if (CEDGame.UIManager.HuesWindow.SelectedId != -1)
                so.StaticTile.Hue = (ushort)CEDGame.UIManager.HuesWindow.SelectedId;
        }
        _pressed = false;
    }
}