using System.Numerics;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class OptionsWindow : Window
{
    public override string Name => "Options";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
    
    private Vector4 _virtualLayerFillColor;
    private Vector4 _virtualLayerBorderColor;

    protected override void InternalDraw()
    {
        if (ImGui.BeginTabBar("Options"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                if (ImGui.Checkbox("Prefer Texture Map for land tiles", ref Config.Instance.PreferTexMaps))
                {
                    CEDGame.MapManager.Reset();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Virtual Layer"))
            {
                if (ImGui.ColorPicker4("Virtual Layer Fill Color", ref _virtualLayerFillColor))
                {
                    CEDGame.MapManager.MapEffect.VirtualLayerFillColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _virtualLayerFillColor.X,
                        _virtualLayerFillColor.Y,
                        _virtualLayerFillColor.Z,
                        _virtualLayerFillColor.W
                    );
                }
                if (ImGui.ColorPicker4("Virtual Layer Border Color", ref _virtualLayerBorderColor))
                {
                    CEDGame.MapManager.MapEffect.VirtualLayerBorderColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _virtualLayerBorderColor.X,
                        _virtualLayerBorderColor.Y,
                        _virtualLayerBorderColor.Z,
                        _virtualLayerBorderColor.W
                    );
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}