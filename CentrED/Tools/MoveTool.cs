using System.Numerics;
using CentrED.Map;
using CentrED.UI;
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
        var delta = Vector2.Zero;
        var buttonSize = new Vector2(19, 19);
        var spacing = new Vector2(4, 4);
        var totalWidth = 3 * buttonSize.X + 2 * spacing.X;
        var xOffset = (ImGui.GetContentRegionAvail().X - totalWidth) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xOffset);
        ImGui.BeginGroup();
        ImGui.PushButtonRepeat(true);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing);
        var startPos = ImGui.GetCursorPos();
        if (ImGui.Button("##x1", buttonSize))
        {
            _xDelta--;
        }
        ImGui.SameLine();
        if (ImGui.ArrowButton("up", ImGuiDir.Up))
        {
            _xDelta--;
            _yDelta--;
        }
        ImGui.SameLine();
        if (ImGui.Button("##y1", buttonSize))
        {
            _yDelta--;
        }
        if (ImGui.ArrowButton("left", ImGuiDir.Left))
        {
            _xDelta--;
            _yDelta++;
        }
        ImGui.SameLine();
        ImGui.PopButtonRepeat();
        if (ImGui.Button("?", buttonSize))
        {
            if (delta == Vector2.Zero)
            {
                _xDelta = 0;
                _yDelta = 0;
            }
        }
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            //TODO: Do magic with delta
            delta = ImGui.GetMouseDragDelta();
        }
        UIManager.Tooltip(/*"Drag Me\n" + */"Click to reset");
        ImGui.SameLine();
        ImGui.PushButtonRepeat(true);
        if (ImGui.ArrowButton("right", ImGuiDir.Right))
        {
            _xDelta++;
            _yDelta--;
        }
        if (ImGui.Button("##y2", buttonSize))
        {
            _yDelta++;
        }
        ImGui.SameLine();
        if (ImGui.ArrowButton("down", ImGuiDir.Down))
        {
            _xDelta++;
            _yDelta++;
        }
        ImGui.SameLine();
        if (ImGui.Button("##x2", buttonSize))
        {
            _xDelta++;
        }
        ImGui.PopButtonRepeat();
        ImGui.PopStyleVar();
        var endPos = ImGui.GetCursorPos();
        var style = ImGui.GetStyle();
        var framePadding = style.FramePadding;
        if(_xDelta < 0 )
        {
            ImGui.SetCursorPos(startPos + framePadding);
            ImGui.Text($"{-_xDelta}");
        }
        if(_yDelta < 0 )
        {
            ImGui.SetCursorPos(startPos + new Vector2((buttonSize.X + spacing.X) * 2 , 0) + framePadding);
            ImGui.Text($"{-_yDelta}");
        }
        if(_yDelta > 0 )
        {
            ImGui.SetCursorPos(startPos + new Vector2(0, (buttonSize.Y + spacing.Y) * 2) + framePadding);
            ImGui.Text($"{_yDelta}");
        }
        if(_xDelta > 0 )
        {
            ImGui.SetCursorPos(startPos + (buttonSize + spacing) * 2 + framePadding);
            ImGui.Text($"{_xDelta}");
        }
        ImGui.SetCursorPos(endPos);
        ImGui.EndGroup();
        ImGui.Text("Delta X: " + _xDelta );
        ImGui.Text("Delta Y: " + _yDelta );
        if (ImGui.Button("Reverse"))
        {
            _xDelta = -_xDelta;
            _yDelta = -_yDelta;
        }
        
        ImGui.InputInt("X", ref _xDelta);
        ImGui.InputInt("Y", ref _yDelta);
        
        ImGui.Text(delta.ToString());
        
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