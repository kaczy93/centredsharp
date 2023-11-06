using CentrED.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools;

public class MoveTool : Tool {
    internal MoveTool(UIManager uiManager) : base(uiManager) { }

    public override string Name => "MoveTool";

    private int _xDelta;
    private int _yDelta;
    
    private bool _pressed;
    private StaticObject _focusObject;

    internal override void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver );
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        ImGui.InputInt("X", ref _xDelta);
        ImGui.InputInt("Y", ref _yDelta);
        ImGui.End();
    }

    public override void OnMouseEnter(MapObject? o) {
        if (o is StaticObject so) {
            so.Alpha = 0.3f;
            var newTile = new StaticTile(
                so.StaticTile.Id,
                (ushort)(so.StaticTile.X + _xDelta),
                (ushort)(so.StaticTile.Y + _yDelta),
                so.StaticTile.Z,
                so.StaticTile.Hue
            );
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
                so.StaticTile.UpdatePos((ushort)(so.StaticTile.X + _xDelta), (ushort)(so.StaticTile.Y + _yDelta), so.StaticTile.Z); 
        }
        _pressed = false;
    }
}