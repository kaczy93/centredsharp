using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class DebugWindow : Window
{
    public override string Name => "Debug";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;

    private int _gotoX;
    private int _gotoY;
    private bool _showTestWindow;

    protected override void InternalDraw()
    {
        var uiManager = CEDGame.UIManager;
        var mapManager = CEDGame.MapManager;
        ImGui.Text($"FPS: {uiManager.FramesPerSecond:F1}");
        foreach (var nameValue in Metrics.Values)
        {
            ImGui.Text($"{nameValue.Key}: {nameValue.Value.TotalMilliseconds}ms");
        }
        ImGui.Separator();
        ImGui.Text
        (
            $"Resolution: {uiManager._graphicsDevice.PresentationParameters.BackBufferWidth}x{uiManager._graphicsDevice.PresentationParameters.BackBufferHeight}"
        );
        ImGui.Text($"Land tiles: {mapManager.LandTilesCount}");
        ImGui.Text($"Static tiles: {mapManager.StaticTilesCount}");
        ImGui.Text($"Camera focus tile {mapManager.Camera.LookAt / mapManager.TILE_SIZE}");
        ImGui.Separator();

        ImGui.SliderFloat("Zoom", ref mapManager.Camera.Zoom, 0.2f, 4.0f);
        ImGui.Separator();
        ImGui.InputInt("Camera x", ref _gotoX);
        ImGui.InputInt("Camera y", ref _gotoY);
        if (ImGui.Button("Undo"))
        {
            CEDClient.Undo();
        }
        if (ImGui.Button("Update pos"))
        {
            mapManager.Position = new Point(_gotoX, _gotoY);
        }

        ImGui.Separator();
        if (ImGui.Button("Server Flush"))
            mapManager.Client.Flush();
        if (ImGui.Button("Clear cache"))
            mapManager.Reset();
        if(ImGui.Button("Reload Shader"))
            mapManager.ReloadShader();
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