using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using ClassicUO.Assets;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class DrawTool : BaseTool
{
    private readonly TilesWindow _tilesWindow;
    private readonly HuesWindow _huesWindow;
    public DrawTool()
    {
        _tilesWindow = UIManager.GetWindow<TilesWindow>();
        _huesWindow = UIManager.GetWindow<HuesWindow>();
    }
    
    public override string Name => "Draw";
    public override Keys Shortcut => Keys.F2;

    enum DrawSource
    {
        TILE,
        TILE_SET,
        BLUEPRINT
    }

    enum DrawMode
    {
        ON_TOP,
        REPLACE,
        COPY_Z,
        FIXED_Z
    }

    private int _drawSource;
    private int _drawMode;
    private int _randomZ;
    private bool _withHue;
    private bool _emptyTileOnly;
    private bool _showVirtualLayer;
    private bool _tileSetSequential;

    //TODO: Remove me!
    private ushort _multiId; 
    private List<MultiInfo>? _Info = [];

    internal override void Draw()
    {
        ImGui.Text("Source");
        ImGui.RadioButton("Tile", ref _drawSource, (int)DrawSource.TILE);
        ImGui.RadioButton("Tile Set", ref _drawSource, (int)DrawSource.TILE_SET);
        ImGui.RadioButton("Blueprint", ref _drawSource, (int)DrawSource.BLUEPRINT);

        if (_drawSource == (int)DrawSource.TILE_SET)
        {
            ImGui.Separator();
            ImGui.Text("Source options");
            ImGuiEx.TwoWaySwitch("Random", "Sequential", ref _tileSetSequential);
        }
        if (_drawSource == (int)DrawSource.BLUEPRINT)
        {
            ImGui.Separator();
            ImGui.Text("Source options");
            if (ImGuiEx.InputUInt16("MultiId", ref _multiId))
            {
                _Info = MapManager.UoFileManager.Multis.GetMultis(_multiId);
            }
        }

        ImGui.Separator();
        ImGui.Text("Mode");
        var modeChanged = ImGui.RadioButton("On Top", ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.SetItemTooltip
            ("Static will be placed on top of the selected tile\n" + "This means Z + item height defined in tiledata");
        modeChanged |= ImGui.RadioButton("Replace", ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.SetItemTooltip("Static will replace selected tile");
        modeChanged |= ImGui.RadioButton("Copy Z", ref _drawMode, (int)DrawMode.COPY_Z);
        ImGui.SetItemTooltip("Static will have the same Z as selected tile");
        modeChanged |= ImGui.RadioButton("Fixed Z", ref _drawMode, (int)DrawMode.FIXED_Z);
        ImGui.SetItemTooltip("Static Z will be set to a selected value");

        if (modeChanged)
        {
            MapManager.UseVirtualLayer = _drawMode == (int)DrawMode.FIXED_Z;
            MapManager.ShowVirtualLayer = MapManager.UseVirtualLayer && _showVirtualLayer;
        }

        if (_drawMode == (int)DrawMode.FIXED_Z)
        {
            ImGui.Separator();
            ImGui.Text("Mode options");
            ImGuiEx.DragInt("Fixed Z", ref MapManager.VirtualLayerZ, 1, -128, 127);
            if (ImGui.Checkbox("Show Virtual Layer", ref _showVirtualLayer))
            {
                MapManager.ShowVirtualLayer = _showVirtualLayer;
            }
        }

        ImGui.Separator();
        ImGui.Text("Common Options");
        ImGuiEx.DragInt("Chance", ref _chance, 1, 0, 100);
        ImGui.Checkbox("With Hue", ref _withHue);
        ImGui.SetItemTooltip("Selected hue will be applied to drawn statics");

        ImGuiEx.DragInt("Add Random Z", ref _randomZ, 1, 0, 127);
        ImGui.SetItemTooltip("Random Z w will be added to static Z");

        ImGui.Checkbox("Empty tile only", ref _emptyTileOnly);
        ImGui.SetItemTooltip("Draw statics only if there are no statics on the tile");
    }

    public override void OnActivated(TileObject? o)
    {
        if (_drawMode == (int)DrawMode.FIXED_Z)
        {
            MapManager.UseVirtualLayer = true;
            MapManager.ShowVirtualLayer = _showVirtualLayer;
        }
    }

    public override void OnDeactivated(TileObject? o)
    {
        MapManager.UseVirtualLayer = false;
        MapManager.ShowVirtualLayer = false;
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o == null)
            return;

        if (!CanDrawOn(o))
            return;
        
        ushort ghostId = (DrawSource)_drawSource switch
        {
            DrawSource.TILE => _tilesWindow.SelectedId,
            DrawSource.TILE_SET when _tileSetSequential => GetSequentialTileId(o.Tile.X, o.Tile.Y),
            DrawSource.TILE_SET => _tilesWindow.ActiveTileSetValues[Random.Shared.Next(_tilesWindow.ActiveTileSetValues.Length)],
            DrawSource.BLUEPRINT => 0,
            _ => throw new ArgumentException($"Invalid draw source {_drawSource}")
        };

        if (_drawMode == (int)DrawMode.REPLACE && o is StaticObject so)
        {
            so.Alpha = 0.3f;
        }
        
        if (_drawSource == (int)DrawSource.BLUEPRINT)
        {
            if (_Info == null)
                return;
            
            var ghosts = _Info.Select
            (mi => new StaticTile
                 (mi.ID, (ushort)(o.Tile.X + mi.X), (ushort)(o.Tile.Y + mi.Y), (sbyte)(CalculateNewZ(o) + mi.Z), _withHue ? _huesWindow.ActiveId : (ushort)0)
            ).Select(st => new StaticObject(st));
            MapManager.StaticsManager.AddGhosts(o, ghosts);
        }
        else if (_tilesWindow.StaticMode)
        {
            //TODO: Should we pool ghost tiles to avoid allocation?
            var newTile = new StaticTile
            (
                ghostId,
                o.Tile.X,
                o.Tile.Y,
                CalculateNewZ(o),
                _withHue ? _huesWindow.ActiveId : (ushort)0
            );
            MapManager.StaticsManager.AddGhost(o, new StaticObject(newTile));
        }
        else if (o is LandObject lo)
        {
            o.Visible = false;
            var newTile = new LandTile(ghostId, o.Tile.X, o.Tile.Y, CalculateNewZ(o));
            MapManager.GhostLandTiles[lo] = new LandObject(newTile);
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o == null)
            return;

        o.Reset();
        MapManager.StaticsManager.ClearGhost(o);
        if (o is LandObject lo)
        {
            MapManager.GhostLandTiles.Remove(lo);
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o == null)
            return;

        
        if (_drawMode == (int)DrawMode.REPLACE && o is StaticObject so)
        {
            Client.Remove(so.StaticTile);
        }
        
        if (_drawSource == (int)DrawSource.BLUEPRINT)
        {
            foreach (var ghost in MapManager.StaticsManager.GetGhosts(o))
            {
                Client.Add(ghost.StaticTile);
            }
        }
        else if (_tilesWindow.StaticMode)
        {
            if (MapManager.StaticsManager.TryGetGhost(o, out var ghostTile))
            {
               
                Client.Add(ghostTile.StaticTile);
            }
        }
        else if (o is LandObject lo)
        {
            if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                lo.LandTile.ReplaceLand(ghostTile.Tile.Id, ghostTile.Tile.Z);
            }
        }
    }

    private sbyte CalculateNewZ(TileObject o)
    {
        var height = o.Tile.Z;
        if (_drawMode == (int)DrawMode.FIXED_Z)
        {
            height = (sbyte)MapManager.VirtualLayerZ;
        }
        else if (_drawMode == (int)DrawMode.ON_TOP && o is StaticObject)
        {
            height += (sbyte)MapManager.UoFileManager.TileData.StaticData[o.Tile.Id].Height;
        }

        if (_randomZ > 0)
        {
            //Should it be +/-?
            height += (sbyte)Random.Shared.Next(0, _randomZ);
        }

        return height;
    }
    
    private bool CanDrawOn(TileObject o)
    {
        if (_tilesWindow.StaticMode && _emptyTileOnly)
        {
            if (o is StaticObject so)
            {
                if (MapManager.CanDrawStatic(so))
                {
                    return false;
                }
            }
            else if (o is VirtualLayerTile)
            {
                foreach (var so2 in MapManager.StaticsManager.Get(o.Tile.X, o.Tile.Y))
                {
                    if (so2.StaticTile.Z == o.Tile.Z && MapManager.CanDrawStatic(so2))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    
    private int _sequenceIndex;

    private ushort GetSequentialTileId(ushort x, ushort y)
    {
        if (_tilesWindow.ActiveTileSetValues.Length == 0)
            return _tilesWindow.SelectedId;

        if (IsAreaOperation)
        {
            var width = Math.Abs(AreaEndX - AreaStartX);

            var deltaX = Math.Abs(x - AreaStartX);
            var deltaY = Math.Abs(y - AreaStartY);

            var sequenceIndex = deltaY * width + deltaX;

            sequenceIndex %= _tilesWindow.ActiveTileSetValues.Length;

            return _tilesWindow.ActiveTileSetValues[sequenceIndex];
        }
        var tileId = _tilesWindow.ActiveTileSetValues[_sequenceIndex];

        if (Pressed)
        {
            _sequenceIndex++;
            if (_sequenceIndex >= _tilesWindow.ActiveTileSetValues.Length)
            {
                _sequenceIndex = 0;
            }
        }
        else
        {
            _sequenceIndex = 0;
        }

        return tileId;
    }
}