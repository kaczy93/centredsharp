using CentrED.Map;
using CentrED.UI.Windows;
using ClassicUO.Assets;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : Tool
{
    public override string Name => "DrawTool";

    private bool _pressed;
    private MapObject _focusObject;

    [Flags]
    enum DrawMode
    {
        ON_TOP = 0,
        REPLACE = 1,
        SAME_POS = 2,
    }

    private bool _withHue;
    private int _drawMode;

    internal override void DrawWindow()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver);
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.RadioButton("Same Postion", ref _drawMode, (int)DrawMode.SAME_POS);
        ImGui.End();
    }

    public override void OnMouseEnter(MapObject? o)
    {
        if (o == null)
            return;
        ushort tileX = o.Tile.X;
        ushort tileY = o.Tile.Y;
        sbyte tileZ = o.Tile.Z;
        byte height = o switch
        {
            StaticObject so => TileDataLoader.Instance.StaticData[so.Tile.Id].Height,
            _ => 0
        };

        var newId = CEDGame.UIManager.TilesWindow._selectedId;
        if (TilesWindow.IsLandTile(newId))
        {
            if (o is LandObject lo)
            {
                lo.Visible = false;
                var newTile = new LandTile((ushort)newId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
                CEDGame.MapManager.GhostLandTiles.Add(new LandObject(CEDClient, newTile));
            }
        }
        else
        {
            var newZ = (DrawMode)_drawMode switch
            {
                DrawMode.ON_TOP => tileZ + height,
                _ => tileZ
            };

            if (o is StaticObject && (DrawMode)_drawMode == DrawMode.REPLACE)
            {
                o.Visible = false;
            }

            var newTile = new StaticTile
            (
                (ushort)(newId - ArtLoader.MAX_LAND_DATA_INDEX_COUNT),
                tileX,
                tileY,
                (sbyte)newZ,
                (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.SelectedId + 1 : 0)
            );
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }

    public override void OnMouseLeave(MapObject? o)
    {
        if (TilesWindow.IsLandTile(CEDGame.UIManager.TilesWindow._selectedId))
        {
            if (o is LandObject lo)
            {
                lo.Visible = true;
                CEDGame.MapManager.GhostLandTiles.Clear();
            }
        }
        else
        {
            if (o != null)
                o.Visible = true;
            CEDGame.MapManager.GhostStaticTiles.Clear();
        }
    }

    public override void OnMousePressed(MapObject? o)
    {
        if (_pressed || o == null)
            return;
        _pressed = true;
        _focusObject = o;
    }

    public override void OnMouseReleased(MapObject? o)
    {
        if (_pressed && o == _focusObject)
        {
            var newId = CEDGame.UIManager.TilesWindow._selectedId;
            if (TilesWindow.IsLandTile(newId) && o is LandObject lo)
            {
                lo.LandTile.Id = (ushort)CEDGame.UIManager.TilesWindow._selectedId;
            }
            else
            {
                var newTile = CEDGame.MapManager.GhostStaticTiles[0].StaticTile;
                CEDGame.MapManager.Client.Add(newTile);
            }
        }
        _pressed = false;
    }
}