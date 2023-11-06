using CentrED.Client;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadarMap = CentrED.Map.RadarMap;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI;

public partial class UIManager {
    public static Vector4 Red = new (1, 0, 0, 1);
    public static Vector4 Green = new (0, 1, 0, 1);
    public static Vector4 Blue = new (0, 0, 1, 1);
    
    private void CenterWindow() {
        ImGui.SetWindowPos( 
            new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2 - ImGui.GetWindowSize().X / 2,
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2 - ImGui.GetWindowSize().Y / 2)
            , ImGuiCond.FirstUseEver
        );
    }
    
    private bool _optionsShowWindow;
    private void DrawOptionsWindow() {
        if (!_optionsShowWindow) return;
        
        ImGui.Begin("Options", ref _optionsShowWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        CenterWindow();
        ImGui.Text("Nothing to see here (yet) :)");
        ImGui.End();
    }
    
    private bool _minimapShowWindow;

    private void DrawMinimapWindow() {
        if (!_minimapShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Minimap", ref _minimapShowWindow);
        if (ImGui.Button("Reload")) {
            _mapManager.Client.Send(new RequestRadarMapPacket());
        }
        if (_mapManager.Client.Initialized) {
            var currentPos = ImGui.GetCursorScreenPos();
            var tex = RadarMap.Instance.Texture;
            DrawImage(tex, tex.Bounds);
            if (ImGui.IsMouseHoveringRect(currentPos, new(currentPos.X + tex.Bounds.Width, currentPos.Y + tex.Bounds.Height), true)) {
                if (ImGui.IsWindowFocused() && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                    var coords = ImGui.GetMousePos() - currentPos;
                    _mapManager.SetPos((ushort)(coords.X * 8), (ushort)(coords.Y * 8));
                }
            }
            _mapManager.CalculateViewRange(_mapManager.Camera, out var rect);
            var p1 = currentPos + new Vector2(rect.Left / 8, rect.Top / 8);
            var p2 = currentPos + new Vector2(rect.Right / 8, rect.Bottom / 8);
            ImGui.GetWindowDrawList().AddRect(p1, p2, ImGui.GetColorU32(Red));
        }
        ImGui.End();
    }
}