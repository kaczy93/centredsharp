using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public class SelectTool : Tool {
    internal SelectTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }

    public override string Name => "Select";

    public override void OnMousePressed(MapObject? selected) {
        _uiManager.InfoSelectedTile = selected;
    }

    public override void OnActivated(MapObject? o) {
        _uiManager.InfoShowWindow = true;
    }
}