using CentrED.Map;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools; 

public class ElevateTool : Tool {
    [Flags]
    enum ZMode {
        INC = 0,
        DEC = 1,
        SET = 2,
    }

    private int zMode;
    private int value;

    private bool _pressed;
    private MapObject _focusObject;
    public override string Name => "ElevateTool";

    internal override void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver );
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        ImGui.RadioButton("Inc", ref zMode, (int)ZMode.INC);
        ImGui.RadioButton("Dec", ref zMode, (int)ZMode.DEC);
        ImGui.RadioButton("Set", ref zMode, (int)ZMode.SET);
        
        ImGui.InputInt("Value", ref value);
        ImGui.End();
    }
    
    private sbyte NewZ(BaseTile tile) => (sbyte)((ZMode)zMode switch {
        ZMode.INC => tile.Z + value,
        ZMode.DEC => tile.Z - value,
        ZMode.SET => value,
        _ => throw new ArgumentOutOfRangeException()
    });
    
    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            var tile = so.StaticTile;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        } else if (o is LandObject lo) {
            var tile = lo.LandTile;
            lo.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, NewZ(tile));
            CEDGame.MapManager.GhostLandTiles.Add(new LandObject(CEDGame.MapManager.Client, newTile));
        }
    }
    
    public override void OnMouseLeave(MapObject? o) {
        if (o is StaticObject so) {
            so.Alpha = 1f;
            CEDGame.MapManager.GhostStaticTiles.Clear();
        } else if (o is LandObject lo) {
            lo.Visible = true;
            CEDGame.MapManager.GhostLandTiles.Clear();
        }
    }

    public override void OnMousePressed(MapObject? o) {
        if (!_pressed && o != null) {
            _pressed = true;
            _focusObject = o;
        }
    }
    
    public override void OnMouseReleased(MapObject? o) {
        if (_pressed && o == _focusObject) {
            o.Tile.Z = NewZ(o.Tile);
        }
        _pressed = false;
    }
}