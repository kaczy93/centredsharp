using System.Numerics;
using ImGuiNET;

namespace CentrED.UI.Windows; 

public class DebugWindow : Window {
    public DebugWindow(UIManager uiManager) : base(uiManager) { }
    public override string Name => "Debug";
    
    private int _gotoX;
    private int _gotoY;
    private bool _showTestWindow;
    
    public override void Draw() {
        if (!Show) return;

        // ImGui.SetNextWindowPos(new Vector2(
        //         _graphicsDevice.PresentationParameters.BackBufferWidth / 2,
        //         _graphicsDevice.PresentationParameters.BackBufferHeight / 2
        //     ),
        //     ImGuiCond.FirstUseEver);
        ImGui.Begin(Id, ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.Text($"FPS: {_uiManager._framesPerSecond:F1}");
        ImGui.Text($"Resolution: {_uiManager._graphicsDevice.PresentationParameters.BackBufferWidth}x{_uiManager._graphicsDevice.PresentationParameters.BackBufferHeight}");
        ImGui.Text($"Land tiles: {_uiManager._mapManager.LandTiles.Count}");
        ImGui.Text($"Static tiles: {_uiManager._mapManager.StaticTiles.Count}");
        ImGui.Text($"Camera focus tile {_uiManager._mapManager.Camera.LookAt / _uiManager._mapManager.TILE_SIZE}");
        ImGui.Separator();

        ImGui.Checkbox("DrawLand", ref _uiManager._mapManager.IsDrawLand);
        ImGui.Checkbox("DrawStatics", ref _uiManager._mapManager.IsDrawStatic);
        ImGui.Checkbox("DrawShadows", ref _uiManager._mapManager.IsDrawShadows);
        ImGui.SliderInt("Min Z render", ref _uiManager._mapManager.MIN_Z, -127, 127);
        ImGui.SliderInt("Max Z render", ref _uiManager._mapManager.MAX_Z, -127, 127);
        ImGui.SliderFloat("Zoom", ref _uiManager._mapManager.Camera.Zoom, 0.2f, 10.0f);
        ImGui.Separator();
        ImGui.InputInt("Camera x", ref _gotoX);
        ImGui.InputInt("Camera y", ref _gotoY);
        if (ImGui.Button("Update pos")) {
            _uiManager._mapManager.SetPos((ushort)_gotoX, (ushort)_gotoY);
        }

        ImGui.Separator();
        if (ImGui.Button("Server Flush")) _uiManager._mapManager.Client.Flush();
        if (ImGui.Button("Clear cache")) _uiManager._mapManager.Reset();
        // if (ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
        if (ImGui.Button("Test Window")) _showTestWindow = !_showTestWindow;
        if (_showTestWindow)
        {
            ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref _showTestWindow);
        }
        ImGui.End();
    }
}