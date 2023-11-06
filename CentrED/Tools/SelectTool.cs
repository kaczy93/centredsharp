using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public class SelectTool : Tool {
    internal SelectTool(UIManager uiManager) : base(uiManager) { }

    public override string Name => "Select";

    public override void OnMousePressed(MapObject? selected) {
        _uiManager._infoWindow.Selected = selected;
    }

    public override void OnActivated(MapObject? o) {
        _uiManager._infoWindow.Show = true;
    }
}