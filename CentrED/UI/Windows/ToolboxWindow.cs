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
        
        // Add a title for the tool options section
        ImGui.Text("Tool Options");
        
        // Create an outer border for all tool options content
        ImGui.BeginChild("ToolOptionsContainer", new System.Numerics.Vector2(-1, -1), ImGuiChildFlags.Borders);
        
        // Draw the active tool's UI
        CEDGame.MapManager.ActiveTool.Draw();
        
        // End the bordered container
        ImGui.EndChild();
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