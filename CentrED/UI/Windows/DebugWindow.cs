using System.Globalization;
using System.Xml.Serialization;
using CentrED.IO;
using CentrED.IO.Models;
using CentrED.IO.Models.Centredplus;
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
    public bool ClassicUONormals;

    protected override void InternalDraw()
    {
        if (ImGui.BeginTabBar("DebugTabs"))
        {
            DrawGeneralTab();
            DrawGhostTilesTab();
            DrawLandBrushTab();
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
            if (ImGui.Checkbox("ClassicUO Normals", ref ClassicUONormals))
            {
                mapManager.Reset();
            }
            ImGui.Checkbox("Draw SelectionBuffer", ref CEDGame.MapManager.DebugDrawSelectionBuffer);

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

    private string _tilesBrushPath = "TilesBrush.xml";
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));
    private static TilesBrush _tilesBrush;

    private void DrawLandBrushTab()
    {
        if (ImGui.BeginTabItem("LandBrush"))
        {
            ImGui.InputText("File", ref _tilesBrushPath, 512);
            ImGui.SameLine();
            if (ImGui.Button("..."))
            {
                ImGui.OpenPopup("open-file");
            }
            var isOpen = true;
            if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar))
            {
                var picker = FilePicker.GetFilePicker(this, Environment.CurrentDirectory, ".xml");
                if (picker.Draw())
                {
                    _tilesBrushPath = picker.SelectedFile;
                    FilePicker.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }
            if (ImGui.Button("Read"))
            {
                try
                {
                    using var reader = new FileStream(_tilesBrushPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _tilesBrush = (TilesBrush)_xmlSerializer.Deserialize(reader)!;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            if (_tilesBrush != null)
            {
                if (ImGui.Button("Import"))
                {
                    ImportLandBrush();
                    CEDGame.MapManager.InitLandBrushes();
                }
                foreach (var brush in _tilesBrush.Brush)
                {
                    ImGui.Text($"Brush({brush.Id}), {brush.Name}");
                    ImGui.Indent();
                    foreach (var land in brush.Land)
                    {
                        ImGui.Text($"Land({land.ID}), {land.Chance}");
                    }
                    foreach (var edge in brush.Edge)
                    {
                        ImGui.Text($"Edge to {edge.To}");
                        ImGui.Indent();
                        foreach (var edgeLand in edge.Land)
                        {
                            ImGui.Text($"Land({edgeLand.Type}) {edgeLand.ID}");
                        }
                        ImGui.Unindent();
                    }
                    ImGui.Unindent();
                }
            }

            ImGui.EndTabItem();
        }
    }

    private void ImportLandBrush()
    {
        var target = ProfileManager.ActiveProfile.LandBrush;
        target.Clear();
        foreach (var brush in _tilesBrush.Brush)
        {
            var newBrush = new LandBrush();
            newBrush.Name = brush.Name;
            foreach (var land in brush.Land)
            {
                if (TryParseHex(land.ID, out var newId))
                {
                    newBrush.Tiles.Add(newId);
                }
                else
                {
                    Console.WriteLine($"Unable to parse land ID {land.ID} in brush {brush.Id}");
                }
            }
            foreach (var edge in brush.Edge)
            {
                var to = _tilesBrush.Brush.Find(b => b.Id == edge.To);
                var newList = new List<LandBrushTransition>();
                foreach (var edgeLand in edge.Land)
                {
                    if (TryParseHex(edgeLand.ID, out var newId))
                    {
                        var newType = ConvertType(edgeLand.Type);
                        newList.Add(new LandBrushTransition{TileID =  newId, Direction = newType});
                    }
                    else
                    {
                        Console.WriteLine($"Unable to parse edgeland ID {edgeLand.ID} in brush {brush.Id}");
                    }
                }
                newBrush.Transitions.Add(to.Name, newList);
            }
            target.Add(newBrush.Name, newBrush);
        }
    }

    private bool TryParseHex(string value, out ushort result)
    {
        //Substring removes 0x from the value
        return ushort.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private Direction ConvertType(string oldType)
    {
        switch (oldType)
        {
            case "DR": return Direction.Up;
            case "DL": return Direction.Right;
            case "UL": return Direction.Down;
            case "UR": return Direction.Left;
            case "LL": return Direction.Down | Direction.East | Direction.Right ;
            case "UU": return Direction.Left | Direction.South | Direction.Down;
            //File mentions type FF but it's never used
            // "FF" => 
            default:
                Console.WriteLine("Unknown type " + oldType);
                return 0;
        }
    }


    private void DrawLand(LandObject lo)
    {
        var landTile = lo.LandTile;
        ImGui.TableNextRow();
        if (ImGui.TableNextColumn())
        {
            var texture = ArtLoader.Instance.GetLandTexture(landTile.Id, out var bounds);
            CEDGame.UIManager.DrawImage(texture, bounds);
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
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
            CEDGame.UIManager.DrawImage
            (
                texture,
                new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height)
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