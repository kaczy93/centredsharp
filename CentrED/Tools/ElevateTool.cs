using CentrED.Map;
using CentrED.UI;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class ElevateTool : BaseTool
{
    
    public override string Name => "Elevate";
    public override Keys Shortcut => Keys.F4;
    
    enum ZMode
    {
        ADD = 0,
        SET = 1,
        RANDOM = 2
    }

    private int zMode;
    private int value;

    internal override void Draw()
    {
        base.Draw();
        ImGui.RadioButton("Add", ref zMode, (int)ZMode.ADD);
        ImGui.RadioButton("Set", ref zMode, (int)ZMode.SET);
        ImGui.RadioButton("Random +/-", ref zMode, (int)ZMode.RANDOM);
        if (ImGui.Button("Inverse"))
        {
            value = -value;
        }
        UIManager.DragInt("Z", ref value, 1, -128, 127);
    }

    private sbyte NewZ(BaseTile tile) => (sbyte)((ZMode)zMode switch
    {
        ZMode.ADD => tile.Z + value,
        ZMode.SET => value,
        ZMode.RANDOM => tile.Z + Random.Next(-Math.Abs(value), Math.Abs(value) + 1),
        _ => throw new ArgumentOutOfRangeException()
    });

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            var tile = so.StaticTile;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            MapManager.GhostStaticTiles[so] = new StaticObject(newTile);
        }
        else if (o is LandObject lo)
        {
            var tile = lo.LandTile;
            lo.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, NewZ(tile));
            MapManager.GhostLandTiles[lo] = new LandObject(newTile);
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        o?.Reset();
        if (o is StaticObject)
        {
            MapManager.GhostStaticTiles.Remove(o);
        }
        else if (o is LandObject lo)
        {
            MapManager.GhostLandTiles.Remove(lo);
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is StaticObject)
        {
            if (MapManager.GhostStaticTiles.TryGetValue(o, out var ghostTile))
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