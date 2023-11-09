using ImGuiNET;

namespace CentrED.UI.Windows;

public class FilterWindow : Window
{
    public override string Name => "Filter";

    private int minZ = -127;
    private int maxZ = 127;

    public int MinZ => minZ;
    public int MaxZ => maxZ;

    public override void Draw()
    {
        if (!Show)
            return;
        ImGui.Begin(Name, ref _show);
        ImGui.SliderInt("Min Z render", ref minZ, -127, 127);
        ImGui.SliderInt("Max Z render", ref maxZ, -127, 127);
        ImGui.End();
    }
}