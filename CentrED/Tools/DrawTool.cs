using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class DrawTool : BaseTool
{
    private readonly TilesWindow _tilesWindow;
    private readonly BlueprintsWindow _blueprintsWindow;
    private HueTool _hueTool;
    public DrawTool()
    {
        _tilesWindow = UIManager.GetWindow<TilesWindow>();
        _blueprintsWindow = UIManager.GetWindow<BlueprintsWindow>();
    }

    public override void PostConstruct(MapManager mapManager)
    {
        _hueTool = mapManager.Tools.OfType<HueTool>().First();
    }

    public override string Name => LangManager.Get(DRAW_TOOL);
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

    private ushort[] _tileSetValues => _tilesWindow.ActiveTileSetValues;

    internal override void Draw()
    {
        ImGui.Text(LangManager.Get(SOURCE));
        ImGui.RadioButton(LangManager.Get(TILES), ref _drawSource, (int)DrawSource.TILE);
        ImGui.RadioButton(LangManager.Get(TILE_SET), ref _drawSource, (int)DrawSource.TILE_SET);
        if (_tileSetValues.Length <= 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(LangManager.Get(EMPTY));
        }
        ImGui.RadioButton(LangManager.Get(BLUEPRINTS), ref _drawSource, (int)DrawSource.BLUEPRINT);

        if (_drawSource == (int)DrawSource.TILE_SET)
        {
            ImGui.Separator();
            ImGui.Text(LangManager.Get(SOURCE_PARAMETERS));
            ImGuiEx.TwoWaySwitch(LangManager.Get(RANDOM), LangManager.Get(SEQUENTIAL), ref _tileSetSequential);
        }

        ImGui.Separator();
        ImGui.Text(LangManager.Get(MODE));
        var modeChanged = ImGui.RadioButton(LangManager.Get(ON_TOP), ref _drawMode, (int)DrawMode.ON_TOP);
        ImGui.SetItemTooltip(LangManager.Get(ON_TOP_TOOLTIP));
        modeChanged |= ImGui.RadioButton(LangManager.Get(REPLACE), ref _drawMode, (int)DrawMode.REPLACE);
        ImGui.SetItemTooltip(LangManager.Get(REPLACE_TOOLTIP));
        modeChanged |= ImGui.RadioButton(LangManager.Get(COPY_Z), ref _drawMode, (int)DrawMode.COPY_Z);
        ImGui.SetItemTooltip(LangManager.Get(COPY_Z_TOOLTIP));
        modeChanged |= ImGui.RadioButton(LangManager.Get(FIXED_Z), ref _drawMode, (int)DrawMode.FIXED_Z);
        ImGui.SetItemTooltip(LangManager.Get(FIXED_Z_TOOLTIP));

        if (modeChanged)
        {
            MapManager.UseVirtualLayer = _drawMode == (int)DrawMode.FIXED_Z;
            MapManager.ShowVirtualLayer = MapManager.UseVirtualLayer && _showVirtualLayer;
        }

        if (_drawMode == (int)DrawMode.FIXED_Z)
        {
            ImGui.Separator();
            ImGui.Text(LangManager.Get(MODE_PARAMETERS));
            ImGuiEx.DragInt(LangManager.Get(FIXED_Z), ref MapManager.VirtualLayerZ, 1, -128, 127);
            if (ImGui.Checkbox(LangManager.Get(SHOW_VIRTUAL_LAYER), ref _showVirtualLayer))
            {
                MapManager.ShowVirtualLayer = _showVirtualLayer;
            }
        }

        ImGui.Separator();
        ImGui.Text(LangManager.Get(COMMON_PARAMETERS));
        ImGuiEx.DragInt(LangManager.Get(CHANCE), ref _chance, 1, 0, 100);
        ImGui.Checkbox(LangManager.Get(WITH_HUE), ref _withHue);
        ImGui.SetItemTooltip(LangManager.Get(WITH_HUE_TOOLTIP));

        ImGuiEx.DragInt(LangManager.Get(ADD_RANDOM_Z), ref _randomZ, 1, 0, 127);
        ImGui.SetItemTooltip(LangManager.Get(ADD_RANDOM_Z_TOOLTIP));

        ImGui.Checkbox(LangManager.Get(EMPTY_TILE_ONLY), ref _emptyTileOnly);
        ImGui.SetItemTooltip(LangManager.Get(EMPTY_TILE_ONLY_TOOLTIP));
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
        o = TransformTarget(o);
        if (o == null)
            return;

        if (!CanDrawOn(o))
            return;

        if (_drawSource == (int)DrawSource.TILE_SET && _tilesWindow.ActiveTileSetValues.Length == 0)
        {
            return;
        }
        
        ushort ghostId = (DrawSource)_drawSource switch
        {
            DrawSource.TILE => _tilesWindow.SelectedId,
            DrawSource.TILE_SET when _tileSetSequential => GetSequentialTileId(o.Tile.X, o.Tile.Y),
            DrawSource.TILE_SET => _tileSetValues[Random.Shared.Next(_tileSetValues.Length)],
            DrawSource.BLUEPRINT => 0,
            _ => throw new ArgumentException($"Invalid draw source {_drawSource}")
        };

        if (_drawMode == (int)DrawMode.REPLACE && o is StaticObject so)
        {
            so.Highlighted = true;
        }
        
        if (_drawSource == (int)DrawSource.BLUEPRINT)
        {
            var tiles = _blueprintsWindow.Active;
            if (tiles.Count <= 0)
                return;
            
            var ghosts = tiles.Select
            (t => new StaticTile
                 (t.Id, (ushort)(o.Tile.X + t.X), (ushort)(o.Tile.Y + t.Y), (sbyte)(CalculateNewZ(o) + t.Z), _withHue ? _hueTool.ActiveHue : t.Hue)
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
                _withHue ? _hueTool.ActiveHue : (ushort)0
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
        o = TransformTarget(o);
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
        o = TransformTarget(o);
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

    private TileObject? TransformTarget(TileObject? o)
    {
        if (o == null)
            return o;
        
        if (Application.CEDGame.MapManager.UseVirtualLayer && _tilesWindow.LandMode && o is VirtualLayerTile)
        {
            return Application.CEDGame.MapManager.LandTiles[o.Tile.X, o.Tile.Y];
        }
        if (AreaMode && _tilesWindow.LandMode)
        {
            return Application.CEDGame.MapManager.LandTiles[o.Tile.X, o.Tile.Y];
        }
        return o;
    }

    private sbyte CalculateNewZ(TileObject o)
    {
        int height = o.Tile.Z;
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
            height += Random.Shared.Next(0, _randomZ);
        }
        return (sbyte)Math.Clamp(height, sbyte.MinValue, sbyte.MaxValue);
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
        if (AreaMode)
        {
            var width = Math.Abs(Area.X2 - Area.X1);

            var deltaX = Math.Abs(x - Area.X1);
            var deltaY = Math.Abs(y - Area.Y1);

            var sequenceIndex = deltaY * width + deltaX;

            sequenceIndex %= _tileSetValues.Length;

            return _tileSetValues[sequenceIndex];
        }
        var tileId = _tileSetValues[_sequenceIndex];

        if (Pressed)
        {
            _sequenceIndex++;
            if (_sequenceIndex >= _tileSetValues.Length)
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