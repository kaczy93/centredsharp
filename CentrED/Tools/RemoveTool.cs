using CentrED.Map;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class RemoveTool : Tool
{
    public override string Name => "RemoveTool";
    public override Keys Shortcut => Keys.F5;

    private bool _pressed;

    public override void OnMouseEnter(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 0.2f;
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 1.0f;
        }
        if (_pressed)
        {
            Apply(o);
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
        if(o is StaticObject so)
            CEDClient.Remove(so.StaticTile);
    }
}