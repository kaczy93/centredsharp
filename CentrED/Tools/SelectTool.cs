using CentrED.Map;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => "Select";
    public override Keys Shortcut => Keys.F1;

    private bool _pressed;
    
    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
        OnMouseEnter(o);
    }

    public override void OnMouseReleased(TileObject? o)
    {
        _pressed = false;
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (_pressed)
        {
            CEDGame.UIManager.InfoWindow.Selected = o;
            if (o is StaticObject)
            {
                CEDGame.UIManager.TilesWindow.SelectedStaticId = o.Tile.Id;
            }
            else if (o is LandObject)
            {
                CEDGame.UIManager.TilesWindow.SelectedLandId = o.Tile.Id;
            }
        }
    }

    public override void OnActivated(TileObject? o)
    {
        CEDGame.UIManager.InfoWindow.Show = true;
    }
}