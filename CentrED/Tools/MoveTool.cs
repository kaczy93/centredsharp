using CentrED.Map;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools;

public class MoveTool : Tool
{
    public override string Name => "MoveTool";

    private int _xDelta;
    private int _yDelta;

    private bool _pressed;

    internal override void Draw()
    {
        ImGui.InputInt("X", ref _xDelta);
        ImGui.InputInt("Y", ref _yDelta);
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 0.3f;
            var newTile = new StaticTile
            (
                so.StaticTile.Id,
                (ushort)(so.StaticTile.X + _xDelta),
                (ushort)(so.StaticTile.Y + _yDelta),
                so.StaticTile.Z,
                so.StaticTile.Hue
            );
            CEDGame.MapManager.GhostStaticTiles.Add(new StaticObject(newTile));
        }
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if(_pressed)
            Apply(o);
        if (o is StaticObject so)
        {
            so.Alpha = 1f;
            CEDGame.MapManager.GhostStaticTiles.Clear();
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_pressed)
        {
            Apply(o);
        }
        _pressed = false;
    }

    private void Apply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.StaticTile.UpdatePos
                ((ushort)(so.StaticTile.X + _xDelta), (ushort)(so.StaticTile.Y + _yDelta), so.StaticTile.Z);
        }
    }
}