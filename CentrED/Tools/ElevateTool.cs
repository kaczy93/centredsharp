using CentrED.Map;
using CentrED.UI;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class ElevateTool : BaseTool
{
    
    public override string Name => LangManager.Get(ELEVATE_TOOL);
    public override Keys Shortcut => Keys.F4;
    
    enum ZMode
    {
        ADD = 0,
        FIXED = 1,
    }

    private int _mode;
    private int _value;
    private int _randomPlus;
    private int _randomMinus;
    private bool _lockPlusMinus;

    internal override void Draw()
    {
        ImGui.Text(LangManager.Get(MODE));
        ImGui.RadioButton(LangManager.Get(ADD_Z), ref _mode, (int)ZMode.ADD);
        ImGui.RadioButton(LangManager.Get(FIXED_Z), ref _mode, (int)ZMode.FIXED);
        ImGui.Separator();
        
        ImGuiEx.DragInt("Z", ref _value, 1, -128, 127);
        ImGui.SameLine();
        if (ImGui.Button(LangManager.Get(INVERSE)))
        {
            _value = -_value;
        }
        ImGui.Separator();
        
        ImGui.BeginGroup();
        if (ImGuiEx.DragInt(LangManager.Get(PLUS_RANDOM_Z), ref _randomPlus, 1, 0, 127) && _lockPlusMinus)
        {
            _randomMinus = _randomPlus;
        }
        if (ImGuiEx.DragInt(LangManager.Get(MINUS_RANDOM_Z), ref _randomMinus, 1, 0, 128) && _lockPlusMinus)
        {
            _randomPlus = _randomMinus;       
        }
        ImGui.EndGroup();
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (ImGui.Checkbox($"{LangManager.Get(LOCK)}##Plus", ref _lockPlusMinus))
        {
            if (_lockPlusMinus)
                _randomMinus = _randomPlus;
        }
        if (ImGui.Checkbox($"{LangManager.Get(LOCK)}##Minus", ref _lockPlusMinus))
        {
            if (_lockPlusMinus)
                _randomPlus = _randomMinus;
        }
        ImGui.EndGroup();
        ImGui.Separator();
        ImGuiEx.DragInt(LangManager.Get(CHANCE), ref _chance, 1, 0, 100);
        
    }

    private sbyte NewZ(BaseTile tile)
    {
        var newZ = (ZMode)_mode switch
        {
            ZMode.ADD => tile.Z + _value,
            ZMode.FIXED => _value,
            _ => throw new ArgumentOutOfRangeException("[ElevateTool] Invalid Z mode:")
        };
        newZ += Random.Shared.Next(-_randomMinus, _randomPlus + 1);

        return (sbyte)Math.Clamp(newZ, sbyte.MinValue, sbyte.MaxValue);
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            var tile = so.StaticTile;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            MapManager.StaticsManager.AddGhost(so, new StaticObject(newTile));
        }
        else if (o is LandObject lo)
        {
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
}