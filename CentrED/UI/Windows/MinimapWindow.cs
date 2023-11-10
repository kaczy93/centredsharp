using CentrED.Client;
using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.Server.Map;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using RadarMap = CentrED.Map.RadarMap;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI.Windows;

public class MinimapWindow : Window
{
    public override string Name => "Minimap";

    private string _inputFavoriteName = "";
    private string _keyToDelete = "";
    private string _coordsText = "";
    public override void Draw()
    {
        if (!Show) return;

        ImGui.Begin("Minimap", ref _show, ImGuiWindowFlags.AlwaysAutoResize);

        if (CEDGame.MapManager.Client.Initialized)
        {

            ImGui.InputText("Favorite Name", ref _inputFavoriteName, 64);
            ImGui.SameLine();

            if (ImGui.Button("Add Favorite"))
            {

                if (string.IsNullOrEmpty(_inputFavoriteName) || ProfileManager.ActiveProfile.RadarFavorites.ContainsKey(_inputFavoriteName))
                {
                    ImGui.OpenPopup("Error");
                }
                else
                {
                    ProfileManager.ActiveProfile.RadarFavorites.Add(_inputFavoriteName, new()
                    {
                        X = (ushort)CEDGame.MapManager.Position.X,
                        Y = (ushort)CEDGame.MapManager.Position.Y
                    });
                    ProfileManager.Save(ProfileManager.ActiveProfile);
                    _inputFavoriteName = "";
                }

            }



            if (ImGui.BeginChild("ButtonGroup", new Vector2(RadarMap.Instance.Texture.Width, 100), true))
            {

                foreach (var (key, value) in ProfileManager.ActiveProfile.RadarFavorites)
                {

                    if (key != ProfileManager.ActiveProfile.RadarFavorites.First().Key)
                    {
                        ImGui.SameLine();
                    }


                    if (ImGui.GetCursorPos().X + 75 >= RadarMap.Instance.Texture.Width)
                    {
                        ImGui.NewLine();
                    }

                    var cursorPosition = ImGui.GetCursorPos();

                    if (ImGui.Button($"{key}", new Vector2(75, 19)))
                    {
                        CEDGame.MapManager.Position = new Point(value.X, value.Y);
                    }
                    ImGui.SetCursorPos(cursorPosition + new Vector2(ImGui.GetItemRectSize().X, 0));


                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                    if (ImGui.Button($"x##{key}"))
                    {

                        _keyToDelete = key;
                        ImGui.OpenPopup("Confirmation");


                    }
                    ImGui.PopStyleColor(2);


                }
                if (ImGui.BeginPopupModal("Confirmation", ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.Text("Are you sure you want to delete this favorite?");

                    if (ImGui.Button("Yes"))
                    {
                        if (!string.IsNullOrEmpty(_keyToDelete))
                        {
                            ProfileManager.ActiveProfile.RadarFavorites.Remove(_keyToDelete);
                            ProfileManager.Save(ProfileManager.ActiveProfile);
                            _keyToDelete = "";
                        }
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("No"))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
                ImGui.EndChild();
            }


            if (ImGui.BeginPopupModal("Error", ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.TextColored(new Vector4(1.0f, .0f, .0f, 1.0f), "Name already exists or empty value!");

                if (ImGui.Button("Ok"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }


            var currentPos = ImGui.GetCursorScreenPos();
            var tex = RadarMap.Instance.Texture;
            CEDGame.UIManager.DrawImage(tex, tex.Bounds);
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Button("Reload"))
                {
                    CEDClient.Send(new RequestRadarMapPacket());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.IsMouseHoveringRect(currentPos, new(currentPos.X + tex.Bounds.Width, currentPos.Y + tex.Bounds.Height), true))
            {
                var coords = ImGui.GetMousePos() - currentPos;
                if (ImGui.IsWindowFocused() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    CEDGame.MapManager.Position = new Point((int)(coords.X * 8), (int)(coords.Y * 8));
                }
                _coordsText = $"x:{coords.X * 8} y:{coords.Y * 8}";

            }
            CEDGame.MapManager.CalculateViewRange(CEDGame.MapManager.Camera, out var rect);
            var p1 = currentPos + new Vector2(rect.Left / 8, rect.Top / 8);
            var p2 = currentPos + new Vector2(rect.Right / 8, rect.Bottom / 8);
            ImGui.GetWindowDrawList().AddRect(p1, p2, ImGui.GetColorU32(UIManager.Red));
        }
        ImGui.Text(_coordsText);
        ImGui.End();
    }
}