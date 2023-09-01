using CentrED.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools; 

public class ElevateTool : Tool {
    internal ElevateTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }
    
    [Flags]
    enum ZMode {
        INC = 0,
        DEC = 1,
        SET = 2,
    }

    private int zMode;
    private int value;

    private bool _pressed;
    private StaticObject _focusObject;
    public override string Name => "ElevateTool";

    protected override void DrawWindowInternal() {
        ImGui.RadioButton("Inc", ref zMode, (int)ZMode.INC);
        ImGui.RadioButton("Dec", ref zMode, (int)ZMode.DEC);
        ImGui.RadioButton("Set", ref zMode, (int)ZMode.SET);
        
        ImGui.InputInt("Value", ref value);
    }
    
    private sbyte newZ(StaticTile tile) => (sbyte)((ZMode)zMode switch {
        ZMode.INC => tile.Z + value,
        ZMode.DEC => tile.Z - value,
        ZMode.SET => value,
        _ => throw new ArgumentOutOfRangeException()
    });
    
    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            var tile = so.root;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, newZ(tile), tile.Hue);
            _mapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }
    
    public override void OnMouseLeave(MapObject? o) {
        if (o is StaticObject so) {
            so.Alpha = 1f;
            _mapManager.GhostStaticTiles.Clear();
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
            so.root.Z = newZ(so.root);
        }
        _pressed = false;
    }
}