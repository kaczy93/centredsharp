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
    private int _lightLevel = 30;

    protected override void InternalDraw()
    {
        if (ImGui.BeginTabBar("Options"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                if (ImGui.SliderInt("LightLevel", ref _lightLevel, 0, 30))
                {
                    CEDGame.MapManager.MapEffect.LightLevel = (_lightLevel + 2) / 32f;
                }
                ImGui.Checkbox("Show NoDraw tiles", ref CEDGame.MapManager.ShowNoDraw);
                if (ImGui.Checkbox("Prefer Texture Map for land tiles", ref Config.Instance.PreferTexMaps))
                {
                    CEDGame.MapManager.Reset();
                }
                ImGui.Checkbox("Legacy mouse scroll behavior", ref Config.Instance.LegacyMouseScroll);
                UIManager.Tooltip("Mouse scroll up/down: elevate tile\nCtrl + Mouse scroll up/down: Zoom in/out");
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