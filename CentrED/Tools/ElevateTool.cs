using CentrED.Map;
using CentrED.UI;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;
using static CentrED.CentrED;

namespace CentrED.Tools;

public class ElevateTool : BaseTool
{
    
    public override string Name => App.Lang.Get(ELEVATE_TOOL);
    public override Keys Shortcut => Keys.F4;
    
    enum ZMode
    {
        ADD = 0,
        FIXED = 1,
        SNAP_TO_TERRAIN = 2,
    }

    private int _mode;
    private int _value;
    private int _randomPlus;
    private int _randomMinus;
    private bool _lockPlusMinus;

    internal override void Draw()
    {
        ImGui.Text(App.Lang.Get(MODE));
        ImGui.RadioButton(App.Lang.Get(ADD_Z), ref _mode, (int)ZMode.ADD);
        ImGui.RadioButton(App.Lang.Get(FIXED_Z), ref _mode, (int)ZMode.FIXED);
        ImGui.RadioButton(App.Lang.Get(SNAP_TO_TERRAIN), ref _mode, (int)ZMode.SNAP_TO_TERRAIN);
        ImGui.Separator();
        
        ImGui.BeginDisabled(_mode == (int)ZMode.SNAP_TO_TERRAIN);
        ImGuiEx.DragInt("Z", ref _value, 1, ZRange.Min, ZRange.Max);
        ImGui.SameLine();
        if (ImGui.Button(App.Lang.Get(INVERSE)))
        {
            _value = -_value;
        }
        ImGui.EndDisabled();
        ImGui.Separator();
        
        ImGui.BeginGroup();
        if (ImGuiEx.DragInt(App.Lang.Get(PLUS_RANDOM_Z), ref _randomPlus, 1, 0, 127) && _lockPlusMinus)
        {
            _randomMinus = _randomPlus;
        }
        if (ImGuiEx.DragInt(App.Lang.Get(MINUS_RANDOM_Z), ref _randomMinus, 1, 0, 128) && _lockPlusMinus)
        {
            _randomPlus = _randomMinus;       
        }
        ImGui.EndGroup();
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (ImGui.Checkbox($"{App.Lang.Get(LOCK)}##Plus", ref _lockPlusMinus))
        {
            if (_lockPlusMinus)
                _randomMinus = _randomPlus;
        }
        if (ImGui.Checkbox($"{App.Lang.Get(LOCK)}##Minus", ref _lockPlusMinus))
        {
            if (_lockPlusMinus)
                _randomPlus = _randomMinus;
        }
        ImGui.EndGroup();
        ImGui.Separator();
        DrawChance();
    }

    private short NewZ(BaseTile tile)
    {
        var newZ = (ZMode)_mode switch
        {
            ZMode.ADD => tile.Z + _value,
            ZMode.FIXED => _value,
            ZMode.SNAP_TO_TERRAIN => MapManager.GetLandTile(tile.X, tile.Y)?.Z ?? tile.Z, 
            _ => throw new ArgumentOutOfRangeException("[ElevateTool] Invalid Z mode:")
        };
        newZ += Random.Shared.Next(-_randomMinus, _randomPlus + 1);

        return ZRange.Clamp(newZ);
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            var tile = so.StaticTile;
            so.Highlighted = true;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            MapManager.StaticsManager.AddGhost(so, new StaticObject(newTile));
        }
        else if (o is LandObject lo)
        {
            if (_mode == (int)ZMode.SNAP_TO_TERRAIN)
                return;
            
            var tile = lo.LandTile;
            lo.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, NewZ(tile));
            MapManager.GhostLandTiles[lo] = new LandObject(newTile);
            MapManager.OnLandTileElevated(newTile, newTile.Z);
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        o?.Reset();
        if (o is StaticObject)
        {
            MapManager.StaticsManager.ClearGhost(o);
        }
        else if (o is LandObject lo)
        {
            MapManager.GhostLandTiles.Remove(lo);
            MapManager.OnLandTileElevated(lo.LandTile, lo.LandTile.Z);
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o is StaticObject)
        {
            if (MapManager.StaticsManager.TryGetGhost(o, out var ghostTile))
            {
                o.Tile.Z = ghostTile.Tile.Z;
            }
        }
        else if (o is LandObject lo)
        {
            if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                o.Tile.Z = ghostTile.Tile.Z;
            }
        }
    }

    public override void GrabZ(short z)
    {
        _mode = (int)ZMode.FIXED;
        _value = z;
    }
}