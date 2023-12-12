using CentrED.Map;
using CentrED.UI;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools;

public class ElevateTool : Tool
{
    [Flags]
    enum ZMode
    {
        ADD = 0,
        SET = 2,
    }

    private int zMode;
    private int value;

    private bool _pressed;
    public override string Name => "ElevateTool";

    internal override void Draw()
    {
        ImGui.RadioButton("Add", ref zMode, (int)ZMode.ADD);
        ImGui.RadioButton("Set", ref zMode, (int)ZMode.SET);
        UIManager.DragInt("Z", ref value, 1, -127, 127);
    }

    private sbyte NewZ(BaseTile tile) => (sbyte)((ZMode)zMode switch
    {
        ZMode.ADD => tile.Z + value,
        ZMode.SET => value,
        _ => throw new ArgumentOutOfRangeException()
    });

    public override void OnMouseEnter(TileObject? o)
    {
        if (o is StaticObject so)
        {
            var tile = so.StaticTile;
            so.Alpha = 0.3f;
            var newTile = new StaticTile(tile.Id, tile.X, tile.Y, NewZ(tile), tile.Hue);
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
        else if (o is LandObject lo)
        {
            var tile = lo.LandTile;
            lo.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, NewZ(tile));
            CEDGame.MapManager.GhostLandTiles.Add(new LandObject(newTile));
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 1f;
            CEDGame.MapManager.GhostStaticTiles.Clear();
        }
        else if (o is LandObject lo)
        {
            lo.Visible = true;
            CEDGame.MapManager.GhostLandTiles.Clear();
        }
        if (_pressed)
        {
            Apply(o);
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_pressed && o != null)
        {
            Apply(o);
        }
        _pressed = false;
    }

    private void Apply(TileObject? o)
    {
        if(o != null)
            o.Tile.Z = NewZ(o.Tile);
    }
}