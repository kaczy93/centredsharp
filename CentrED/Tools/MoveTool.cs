using System.Numerics;
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
        var buttonSize = new Vector2(19, 19);
        var i = 0;
        ImGui.PushButtonRepeat(true);
        var startPos = ImGui.GetCursorPos();
        if (ImGui.Button("##x1", buttonSize))
        {
            _xDelta--;
        }
        ImGui.SameLine(0,4);
        if (ImGui.ArrowButton("up", ImGuiDir.Up))
        {
            _xDelta--;
            _yDelta--;
        }
        ImGui.SameLine(0,4);
        if (ImGui.Button("##y1", buttonSize))
        {
            _yDelta--;
        }
        if (ImGui.ArrowButton("left", ImGuiDir.Left))
        {
            _xDelta--;
            _yDelta++;
        }
        ImGui.SameLine(0,4);
        if (ImGui.ArrowButton("none", ImGuiDir.None))
        {
            
        }
        ImGui.SameLine(0,4);
        if (ImGui.ArrowButton("right", ImGuiDir.Right))
        {
            _xDelta++;
            _yDelta--;
        }
        if (ImGui.Button("##y2", buttonSize))
        {
            _yDelta++;
        }
        ImGui.SameLine(0,4);
        if (ImGui.ArrowButton("down", ImGuiDir.Down))
        {
            _xDelta++;
            _yDelta++;
        }
        ImGui.SameLine(0,4);
        if (ImGui.Button("##x2", buttonSize))
        {
            _xDelta++;
        }
        ImGui.PopButtonRepeat();
        var endPos = ImGui.GetCursorPos();
        var style = ImGui.GetStyle();
        var framePadding = style.FramePadding;
        var cellPadding = style.CellPadding;
        if(_xDelta < 0 )
        {
            ImGui.SetCursorPos(startPos + framePadding);
            ImGui.Text($"{-_xDelta}");
        }
        if(_yDelta < 0 )
        {
            ImGui.SetCursorPos(startPos + new Vector2((buttonSize.X + framePadding.X) * 2 , 0) + framePadding);
            ImGui.Text($"{-_yDelta}");
        }
        if(_yDelta > 0 )
        {
            ImGui.SetCursorPos(startPos + new Vector2(0, (buttonSize.Y + framePadding.Y) * 2) + framePadding);
            ImGui.Text($"{_yDelta}");
        }
        if(_xDelta > 0 )
        {
            ImGui.SetCursorPos(startPos + (buttonSize + framePadding) * 2 + framePadding);
            ImGui.Text($"{_xDelta}");
        }
        
        
        ImGui.SetCursorPos(endPos);
        ImGui.Text("Delta X: " + _xDelta );
        ImGui.Text("Delta Y: " + _yDelta );
        
        
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