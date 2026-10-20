using System.Drawing;
using CentrED.Map;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.Constants;

namespace CentrED.UI.Windows;

public class DebugWindow : Window
{
    public override string Name => "Debug";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.None;

    private int _gotoX;
    private int _gotoY;

    protected override void InternalDraw()
    {
        if (ImGui.BeginTabBar("DebugTabs"))
        {
            DrawGeneralTab();
            DrawPerformanceTab();
            DrawGhostTilesTab();
            ImGui.EndTabBar();
        }
    }

    private void DrawGeneralTab()
    {
        if (ImGui.BeginTabItem("General"))
        {
            ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
            var mapManager = App.MapManager;
            ImGui.Text
            (
                $"Resolution: {App.Window.ClientBounds.Width}x{App.Window.ClientBounds.Height}"
            );
            if (App.Client.Running)
            {
                ImGui.Text($"Land tiles: {mapManager.LandTilesCount}");
                ImGui.Text($"Static tiles: {mapManager.StaticsManager.Count}");
                ImGui.Text($"Animated Static tiles: {mapManager.StaticsManager.AnimatedTiles.Count}");
                ImGui.Text($"Light Tiles: {mapManager.StaticsManager.LightTiles.Count}");
                ImGui.Text($"Camera focus tile {mapManager.Camera.LookAt / TILE_SIZE}");
                var mousePos = ImGui.GetMousePos();
                ImGui.Text
                (
                    $"Virutal Layer Pos: {mapManager.Unproject((int)mousePos.X, (int)mousePos.Y, mapManager.VirtualLayerZ)}"
                );
                ImGui.Separator();
                ImGui.Text("Camera");
                var x = mapManager.TilePosition.X;
                var y = mapManager.TilePosition.Y;

                var cameraMoved = ImGuiEx.DragInt("Position x", ref x, 1, 0, App.Client.WidthInTiles - 1);
                cameraMoved |= ImGuiEx.DragInt("Position y", ref y, 1, 0, App.Client.HeightInTiles - 1);
                if (cameraMoved)
                {
                    mapManager.TilePosition = new Point(x, y);
                }
                if (ImGui.SliderFloat("Zoom", ref mapManager.Camera.Zoom, 0.2f, 4.0f))
                {
                    mapManager.Camera.Zoom = Math.Max(0.01f, mapManager.Camera.Zoom);
                }
                ImGui.NewLine();
                ImGui.SliderFloat("Yaw", ref mapManager.Camera.Yaw, -180.0f, 180.0f);
                ImGui.SliderFloat("Pitch", ref mapManager.Camera.Pitch, -180.0f, 180.0f);
                ImGui.SliderFloat("Roll", ref mapManager.Camera.Roll, -180.0f, 180.0f);
                ImGui.Separator();
                ImGui.Text("Misc");
                ImGui.Checkbox("Draw SelectionBuffer", ref App.MapManager.DebugDrawSelectionBuffer);
                ImGui.Checkbox("Draw LightMap", ref App.MapManager.DebugDrawLightMap);
            }
            ImGui.Checkbox("Debug Logging", ref App.MapManager.DebugLogging);
            ImGui.Checkbox("Debug Invalid Tiles", ref App.MapManager.DebugInvalidTiles);

            ImGui.Separator();
            if (ImGui.Button("Reload Shader"))
                mapManager.ReloadShader();
            ImGui.Separator();
            if (ImGui.Button("Test Window"))
                App.UIManager.ShowTestWindow = !App.UIManager.ShowTestWindow;
            ImGui.EndTabItem();
        }
    }

    private void DrawPerformanceTab()
    {
        if (ImGui.BeginTabItem("Performance"))
        {
            ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
            foreach (var nameValue in Metrics.Timers.OrderBy(t => t.Key))
            {
                ImGui.Text($"{nameValue.Key}: {nameValue.Value.TotalMilliseconds}ms");
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawGhostTilesTab()
    {
        var count = App.MapManager.GhostLandTiles.Values.Count +
                    App.MapManager.StaticsManager.GhostTiles.Count();
        if (ImGui.BeginTabItem("GhostTiles"))
        {
            ImGui.Text($"Ghost Tiles: {count}");
            if (ImGui.BeginTable("GhostTilesTable", 2))
            {
                foreach (var landTile in App.MapManager.GhostLandTiles.Values)
                {
                    DrawLand(landTile);
                }
                foreach (var staticTile in App.MapManager.StaticsManager.GhostTiles)
                {
                    DrawStatic(staticTile);
                }
                ImGui.EndTable();
            }
            ImGui.EndTabItem();
        }
    }
    
    private void DrawLand(LandObject lo)
    {
        var landTile = lo.LandTile;
        ImGui.TableNextRow();
        if (ImGui.TableNextColumn())
        {
            var spriteInfo = App.MapManager.Arts.GetLand(landTile.Id);
            App.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV);
        }
        if (ImGui.TableNextColumn())
        {
            ImGui.Text("Land " + App.MapManager.UoFileManager.TileData.LandData[landTile.Id].Name ?? "");
            ImGui.Text($"x:{landTile.X} y:{landTile.Y} z:{landTile.Z}");
            ImGui.Text($"id: {landTile.Id.FormatId()}");
        }
    }

    private void DrawStatic(StaticObject so)
    {
        var staticTile = so.StaticTile;
        ImGui.TableNextRow();
        if (ImGui.TableNextColumn())
        {
            var spriteInfo = App.MapManager.Arts.GetArt(staticTile.Id);
            var realBounds = App.MapManager.Arts.GetRealArtBounds(staticTile.Id);
            App.UIManager.DrawImage
            (
                spriteInfo.Texture,
                new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height)
            );
        }
        if (ImGui.TableNextColumn())
        {
            ImGui.Text("Static " + App.MapManager.UoFileManager.TileData.StaticData[staticTile.Id].Name);
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: {staticTile.Id.FormatId()} hue: {staticTile.Hue.FormatId()}");
        }
    }
}