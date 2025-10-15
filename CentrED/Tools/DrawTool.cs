﻿using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class DrawTool : BaseTool
{
    private readonly TilesWindow _tilesWindow;
    private readonly HuesWindow _huesWindow;
    // private readonly BlueprintsWindow _blueprintsWindow;
    public DrawTool()
    {
        _tilesWindow = UIManager.GetWindow<TilesWindow>();
        _huesWindow = UIManager.GetWindow<HuesWindow>();
        // _blueprintsWindow = UIManager.GetWindow<BlueprintsWindow>();
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

    internal override void Draw()
    {
        ImGui.Text(LangManager.Get(SOURCE));
        ImGui.RadioButton(LangManager.Get(TILES), ref _drawSource, (int)DrawSource.TILE);
        ImGui.RadioButton(LangManager.Get(TILE_SET), ref _drawSource, (int)DrawSource.TILE_SET);
        // ImGui.RadioButton("Blueprint", ref _drawSource, (int)DrawSource.BLUEPRINT);

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
            // var info = Application.CEDGame.MapManager.BlueprintManager.Get(_blueprintsWindow.SelectedId);
            // if (info.Count <= 0)
            //     return;
            //
            // var ghosts = info.Select
            // (mi => new StaticTile
            //      (mi.ID, (ushort)(o.Tile.X + mi.X), (ushort)(o.Tile.Y + mi.Y), (sbyte)(CalculateNewZ(o) + mi.Z), _withHue ? _huesWindow.ActiveId : (ushort)0)
            // ).Select(st => new StaticObject(st));
            // MapManager.StaticsManager.AddGhosts(o, ghosts);
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