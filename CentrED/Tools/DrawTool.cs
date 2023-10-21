using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;

namespace CentrED.Tools;

public class DrawTool : Tool {
    internal DrawTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }
    public override string Name => "DrawTool";

    private bool _pressed;
    private MapObject _focusObject;

    [Flags]
    enum DrawMode {
        ON_TOP = 0,
        REPLACE = 1,
        SAME_POS = 2,
    }

    private bool _withHue;
    private int _drawMode;

    internal override void DrawWindow() {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver);
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.RadioButton("Same Postion", ref _drawMode, (int)DrawMode.SAME_POS);
        ImGui.End();
    }

    public override void OnMouseEnter(MapObject? o) {
        if (o == null) return;
        ushort tileX = o.Tile.X;
        ushort tileY = o.Tile.Y;
        sbyte tileZ = o.Tile.Z;
        byte height = o switch {
            StaticObject so => TileDataLoader.Instance.StaticData[so.Tile.Id].Height,
            _ => 0
        };

        var newId = _uiManager.TilesSelectedId;
        if (_uiManager.IsLandTile(newId)) {
            if (o is LandObject lo) {
                lo.Visible = false;
                var newTile = new LandTile((ushort)newId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
                _mapManager.GhostLandTiles.Add(new LandObject(_mapManager.Client, newTile));
            }
        }
        else {
            var newZ = (DrawMode)_drawMode switch {
                DrawMode.ON_TOP => tileZ + height,
                _ => tileZ
            };

            if (o is StaticObject && (DrawMode)_drawMode == DrawMode.REPLACE) {
                o.Visible = false;
            }

            var newTile = new StaticTile(
                (ushort)(newId - UIManager.MaxLandIndex),
                tileX,
                tileY,
                (sbyte)newZ,
                (ushort)(_withHue ? _uiManager.HuesSelectedId + 1 : 0));
            _mapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }

    public override void OnMouseLeave(MapObject? o) {
        if (_uiManager.IsLandTile(_uiManager.TilesSelectedId)) {
            if (o is LandObject lo) {
                lo.Visible = true;
                _mapManager.GhostLandTiles.Clear();
            }
        }
        else {
            if (o != null)
                o.Visible = true;
            _mapManager.GhostStaticTiles.Clear();
        }
    }

    public override void OnMousePressed(MapObject? o) {
        if (_pressed || o == null) return;
        _pressed = true;
        _focusObject = o;
    }

    public override void OnMouseReleased(MapObject? o) {
        if (_pressed && o == _focusObject) {
            var newId = _uiManager.TilesSelectedId;
            if (_uiManager.IsLandTile(newId) && o is LandObject lo) {
                lo.LandTile.Id = (ushort)_uiManager.TilesSelectedId;
            }
            else {
                var newTile = _mapManager.GhostStaticTiles[0].StaticTile;
                _mapManager.Client.Add(newTile);
            }
        }
        _pressed = false;
    }
}