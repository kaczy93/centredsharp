using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : BaseTool
{
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
    private bool _showVirtualLayer;

    internal override void Draw()
    {
        base.Draw();
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.PushItemWidth(50);
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
        UIManager.DragInt("Z", ref CEDGame.MapManager.VirtualLayerZ, 1, -128, 127);
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
        if (o == null) return;
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
            CEDGame.MapManager.GhostStaticTiles[o] = new StaticObject(newTile);
        }
        else if(o is LandObject lo)
        {
            o.Visible = false;
            var newTile = new LandTile(tilesWindow.ActiveId, o.Tile.X, o.Tile.Y, o.Tile.Z);
            CEDGame.MapManager.GhostLandTiles[lo] = new LandObject(newTile);
        }
    }
    
    protected override void GhostClear(TileObject? o)
    {
        if (o != null)
        {
            o.Reset();
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
            if(CEDGame.MapManager.GhostStaticTiles.TryGetValue(o, out var ghostTile))
            {
                if ((DrawMode)_drawMode == DrawMode.REPLACE && o is StaticObject so)
                {
                    CEDClient.Remove(so.StaticTile);
                }
                CEDClient.Add(ghostTile.StaticTile);
            }
        }
        else if(o is LandObject lo)
        {
            if(CEDGame.MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                o.Tile.Id = ghostTile.Tile.Id;
            }
        }
    }
}