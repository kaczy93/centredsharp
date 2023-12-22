using CentrED.IO.Models;
using CentrED.Tools;
using ImGuiNET;
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
        CEDGame.UIManager.Tools.ForEach(ToolButton);
        ImGui.Separator();
        ImGui.Text("Tool Options");
        CEDGame.MapManager.ActiveTool?.Draw();
    }

    private void ToolButton(Tool tool)
    {
        if (ImGui.RadioButton(tool.Name, CEDGame.MapManager.ActiveTool == tool))
        {
            CEDGame.MapManager.ActiveTool?.OnDeactivated(CEDGame.MapManager.Selected);
            CEDGame.MapManager.ActiveTool = tool;
            CEDGame.MapManager.ActiveTool?.OnActivated(CEDGame.MapManager.Selected);
        }
    }
}