using CentrED.Map;
using static CentrED.Application;

namespace CentrED.Tools;

public class HueTool : Tool
{
    public override string Name => "HueTool";

    private bool _pressed;

    public override void OnActivated(TileObject? o)
    {
        CEDGame.UIManager.HuesWindow.Show = true;
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Hue = CEDGame.UIManager.HuesWindow.ActiveId;
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if(_pressed)
            Apply(o);
        if (o is StaticObject so)
        {
            so.Hue = so.StaticTile.Hue;
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_pressed)
        {
           Apply(o);
        }
        _pressed = false;
    }

    private void Apply(TileObject? o)
    {
        if (o is StaticObject so && CEDGame.UIManager.HuesWindow.SelectedId != -1)
            so.StaticTile.Hue = CEDGame.UIManager.HuesWindow.ActiveId;
    }
}