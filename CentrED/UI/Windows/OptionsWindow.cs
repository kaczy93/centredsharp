using System.Numerics;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class OptionsWindow : Window
{
    public override string Name => "Options";

    private Vector4 _virtualLayerFillColor;
    private Vector4 _virtualLayerBorderColor;
    

    public override void Draw()
    {
        if (!Show)
            return;

        ImGui.Begin("Options", ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
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
            
        ImGui.End();
    }
}