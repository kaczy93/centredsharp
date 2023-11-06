using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public class SelectTool : Tool {
    internal SelectTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }

    public override string Name => "Select";

    public override void OnMousePressed(MapObject? selected) {
        _uiManager._infoWindow.Selected = selected;
    }

    public override void OnActivated(MapObject? o) {
        _uiManager._infoWindow.Show = true;
    }
}