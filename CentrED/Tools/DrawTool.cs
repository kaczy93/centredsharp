using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : Tool
{
    private static readonly Random Random = new();
    public override string Name => "Draw";
    public override Keys Shortcut => Keys.F2;

    private bool _pressed;

    [Flags]
    enum DrawMode
    {
        ON_TOP = 0,
        REPLACE = 1,
        COPY_Z = 2,
        VIRTUAL_LAYER = 3,
    }

    private bool _withHue;
    private int _drawMode;
    private int _drawChance = 100;
    private bool _showVirtualLayer;

    internal override void Draw()
    {
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.PushItemWidth(50);
        UIManager.DragInt("Chance", ref _drawChance, 1, 0, 100);
        ImGui.PopItemWidth();
        UIManager.Tooltip("Double click to set specific value");
        ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.RadioButton("Copy Z", ref _drawMode, (int)DrawMode.COPY_Z);
        ImGui.RadioButton("Virtual Layer", ref _drawMode, (int)DrawMode.VIRTUAL_LAYER);
        if (ImGui.Checkbox("Show VL", ref _showVirtualLayer))
        {
            CEDGame.MapManager.ShowVirtualLayer = _showVirtualLayer;
        }
        UIManager.DragInt("Z", ref CEDGame.MapManager.VirtualLayerZ, 1, -127, 127);
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
        if (_drawMode != (int)DrawMode.VIRTUAL_LAYER )
            return;

        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.LandMode)
        {
            CEDGame.MapManager.GhostLandTiles.Clear();
            var newTile = new LandTile(tilesWindow.ActiveId, (ushort)tilePos.X , (ushort)tilePos.Y, (sbyte)tilePos.Z);
            CEDGame.MapManager.GhostLandTiles.Add(new LandObject(newTile));
        }
        else
        {
            CEDGame.MapManager.GhostStaticTiles.Clear();
            var newTile = new StaticTile(
                tilesWindow.ActiveId, 
                 (ushort)(tilePos.X + 1), 
                 (ushort)(tilePos.Y + 1), 
                 (sbyte)tilePos.Z, 
                 (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.ActiveId : 0)
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

        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.LandMode)
        {
            if (o is LandObject lo)
            {
                lo.Visible = false;
                var newTile = new LandTile(tilesWindow.ActiveId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
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
                tilesWindow.ActiveId,
                tileX,
                tileY,
                (sbyte)newZ,
                (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.ActiveId : 0)
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
        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.LandMode)
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
        if (o == null || Random.Next(100) > _drawChance) return;
        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.StaticMode)
        {
            if (CEDGame.MapManager.GhostStaticTiles.Count > 0)
            {
                var newTile = CEDGame.MapManager.GhostStaticTiles[0].StaticTile;
                if ((DrawMode)_drawMode == DrawMode.REPLACE && o is StaticObject so)
                {
                    so.StaticTile.Id = newTile.Id;
                }
                else
                {
                    CEDClient.Add(newTile);
                }
            }
        }
        else if(o is LandObject lo)
        {
            lo.Tile.Id = tilesWindow.ActiveId;
        }
    }
}