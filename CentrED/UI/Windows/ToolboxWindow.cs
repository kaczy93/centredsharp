using CentrED.Tools;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ToolboxWindow : Window
{
    public override string Name => "Toolbox";

    public override void Draw()
    {
        if (!Show)
            return;

        ImGui.Begin(Name, ref _show);
        CEDGame.UIManager.Tools.ForEach(ToolButton);
        ImGui.End();
        CEDGame.MapManager.ActiveTool?.DrawWindow();
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