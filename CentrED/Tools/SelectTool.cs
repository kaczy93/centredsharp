using CentrED.Map;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => "Select";
    public override Keys Shortcut => Keys.F1;

    public override void OnMouseClicked(TileObject? o)
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

    public override void OnActivated(TileObject? o)
    {
        CEDGame.UIManager.InfoWindow.Show = true;
    }
}