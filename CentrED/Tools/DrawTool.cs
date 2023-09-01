using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;

namespace CentrED.Tools; 

public class DrawTool : Tool {
    internal DrawTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }
    public override string Name => "DrawTool";

    private bool _pressed;
    private StaticObject _focusObject;

    [Flags]
    enum DrawMode {
        ON_TOP = 0,
        REPLACE = 1,
        SAME_POS = 2,
    }

    private bool _withHue;
    private int _drawMode;
    
    protected override void DrawWindowInternal() {
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.RadioButton("Same Postion", ref _drawMode, (int)DrawMode.SAME_POS);
    }

    public override void OnMouseEnter(MapObject? o) {
        ushort tileX;
        ushort tileY;
        sbyte tileZ;
        byte height;
        if (o is LandObject lo) {
            tileX = lo.root.X;
            tileY = lo.root.Y;
            tileZ = lo.root.Z;
            height = 0;
        }
        else if (o is StaticObject so) {
            tileX = so.root.X;
            tileY = so.root.Y;
            tileZ = so.root.Z;
            height = TileDataLoader.Instance.StaticData[so.root.Id].Height;
        }
        else {
            return;
        }

        var newZ = (DrawMode)_drawMode switch {
            DrawMode.ON_TOP => tileZ + height,
            DrawMode.REPLACE or DrawMode.SAME_POS => tileZ
        };
        var newId = _uiManager.TilesSelectedId;
        if (_uiManager.IsLandTile(newId)) {
            
        }
        else {
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
        if(o != null)
            o.Visible = true;
        _mapManager.GhostStaticTiles.Clear();
    }

    public override void OnMousePressed(MapObject? o) {
        if (!_pressed && o is StaticObject so) {
            _pressed = true;
            _focusObject = so;
        }
    }
    
    public override void OnMouseReleased(MapObject? o) {
        if (_pressed && o is StaticObject so && so == _focusObject) {
            // _mapManager.Client.Remove(_focusObject.root);
        }
        _pressed = false;
    }
}