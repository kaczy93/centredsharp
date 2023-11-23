using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

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
        ImGui.Text($"Map UpdateTime: {mapManager.UpdateTime.TotalMilliseconds}ms");
        ImGui.Text($"Map DrawTime: {mapManager.DrawTime.TotalMilliseconds}ms");
        ImGui.Text($"UI UpdateTime: {uiManager.UpdateTime.TotalMilliseconds}ms");
        ImGui.Text($"UI DrawTime: {uiManager.DrawTime.TotalMilliseconds}ms");
        ImGui.Text
        (
            $"Resolution: {uiManager._graphicsDevice.PresentationParameters.BackBufferWidth}x{uiManager._graphicsDevice.PresentationParameters.BackBufferHeight}"
        );
        ImGui.Text($"Land tiles: {mapManager.LandTiles.Count}");
        ImGui.Text($"Static tiles: {mapManager.StaticTiles.Count}");
        ImGui.Text($"Camera focus tile {mapManager.Camera.LookAt / mapManager.TILE_SIZE}");
        ImGui.Separator();

        ImGui.SliderFloat("Zoom", ref mapManager.Camera.Zoom, 0.2f, 4.0f);
        ImGui.Separator();
        ImGui.InputInt("Camera x", ref _gotoX);
        ImGui.InputInt("Camera y", ref _gotoY);
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