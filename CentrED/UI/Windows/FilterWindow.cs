using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class FilterWindow : Window
{
    public override string Name => "Filter";

    public override void Draw()
    {
        if (!Show)
            return;
        ImGui.Begin(Name, ref _show);
        ImGui.SliderInt("Min Z render", ref CEDGame.MapManager.minZ, -127, 127);
        ImGui.SliderInt("Max Z render", ref CEDGame.MapManager.maxZ, -127, 127);
        ImGui.Text("Draw: ");
        ImGui.Checkbox("Land", ref CEDGame.MapManager.IsDrawLand);
        ImGui.SameLine();
        ImGui.Checkbox("Statics", ref CEDGame.MapManager.IsDrawStatics);
        ImGui.SameLine();
        ImGui.Checkbox("Shadows", ref CEDGame.MapManager.IsDrawShadows); 
        ImGui.End();
    }
}