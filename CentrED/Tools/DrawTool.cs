using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;

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
    private int  _randomZ = 0;
    private bool _emptyTileOnly;

    internal override void Draw()
    {
        base.Draw();

        // Random Tile Set checkbox
        bool randomWasChecked = MapManager.UseRandomTileSet;
        if (ImGui.Checkbox("Random Tile Set", ref MapManager.UseRandomTileSet))
        {
            if (!randomWasChecked && MapManager.UseRandomTileSet)
            {
                MapManager.UseSequentialTileSet = false;
            }
        }

        // Sequential Tile Set checkbox
        bool sequentialWasChecked = MapManager.UseSequentialTileSet;
        if (ImGui.Checkbox("Sequential Tile Set", ref MapManager.UseSequentialTileSet))
        {
            if (!sequentialWasChecked && MapManager.UseSequentialTileSet)
            {
                MapManager.UseRandomTileSet = false;
            }
        }

        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.PushItemWidth(50);
        ImGui.PopItemWidth();
        UIManager.Tooltip("Double click to set specific value");
        if (ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP) || 
            ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE) ||
            ImGui.RadioButton("Copy Z", ref _drawMode, (int)DrawMode.COPY_Z))
        {
            MapManager.UseVirtualLayer = false;
        }
        if (ImGui.RadioButton("Virtual Layer", ref _drawMode, (int)DrawMode.VIRTUAL_LAYER))
        {
            MapManager.UseVirtualLayer = true;           
        }
        if (_drawMode == (int)DrawMode.VIRTUAL_LAYER)
        {
            ImGui.SameLine();
            UIManager.DragInt("##VirtualLayerZ", ref MapManager.VirtualLayerZ, 1, -128, 127);
        }
        if (ImGui.Checkbox("Show VL", ref _showVirtualLayer))
        {
            MapManager.ShowVirtualLayer = _showVirtualLayer;
        }             
        UIManager.DragInt("Add Random Z", ref _randomZ, 1, 0, 127);
        ImGui.Checkbox("Empty tile only", ref _emptyTileOnly);
    }

    public override void OnActivated(TileObject? o)
    {
        if (_drawMode == (int)DrawMode.VIRTUAL_LAYER)
        {
            MapManager.ShowVirtualLayer = _showVirtualLayer;
            MapManager.UseVirtualLayer = _drawMode == (int)DrawMode.VIRTUAL_LAYER;
        }
    }

    public override void OnDeactivated(TileObject? o)
    {
        MapManager.ShowVirtualLayer = false;
        MapManager.UseVirtualLayer = false;
    }

    private sbyte CalculateNewZ(TileObject o)
    {
        var height = o.Tile.Z;
        if (_drawMode == (int)DrawMode.VIRTUAL_LAYER)
        {
            height = (sbyte)MapManager.VirtualLayerZ;
        }
        else if (o is StaticObject && _drawMode == (int)DrawMode.ON_TOP)
        {
            height += (sbyte)TileDataLoader.Instance.StaticData[o.Tile.Id].Height;
        }

        if (_randomZ > 0)
        {
            Random _random = new();
            height += (sbyte)_random.Next(0, _randomZ);
        }

        return height;
    }
    
    protected override void GhostApply(TileObject? o)
    {
        if (o == null) return;
        var tilesWindow = UIManager.GetWindow<TilesWindow>();
        if (tilesWindow.StaticMode)
        {
            // Get the right tile ID based on sequence and position
            ushort tileId;
            if (MapManager.UseSequentialTileSet)
            {
                // Use position-aware sequential ID calculation
                tileId = GetSequentialTileId(o.Tile.X, o.Tile.Y);
            }
            else
            {
                tileId = tilesWindow.ActiveId;
            }

            if (_emptyTileOnly)
            {
                if (o is StaticObject)
                {
                    if (MapManager.CanDrawStatic((StaticObject)o))
                    {
                        return;
                    }
                }
                else if(o is VirtualLayerTile)
                {
                    var staticObjects = MapManager.StaticTiles[o.Tile.X, o.Tile.Y];
                    if (staticObjects != null)
                    {
                        foreach (var so2 in staticObjects)
                        {
                            if (so2.StaticTile.Z == o.Tile.Z)
                            {
                                if (MapManager.CanDrawStatic((StaticObject)so2))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (o is StaticObject so && (DrawMode)_drawMode == DrawMode.REPLACE)
            {
                so.Alpha = 0.3f;
            }

            var newTile = new StaticTile
            (
                tileId,
                o.Tile.X,
                o.Tile.Y,
                CalculateNewZ(o),
                (ushort)(_withHue ? UIManager.GetWindow<HuesWindow>().ActiveId : 0)
            );
            MapManager.GhostStaticTiles[o] = new StaticObject(newTile);
        }
        else if(o is LandObject lo)
        {
            o.Visible = false;
            
            // Get the right tile ID based on sequence and position
            ushort tileId;
            if (MapManager.UseSequentialTileSet)
            {
                // Use position-aware sequential ID calculation
                tileId = GetSequentialTileId(o.Tile.X, o.Tile.Y);
            }
            else
            {
                tileId = tilesWindow.ActiveId;
            }
            
            var newTile = new LandTile(tileId, o.Tile.X, o.Tile.Y, CalculateNewZ(o));
            MapManager.GhostLandTiles[lo] = new LandObject(newTile);
        }
    }
    
    protected override void GhostClear(TileObject? o)
    {
        if (o != null)
        {
            o.Reset();
            MapManager.GhostStaticTiles.Remove(o);
            if (o is LandObject lo)
            {
                MapManager.GhostLandTiles.Remove(lo);
            }
        }
    }
    
    protected override void InternalApply(TileObject? o)
    {
        var tilesWindow = UIManager.GetWindow<TilesWindow>();
        if (tilesWindow.StaticMode && o != null)
        {
            if(MapManager.GhostStaticTiles.TryGetValue(o, out var ghostTile))
            {
                if ((DrawMode)_drawMode == DrawMode.REPLACE && o is StaticObject so)
                {
                    Client.Remove(so.StaticTile);
                }
                Client.Add(ghostTile.StaticTile);
            }
        }
        else if(o is LandObject lo)
        {
            if(MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                //o.Tile.Id = ghostTile.Tile.Id;
                lo.LandTile.ReplaceLand(ghostTile.Tile.Id, ghostTile.Tile.Z);
            }     
            
        }
    }
}