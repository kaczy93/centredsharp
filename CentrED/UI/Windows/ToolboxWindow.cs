using CentrED.IO.Models;
using CentrED.Tools;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ToolboxWindow : Window
{
    public override string Name => "Toolbox";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    protected override void InternalDraw()
    {
        CEDGame.MapManager.Tools.ForEach(ToolButton);
        ImGui.Separator();
        ImGui.Text("Tool Options");
        CEDGame.MapManager.ActiveTool.Draw();
    }

    private void ToolButton(Tool tool)
    {
        if (ImGui.RadioButton(tool.Name, CEDGame.MapManager.ActiveTool == tool))
        {
            CEDGame.MapManager.ActiveTool = tool;
        }
        ImGui.SameLine();
        if (tool.Shortcut != Keys.None)
        {
            ImGui.TextDisabled(tool.Shortcut.ToString());
        }
    }
}