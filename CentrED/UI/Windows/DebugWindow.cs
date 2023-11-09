using System.Numerics;
using ImGuiNET;

namespace CentrED.UI.Windows;

public class DebugWindow : Window
{
    public override string Name => "Debug";

    private int _gotoX;
    private int _gotoY;
    private bool _showTestWindow;

    public override void Draw()
    {
        if (!Show)
            return;

        // ImGui.SetNextWindowPos(new Vector2(
        //         _graphicsDevice.PresentationParameters.BackBufferWidth / 2,
        //         _graphicsDevice.PresentationParameters.BackBufferHeight / 2
        //     ),
        //     ImGuiCond.FirstUseEver);
        var uiManager = Application.CEDGame.UIManager;
        var mapManager = Application.CEDGame.MapManager;
        ImGui.Begin(Name, ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.Text($"FPS: {uiManager._framesPerSecond:F1}");
        ImGui.Text
        (
            $"Resolution: {uiManager._graphicsDevice.PresentationParameters.BackBufferWidth}x{uiManager._graphicsDevice.PresentationParameters.BackBufferHeight}"
        );
        ImGui.Text($"Land tiles: {mapManager.LandTiles.Count}");
        ImGui.Text($"Static tiles: {mapManager.StaticTiles.Count}");
        ImGui.Text($"Camera focus tile {mapManager.Camera.LookAt / mapManager.TILE_SIZE}");
        ImGui.Separator();

        ImGui.Checkbox("DrawLand", ref mapManager.IsDrawLand);
        ImGui.Checkbox("DrawStatics", ref mapManager.IsDrawStatic);
        ImGui.Checkbox("DrawShadows", ref mapManager.IsDrawShadows);
        ImGui.SliderInt("Min Z render", ref mapManager.MIN_Z, -127, 127);
        ImGui.SliderInt("Max Z render", ref mapManager.MAX_Z, -127, 127);
        ImGui.SliderFloat("Zoom", ref mapManager.Camera.Zoom, 0.2f, 10.0f);
        ImGui.Separator();
        ImGui.InputInt("Camera x", ref _gotoX);
        ImGui.InputInt("Camera y", ref _gotoY);
        if (ImGui.Button("Update pos"))
        {
            mapManager.SetPos((ushort)_gotoX, (ushort)_gotoY);
        }

        ImGui.Separator();
        if (ImGui.Button("Server Flush"))
            mapManager.Client.Flush();
        if (ImGui.Button("Clear cache"))
            mapManager.Reset();
        // if (ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
        if (ImGui.Button("Test Window"))
            _showTestWindow = !_showTestWindow;
        if (_showTestWindow)
        {
            ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref _showTestWindow);
        }
        ImGui.End();
    }
}