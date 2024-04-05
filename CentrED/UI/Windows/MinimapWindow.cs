using CentrED.Client;
using CentrED.IO;
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
    private int[] mapPos = new int[2];
    private bool _showError = true;
    private bool _showConfirmation = true;

    protected override void InternalDraw()
    {
        if (!CEDGame.MapManager.Client.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        ImGui.InputText("Favorite Name", ref _inputFavoriteName, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add Favorite"))
        {
            if (string.IsNullOrEmpty(_inputFavoriteName) || ProfileManager.ActiveProfile.RadarFavorites.ContainsKey
                    (_inputFavoriteName))
            {
                ImGui.OpenPopup("Error");
                _showError = true;
            }
            else
            {
                ProfileManager.ActiveProfile.RadarFavorites.Add
                (
                    _inputFavoriteName,
                    new()
                    {
                        X = (ushort)CEDGame.MapManager.Position.X,
                        Y = (ushort)CEDGame.MapManager.Position.Y
                    }
                );
                ProfileManager.Save();
                _inputFavoriteName = "";
            }
        }

        if (ImGui.BeginChild("Favorites", new Vector2(RadarMap.Instance.Texture.Width, 100)))
        {
            foreach (var (name, coords) in ProfileManager.ActiveProfile.RadarFavorites)
            {
                if (name != ProfileManager.ActiveProfile.RadarFavorites.First().Key)
                {
                    ImGui.SameLine();
                }
                if (ImGui.GetCursorPos().X + 75 >= RadarMap.Instance.Texture.Width)
                {
                    ImGui.NewLine();
                }

                var cursorPosition = ImGui.GetCursorPos();

                //tooltip for button what shows the key
                if (ImGui.Button($"{name}", new Vector2(75, 19)))
                {
                    CEDGame.MapManager.Position = new Point(coords.X, coords.Y);
                }
                UIManager.Tooltip($"{name}\nX:{coords.X} Y:{coords.Y}");

                ImGui.SetCursorPos(cursorPosition + new Vector2(ImGui.GetItemRectSize().X, 0));

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                if (ImGui.Button($"x##{name}"))
                {
                    _keyToDelete = name;
                    ImGui.OpenPopup("Confirmation");
                    _showConfirmation = true;
                }
                ImGui.PopStyleColor(2);
            }
            if (ImGui.BeginPopupModal
                (
                    "Confirmation",
                    ref _showConfirmation,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text("Are you sure you want to delete this favorite?");
                if (ImGui.Button("Yes"))
                {
                    if (!string.IsNullOrEmpty(_keyToDelete))
                    {
                        ProfileManager.ActiveProfile.RadarFavorites.Remove(_keyToDelete);
                        ProfileManager.Save();
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

        if (ImGui.BeginPopupModal
                ("Error", ref _showError, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextColored(new Vector4(1.0f, .0f, .0f, 1.0f), "Name already exists or empty value!");

            if (ImGui.Button("Ok"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        ImGui.PushItemWidth(100);
        if (ImGui.InputInt2("X/Y", ref mapPos[0]))
        {
            CEDGame.MapManager.Position = new Point(mapPos[0], mapPos[1]);
        };
        ImGui.PopItemWidth();
        if (ImGui.BeginChild("Minimap", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            var currentPos = ImGui.GetCursorScreenPos();
            var tex = RadarMap.Instance.Texture;
            CEDGame.UIManager.DrawImage(tex, tex.Bounds);

            ImGui.SetCursorScreenPos(currentPos);
            //So we can easily interact with minimap
            ImGui.InvisibleButton("MinimapInvButton", new Vector2(tex.Width, tex.Height));
            var hovered = ImGui.IsItemHovered();
            var held = ImGui.IsItemActive();

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Button("Reload"))
                {
                    CEDClient.Send(new RequestRadarMapPacket());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            if (hovered)
            {
                var newPos = (ImGui.GetMousePos() - currentPos) * 8;
                if (held)
                {
                    CEDGame.MapManager.Position = new Point((int)newPos.X, (int)newPos.Y);
                }
                mapPos[0] = (int)newPos.X;
                mapPos[1] = (int)newPos.Y;
            }
            else
            {
                mapPos[0] = CEDGame.MapManager.Position.X;
                mapPos[1] = CEDGame.MapManager.Position.Y;
            }

            var rect = CEDGame.MapManager.ViewRange;
            var p1 = currentPos + new Vector2(rect.Left / 8, rect.Center.Y / 8);
            var p2 = currentPos + new Vector2(rect.Center.X / 8, rect.Top / 8);
            var p3 = currentPos + new Vector2(rect.Right / 8, rect.Center.Y / 8);
            var p4 = currentPos + new Vector2(rect.Center.X / 8, rect.Bottom / 8);
            ImGui.GetWindowDrawList().AddQuad(p1, p2, p3, p4, ImGui.GetColorU32(UIManager.Red));
            ImGui.EndChild();
        }
    }
}