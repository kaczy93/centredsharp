using CentrED.Map;
using static CentrED.Application;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => "Select";

    public override void OnMousePressed(MapObject? selected)
    {
        CEDGame.UIManager.InfoWindow.Selected = selected;
    }

    public override void OnActivated(MapObject? o)
    {
        CEDGame.UIManager.InfoWindow.Show = true;
    }
}