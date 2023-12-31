using CentrED.Map;
using CentrED.UI;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class ElevateTool : BaseTool
{
    
    public override string Name => "Elevate";
    public override Keys Shortcut => Keys.F4;
    
    private static readonly Random _random = new();
    
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
        ImGui.RadioButton("Add", ref zMode, (int)ZMode.ADD);
        ImGui.RadioButton("Set", ref zMode, (int)ZMode.SET);
        ImGui.RadioButton("Random +/-", ref zMode, (int)ZMode.RANDOM);
        if (ImGui.Button("Inverse"))
        {
            value = -value;
        }
        UIManager.DragInt("Z", ref value, 1, -127, 127);
    }

    private sbyte NewZ(BaseTile tile) => (sbyte)((ZMode)zMode switch
    {
        ZMode.ADD => tile.Z + value,
        ZMode.SET => value,
        ZMode.RANDOM => tile.Z + _random.Next(-value, value + 1),
        _ => throw new ArgumentOutOfRangeException()
    });

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            var tile = so.StaticTile;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            CEDGame.MapManager.GhostStaticTiles.Add(so, new StaticObject(newTile));
        }
        else if (o is LandObject lo)
        {
            var tile = lo.LandTile;
            lo.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, NewZ(tile));
            CEDGame.MapManager.GhostLandTiles.Add(lo, new LandObject(newTile));
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject)
        {
            o.Alpha = 1f;
            CEDGame.MapManager.GhostStaticTiles.Remove(o);
        }
        else if (o is LandObject lo)
        {
            o.Visible = true;
            CEDGame.MapManager.GhostLandTiles.Remove(lo);
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is StaticObject)
        {
            o.Tile.Z = CEDGame.MapManager.GhostStaticTiles[o].Tile.Z;
            
        }
        else if (o is LandObject lo)
        {
            o.Tile.Z = CEDGame.MapManager.GhostLandTiles[lo].Tile.Z;
        }
    }
}