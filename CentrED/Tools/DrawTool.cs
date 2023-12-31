using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class DrawTool : Tool
{
    private static readonly Random Random = new();
    public override string Name => "Draw";
    public override Keys Shortcut => Keys.F2;

    private bool _pressed;
    private bool _rectangular;
    private TileObject? _startTile;

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
    
    public override void OnKeyPressed(Keys key)
    {
        if (key == Keys.LeftControl && !_pressed)
        {
            _rectangular = true;
        }
    }

    public override void OnKeyReleased(Keys key)
    {
        if (key == Keys.LeftControl && !_pressed)
        {
            _rectangular = false;
        }
    }
    
    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
        if (_rectangular && _startTile == null && o != null)
        {
            _startTile = o;
        }
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if(_pressed)
        {
            Apply(o);
        }
        _pressed = false;
        _startTile = null;
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (o == null)
            return;

        if (_rectangular && _pressed)
        {
            foreach (var to in  CEDGame.MapManager.GetTopTiles(_startTile, o, CEDGame.UIManager.TilesWindow.LandMode))
            {
                AddGhostTile(to);   
            }
        }
        else
        {
            AddGhostTile(o);
        }
    }
    
    public override void OnMouseLeave(TileObject? o)
    {
        if (_pressed && !_rectangular)
        {
            Apply(o);
        }
        CEDGame.MapManager.GhostStaticTiles.Clear();
        var ghostLandTiles = CEDGame.MapManager.GhostLandTiles;
        foreach (var ghostLandTile in ghostLandTiles)
        {
            var landTile = CEDGame.MapManager.LandTiles[ghostLandTile.Tile.X, ghostLandTile.Tile.Y];
            if (landTile != null)
            {
                landTile.Visible = true;
            }
        }
        CEDGame.MapManager.GhostLandTiles.Clear();
    }
    
    private void Apply(TileObject? o)
    {
        var mapManager = CEDGame.MapManager;
        foreach (var ghostLandTile in mapManager.GhostLandTiles)
        {
            var landTile = mapManager.LandTiles[ghostLandTile.Tile.X, ghostLandTile.Tile.Y];
            if(landTile == null)
                continue;
            landTile.Tile.Id = ghostLandTile.Tile.Id;
        }
        foreach (var ghostStaticTile in mapManager.GhostStaticTiles)
        {
            var staticTiles = mapManager.StaticTiles[ghostStaticTile.Tile.X, ghostStaticTile.Tile.Y];
            if ((DrawMode)_drawMode == DrawMode.REPLACE || staticTiles?.Count == 0)
            {
                var topTile = staticTiles[^1];
                topTile.Tile.Id = ghostStaticTile.Tile.Id;
            }
            else
            {
                CEDClient.Add(ghostStaticTile.StaticTile);
            }
        }
    }
    
    private void AddGhostTile(TileObject? o)
    {
        if (o == null || Random.Next(100) > _drawChance) return;
        var tilesWindow = CEDGame.UIManager.TilesWindow;
        if (tilesWindow.StaticMode)
        {
            var height = o is StaticObject ? TileDataLoader.Instance.StaticData[o.Tile.Id].Height : 0;
            var newZ = o.Tile.Z + (_drawMode == (int)DrawMode.ON_TOP ? height : 0);

            if (o is StaticObject && (DrawMode)_drawMode == DrawMode.REPLACE)
            {
                o.Alpha = 0.3f;
            }

            var newTile = new StaticTile
            (
                tilesWindow.ActiveId,
                o.Tile.X,
                o.Tile.Y,
                (sbyte)newZ,
                (ushort)(_withHue ? CEDGame.UIManager.HuesWindow.ActiveId : 0)
            );
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
        else if(o is LandObject)
        {
            o.Visible = false;
            var newTile = new LandTile(tilesWindow.ActiveId, o.Tile.X, o.Tile.Y, o.Tile.Z);
            CEDGame.MapManager.GhostLandTiles.Add(new LandObject(newTile));
        }
    }
}