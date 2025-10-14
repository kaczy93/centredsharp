using CentrED.Client;
using CentrED.IO;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using static CentrED.LangEntry;
using RadarMap = CentrED.Map.RadarMap;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI.Windows;

public class MinimapWindow : Window
{
    public override string Name => LangManager.Get(MINIMAP_WINDOW) + "###Minimap";

    private string _inputFavoriteName = "";
    private string _favoriteToDelete = "";
    private int[] mapPos = new int[2];
    private bool _showError = true;
    private bool _showConfirmation = true;

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        ImGui.Text(LangManager.Get(FAVORITES));
        if (ImGui.BeginChild("Favorites", new Vector2(RadarMap.Instance.Texture.Width, 100)))
        {
            ImGui.InputText(LangManager.Get(NAME), ref _inputFavoriteName, 64);
            ImGui.SameLine();
            ImGui.BeginDisabled(string.IsNullOrEmpty(_inputFavoriteName) || 
                                ProfileManager.ActiveProfile.RadarFavorites.ContainsKey(_inputFavoriteName));
            if (ImGui.Button(LangManager.Get(ADD)))
            {
                ProfileManager.ActiveProfile.RadarFavorites.Add
                (
                    _inputFavoriteName,
                    new()
                    {
                        X = (ushort)CEDGame.MapManager.TilePosition.X,
                        Y = (ushort)CEDGame.MapManager.TilePosition.Y
                    }
                );
                ProfileManager.Save();
                _inputFavoriteName = "";
            }
            ImGui.EndDisabled();
            
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
                    CEDGame.MapManager.TilePosition = new Point(coords.X, coords.Y);
                }
                ImGuiEx.Tooltip($"X:{coords.X} Y:{coords.Y}");

                ImGui.SetCursorPos(cursorPosition + new Vector2(ImGui.GetItemRectSize().X, 0));

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                if (ImGui.Button($"x##{name}"))
                {
                    _favoriteToDelete = name;
                    ImGui.OpenPopup("DeleteFavorite");
                    _showConfirmation = true;
                }
                ImGui.PopStyleColor(2);
            }
            if (ImGui.BeginPopupModal
                (
                    "DeleteFavorite",
                    ref _showConfirmation,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text(string.Format(LangManager.Get(DELETE_WARNING_1TYPE_2NAME), LangManager.Get(FAVORITE).ToLower(), _favoriteToDelete));
                if (ImGui.Button(LangManager.Get(YES)))
                {
                    if (!string.IsNullOrEmpty(_favoriteToDelete))
                    {
                        ProfileManager.ActiveProfile.RadarFavorites.Remove(_favoriteToDelete);
                        ProfileManager.Save();
                    }
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(LangManager.Get(NO)))
                {
                    ImGui.CloseCurrentPopup();
                }
                _favoriteToDelete = "";
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
        ImGui.Separator();
        ImGui.PushItemWidth(100);
        if (ImGui.InputInt2("X/Y", ref mapPos[0]))
        {
            CEDGame.MapManager.TilePosition = new Point(mapPos[0], mapPos[1]);
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
                if (ImGui.Button(LangManager.Get(RELOAD)))
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
                    CEDGame.MapManager.TilePosition = new Point((int)newPos.X, (int)newPos.Y);
                }
                mapPos[0] = (int)newPos.X;
                mapPos[1] = (int)newPos.Y;
            }
            else
            {
                mapPos[0] = CEDGame.MapManager.TilePosition.X;
                mapPos[1] = CEDGame.MapManager.TilePosition.Y;
            }

            var rect = CEDGame.MapManager.ViewRange;
            var p1 = currentPos + new Vector2(rect.Left / 8, rect.Center.Y / 8);
            var p2 = currentPos + new Vector2(rect.Center.X / 8, rect.Top / 8);
            var p3 = currentPos + new Vector2(rect.Right / 8, rect.Center.Y / 8);
            var p4 = currentPos + new Vector2(rect.Center.X / 8, rect.Bottom / 8);
            ImGui.GetWindowDrawList().AddQuad(p1, p2, p3, p4, ImGui.GetColorU32(ImGuiColor.Red));
            CEDGame.UIManager.GetWindow<LSOWindow>().DrawArea(currentPos);
            CEDGame.UIManager.GetWindow<ServerAdminWindow>().DrawArea(currentPos);
        }
        ImGui.EndChild();
    }
}