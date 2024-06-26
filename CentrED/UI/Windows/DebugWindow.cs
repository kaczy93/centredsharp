using CentrED.Map;
using ClassicUO.Assets;
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
        if (ImGui.BeginTabBar("DebugTabs"))
        {
            DrawGeneralTab();
            DrawGhostTilesTab();
            ImGui.EndTabBar();
        }
    }

    private void DrawGeneralTab()
    {
        if (ImGui.BeginTabItem("General"))
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
            ImGui.Text($"Camera focus tile {mapManager.Camera.LookAt / TileObject.TILE_SIZE}");
            var mousePos = ImGui.GetMousePos();
            ImGui.Text
            (
                $"Virutal Layer Pos: {mapManager.Unproject((int)mousePos.X, (int)mousePos.Y, mapManager.VirtualLayerZ)}"
            );
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
            ImGui.Checkbox("Draw SelectionBuffer", ref CEDGame.MapManager.DebugDrawSelectionBuffer);
            ImGui.Checkbox("Draw LightMap", ref CEDGame.MapManager.DebugDrawLightMap);
            ImGui.Checkbox("Debug Logging", ref CEDGame.MapManager.DebugLogging);

            ImGui.Separator();
            if (ImGui.Button("Server Flush"))
                mapManager.Client.Flush();
            if (ImGui.Button("Reload Shader"))
                mapManager.ReloadShader();
            // if (ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
            if (ImGui.Button("Test Window"))
                _showTestWindow = !_showTestWindow;
            if (_showTestWindow)
            {
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showTestWindow);
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawGhostTilesTab()
    {
        if (ImGui.BeginTabItem("GhostTiles"))
        {
            if (ImGui.BeginTable("GhostTilesTable", 2))
            {
                foreach (var landTile in CEDGame.MapManager.GhostLandTiles.Values)
                {
                    DrawLand(landTile);
                }
                foreach (var staticTile in CEDGame.MapManager.GhostStaticTiles.Values)
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
            var spriteInfo = CEDGame.MapManager.Arts.GetLand(landTile.Id);
            CEDGame.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV);
        }
        if (ImGui.TableNextColumn())
        {
            ImGui.Text("Land " + TileDataLoader.Instance.LandData[landTile.Id].Name ?? "");
            ImGui.Text($"x:{landTile.X} y:{landTile.Y} z:{landTile.Z}");
            ImGui.Text($"id: 0x{landTile.Id:X4} ({landTile.Id})");
        }
    }

    private void DrawStatic(StaticObject so)
    {
        var staticTile = so.StaticTile;
        ImGui.TableNextRow();
        if (ImGui.TableNextColumn())
        {
            var spriteInfo = CEDGame.MapManager.Arts.GetArt(staticTile.Id);
            var realBounds = CEDGame.MapManager.Arts.GetRealArtBounds(staticTile.Id);
            CEDGame.UIManager.DrawImage
            (
                spriteInfo.Texture,
                new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height)
            );
        }
        if (ImGui.TableNextColumn())
        {
            ImGui.Text("Static " + TileDataLoader.Instance.StaticData[staticTile.Id].Name);
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id}) hue: 0x{staticTile.Hue:X4} ({staticTile.Hue})");
        }
    }
}