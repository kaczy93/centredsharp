using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : BaseTool
{
    private static readonly Random Random = new();
    public override string Name => "Draw";
    public override Keys Shortcut => Keys.F2;

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
        if (ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP) || 
            ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE) ||
            ImGui.RadioButton("Copy Z", ref _drawMode, (int)DrawMode.COPY_Z))
        {
            CEDGame.MapManager.UseVirtualLayer = false;
        }
        if (ImGui.RadioButton("Virtual Layer", ref _drawMode, (int)DrawMode.VIRTUAL_LAYER))
        {
            CEDGame.MapManager.UseVirtualLayer = true;
        }
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
            CEDGame.MapManager.UseVirtualLayer = _drawMode == (int)DrawMode.VIRTUAL_LAYER;
        }
    }

    public override void OnDeactivated(TileObject? o)
    {
        CEDGame.MapManager.ShowVirtualLayer = false;
        CEDGame.MapManager.UseVirtualLayer = false;
    }

    private sbyte CalculateNewZ(TileObject o)
    {
        var height = o is StaticObject ? TileDataLoader.Instance.StaticData[o.Tile.Id].Height : 0;
        return (sbyte)(o.Tile.Z + (_drawMode == (int)DrawMode.ON_TOP ? height : 0));
    }
    
    protected override void GhostApply(TileObject? o)
    {
        if (o == null || Random.Next(100) > _drawChance) return;
        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.StaticMode)
        {
            if (o is StaticObject && (DrawMode)_drawMode == DrawMode.REPLACE)
            {
                o.Alpha = 0.3f;
            }

            var newTile = new StaticTile
            (
                tilesWindow.ActiveId,
                o.Tile.X,
                o.Tile.Y,
                CalculateNewZ(o),
                (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.ActiveId : 0)
            );
            CEDGame.MapManager.GhostStaticTiles.Add(o, new StaticObject(newTile));
        }
        else if(o is LandObject lo)
        {
            o.Visible = false;
            var newTile = new LandTile(tilesWindow.ActiveId, o.Tile.X, o.Tile.Y, o.Tile.Z);
            CEDGame.MapManager.GhostLandTiles.Add(lo, new LandObject(newTile));
        }
    }
    
    protected override void GhostClear(TileObject? o)
    {
        if (o != null)
        {
            o.Alpha = 1f;
            o.Visible = true;
            CEDGame.MapManager.GhostStaticTiles.Remove(o);
            if (o is LandObject lo)
            {
                CEDGame.MapManager.GhostLandTiles.Remove(lo);
            }
        }
    }
    
    protected override void Apply(TileObject? o)
    {
        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.StaticMode && o != null)
        {
            var newTile = CEDGame.MapManager.GhostStaticTiles[o];
            if ((DrawMode)_drawMode == DrawMode.REPLACE && o is StaticObject so)
            {
                CEDClient.Remove(so.StaticTile);
            }
            CEDClient.Add(newTile.StaticTile);
        }
        else if(o is LandObject lo)
        {
            var ghostTile = CEDGame.MapManager.GhostLandTiles[lo];
            o.Tile.Id = ghostTile.Tile.Id;
        }
    }
}