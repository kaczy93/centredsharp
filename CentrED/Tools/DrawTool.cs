using CentrED.Map;
using CentrED.UI.Windows;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : Tool
{
    public override string Name => "DrawTool";

    private bool _pressed;

    [Flags]
    enum DrawMode
    {
        ON_TOP = 0,
        REPLACE = 1,
        SAME_POS = 2,
        VIRTUAL_LAYER = 3,
    }

    private bool _withHue;
    private int _drawMode;
    private bool _showVirtualLayer;

    internal override void DrawWindow()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver);
        ImGui.Begin(Name, ImGuiWindowFlags.NoTitleBar);
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.RadioButton("Same Postion", ref _drawMode, (int)DrawMode.SAME_POS);
        if (ImGui.RadioButton("Virtual Layer", ref _drawMode, (int)DrawMode.VIRTUAL_LAYER))
        {
            CEDGame.MapManager.ShowVirtualLayer = _showVirtualLayer;
        }
        if (_drawMode == (int)DrawMode.VIRTUAL_LAYER)
        {
            if (ImGui.Checkbox("Show", ref _showVirtualLayer))
            {
                CEDGame.MapManager.ShowVirtualLayer = _showVirtualLayer;
            }
            ImGui.SliderInt("Z", ref CEDGame.MapManager.VirtualLayerZ, -127, 127);
            var point = CEDGame.MapManager.VirtualLayerTilePos;
            ImGui.Text($"Mouse pos on VL: {point.X} {point.Y}, {point.Z}");
        }
        else
        {
            CEDGame.MapManager.ShowVirtualLayer = false;
        }
        ImGui.End();
    }

    public override void OnActivated(TileObject? o)
    {
        if (_drawMode == (int)DrawMode.VIRTUAL_LAYER)
        {
            CEDGame.MapManager.ShowVirtualLayer = _showVirtualLayer;
        }
    }

    public override void OnDeactivated(TileObject? o)
    {
        CEDGame.MapManager.ShowVirtualLayer = false;
    }

    public override void OnVirtualLayerTile(Vector3 tilePos)
    {
        if (_drawMode != (int)DrawMode.VIRTUAL_LAYER)
            return;
        
        
        var newId = CEDGame.UIManager.TilesWindow.SelectedId;
        if (TilesWindow.IsLandTile(newId))
        {
            CEDGame.MapManager.GhostLandTiles.Clear();
            var newTile = new LandTile((ushort)newId, (ushort)tilePos.X , (ushort)tilePos.Y, (sbyte)tilePos.Z);
            CEDGame.MapManager.GhostLandTiles.Add(new LandObject(newTile));
        }
        else
        {
            CEDGame.MapManager.GhostStaticTiles.Clear();
            var newTile = new StaticTile(
                (ushort)(newId - TilesWindow.MaxLandIndex), 
                 (ushort)(tilePos.X + 1), 
                 (ushort)(tilePos.Y + 1), 
                 (sbyte)tilePos.Z, 
                 (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.SelectedId : 0)
            );
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (o == null || _drawMode == (int)DrawMode.VIRTUAL_LAYER)
            return;
        ushort tileX = o.Tile.X;
        ushort tileY = o.Tile.Y;
        sbyte tileZ = o.Tile.Z;
        byte height = o switch
        {
            StaticObject so => TileDataLoader.Instance.StaticData[so.Tile.Id].Height,
            _ => 0
        };

        var newId = CEDGame.UIManager.TilesWindow.SelectedId;
        if (TilesWindow.IsLandTile(newId))
        {
            if (o is LandObject lo)
            {
                lo.Visible = false;
                var newTile = new LandTile((ushort)newId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
                CEDGame.MapManager.GhostLandTiles.Add(new LandObject(newTile));
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
                o.Alpha = 0.3f;
            }

            var newTile = new StaticTile
            (
                (ushort)(newId - TilesWindow.MaxLandIndex),
                tileX,
                tileY,
                (sbyte)newZ,
                (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.SelectedId : 0)
            );
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if (_pressed)
        {
            Apply(o);
        }
        if (TilesWindow.IsLandTile(CEDGame.UIManager.TilesWindow.SelectedId))
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
                o.Alpha = 1f;
            CEDGame.MapManager.GhostStaticTiles.Clear();
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_pressed)
        {
            Apply(o);
        }
        _pressed = false;
    }

    private void Apply(TileObject? o)
    {
        if (o == null) return;
        var newId = CEDGame.UIManager.TilesWindow.SelectedId;
        if (TilesWindow.IsLandTile(newId) && o is LandObject lo)
        {
            lo.LandTile.Id = (ushort)CEDGame.UIManager.TilesWindow.SelectedId;
        }
        else
        {
            var newTile = CEDGame.MapManager.GhostStaticTiles[0].StaticTile;
            CEDGame.MapManager.Client.Add(newTile);
        }
    }
}