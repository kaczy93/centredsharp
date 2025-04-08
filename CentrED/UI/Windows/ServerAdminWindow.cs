using System.Numerics;
using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using static CentrED.Application;
using static ImGuiNET.ImGuiChildFlags;

namespace CentrED.UI.Windows;

public class ServerAdminWindow : Window
{
    public ServerAdminWindow()
    {
        CEDClient.RegionModified += (name, status) =>
        {
            if (status == ModifyRegionStatus.Modified && regions_selected_index != -1 && regions_selected_index < CEDClient.Admin.Regions.Count)
            {
                regions_selected = CEDClient.Admin.Regions[regions_selected_index];
            }
        };
    }
    public override bool Enabled => CEDClient.Initialized && CEDClient.AccessLevel >= AccessLevel.Administrator;
    public override string Name => "Server Administration";

    public override void OnShow()
    {
        if (CEDClient.Initialized)
        {
            CEDClient.Send(new ListUsersPacket());
            CEDClient.Send(new ListRegionsPacket());
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        if (ImGui.Button("Server Save"))
        {
            CEDClient.Flush();
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop Server"))
        {
            CEDClient.Send(new ServerStopPacket("Server is shutting down"));
        }
        ImGui.Separator();
        if (ImGui.BeginChild("Users/Regions"))
        {
            if (ImGui.BeginTabBar("Users/Regions"))
            {
                DrawUsersTab();
                DrawRegionsTab();
                ImGui.EndTabBar();
            }
            ImGui.EndChild();
        }
    }

    private int users_selected = -1;
    private string users_new_username = "";
    private string users_new_password = "";
    private bool users_show_add_user;
    private bool users_show_remove_user;
    private bool users_show_change_password;

    private void DrawUsersTab()
    {
        if (ImGui.BeginTabItem("Users"))
        {
            if (ImGui.BeginChild("UserList", new(150, 0), Borders | ResizeX))
            {
                for (var i = 0; i < CEDClient.Admin.Users.Count; i++)
                {
                    var user = CEDClient.Admin.Users[i];
                    if (ImGui.Selectable(user.Username, users_selected == i))
                    {
                        users_selected = i;
                    }
                }
                ImGui.EndChild();
            }
            ImGui.SameLine();
            ImGui.BeginGroup();
            if (ImGui.Button("Refresh"))
            {
                CEDClient.Send(new ListUsersPacket());
            }
            if (ImGui.Button("Add User"))
            {
                ImGui.OpenPopup("AddUser");
                users_show_add_user = true;
            }
            if (ImGui.BeginPopupModal("AddUser", ref users_show_add_user, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("Username", ref users_new_username, 32);
                ImGui.InputText("Password", ref users_new_password, 32, ImGuiInputTextFlags.Password);
                if (ImGui.Button("Add"))
                {
                    CEDClient.Send(new ModifyUserPacket(users_new_username, users_new_password, AccessLevel.None, []));
                    ImGui.CloseCurrentPopup();
                    users_selected = CEDClient.Admin.Users.Count;
                    users_new_username = "";
                    users_new_password = "";
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (users_selected == -1)
                ImGui.BeginDisabled();
            if (ImGui.Button("Remove User"))
            {
                ImGui.OpenPopup("RemoveUser");
                users_show_remove_user = true;
            }
            if (ImGui.BeginPopupModal("RemoveUser", ref users_show_remove_user, ImGuiWindowFlags.AlwaysAutoResize))
            {
                var user = CEDClient.Admin.Users[users_selected];
                ImGui.Text($"Are you sure you want to remove user: {user.Username}");
                if (ImGui.Button("Yes"))
                {
                    CEDClient.Send(new DeleteUserPacket(user.Username));
                    ImGui.CloseCurrentPopup();
                    users_selected -= 1;
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (users_selected == -1)
                ImGui.EndDisabled();

            ImGui.Separator();
            if (users_selected != -1 && users_selected < CEDClient.Admin.Users.Count)
            {
                var user = CEDClient.Admin.Users[users_selected];
                var names = Enum.GetNames(typeof(AccessLevel));
                var acessLevelIndex = Array.IndexOf(names, user.AccessLevel.ToString());
                ImGui.PushItemWidth(120);
                if (ImGui.Combo("Access Level", ref acessLevelIndex, string.Join('\0', names)))
                {
                    if (AccessLevel.TryParse(names[acessLevelIndex], out AccessLevel newAccessLevel))
                    {
                        CEDClient.Send(new ModifyUserPacket(user.Username, "", newAccessLevel, user.Regions));
                    }
                }
                if (ImGui.Button("Change Password"))
                {
                    ImGui.OpenPopup("ChangePassword");
                    users_show_change_password = true;
                }
                if (ImGui.BeginPopupModal
                        ("ChangePassword", ref users_show_change_password, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.InputText("NewPassword", ref users_new_password, 32, ImGuiInputTextFlags.Password);
                    if (ImGui.Button("Add"))
                    {
                        CEDClient.Send
                            (new ModifyUserPacket(user.Username, users_new_password, user.AccessLevel, user.Regions));
                        ImGui.CloseCurrentPopup();
                        users_new_password = "";
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
                ImGui.Text("Regions:");
                ImGui.Indent();
                if (ImGui.BeginChild("Regions"))
                {
                    foreach (var region in CEDClient.Admin.Regions)
                    {
                        var hasRegion = user.Regions.Contains(region.Name);
                        if (ImGui.Checkbox(region.Name, ref hasRegion))
                        {
                            var newRegionList = hasRegion ?
                                user.Regions.Append(region.Name).ToList() :
                                user.Regions.Where(r => r != region.Name).ToList();
                            CEDClient.Send(new ModifyUserPacket(user.Username, "", user.AccessLevel, newRegionList));
                        }
                    }
                    ImGui.EndChild();
                }
            }
            ImGui.EndGroup();
            ImGui.EndTabItem();
        }
    }

    private int regions_selected_index = -1;
    private Region regions_selected;
    private string regions_new_region_name = "";
    private bool regions_show_add;
    private bool regions_show_remove;
    private int regions_area_selected = -1;
    private int regions_x1;
    private int regions_y1;
    private int regions_x2;
    private int regions_y2;

    private void DrawRegionsTab()
    {
        if (ImGui.BeginTabItem("Regions"))
        {
            if (ImGui.BeginChild("RegionList", new(150, 0), Borders | ResizeX))
            {
                for (var i = 0; i < CEDClient.Admin.Regions.Count; i++)
                {
                    var region = CEDClient.Admin.Regions[i];
                    if (ImGui.Selectable(region.Name, regions_selected_index == i))
                    {
                        regions_selected_index = i;
                        regions_selected = region;
                        regions_area_selected = -1;
                    }
                }
                ImGui.EndChild();
            }

            ImGui.SameLine();
            ImGui.BeginGroup();
            if (ImGui.Button("Refresh"))
            {
                CEDClient.Send(new ListRegionsPacket());
            }
            if (ImGui.Button("Add Region"))
            {
                ImGui.OpenPopup("AddRegion");
                regions_show_add = true;
            }
            if (ImGui.BeginPopupModal("AddRegion", ref regions_show_add, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("Name", ref regions_new_region_name, 32);
                if (ImGui.Button("Add"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_new_region_name, []));
                    ImGui.CloseCurrentPopup();
                    regions_selected_index = CEDClient.Admin.Regions.Count;
                    regions_new_region_name = "";
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (regions_selected_index == -1)
                ImGui.BeginDisabled();
            if (ImGui.Button("Remove Region"))
            {
                ImGui.OpenPopup("RemoveRegion");
                regions_show_remove = true;
            }
            if (ImGui.BeginPopupModal("RemoveRegion", ref regions_show_remove, ImGuiWindowFlags.AlwaysAutoResize))
            {
                var region = CEDClient.Admin.Regions[regions_selected_index];
                ImGui.Text($"Are you sure you want to remove region: {region.Name}");
                if (ImGui.Button("Yes"))
                {
                    CEDClient.Send(new DeleteRegionPacket(region.Name));
                    ImGui.CloseCurrentPopup();
                    regions_selected_index -= 1;
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (regions_selected_index == -1)
                ImGui.EndDisabled();

            ImGui.Separator();
            if(regions_selected_index != -1)
            {
                ImGui.Text("Areas:");
                ImGui.SameLine();
                if (ImGui.Button("Add Area"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas.Append(new Rect()).ToList()));
                }
                ImGui.SameLine();
                if (regions_area_selected == -1)
                    ImGui.BeginDisabled();
                if (ImGui.Button("Remove Area"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas.Where((_, i) => i != regions_area_selected).ToList()));
                    regions_area_selected -= 1;
                }
                if (regions_area_selected == -1)
                    ImGui.EndDisabled();
                var changedArea = false;
                if(regions_area_selected != -1)
                {
                    var curArea = regions_selected.Areas[regions_area_selected];
                    changedArea = regions_x1 != curArea.X1 || regions_y1 != curArea.Y1 || regions_x2 != curArea.X2 ||
                                      regions_y2 != curArea.Y2;
                }
                if (!changedArea)
                    ImGui.BeginDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Save Area"))
                {
                    var newArea = new Rect
                        ((ushort)regions_x1, (ushort)regions_y1, (ushort)regions_x2, (ushort)regions_y2);
                    regions_selected.Areas[regions_area_selected] = newArea;
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas));
                }
                if (!changedArea)
                    ImGui.EndDisabled();


                if (ImGui.BeginChild("AreaList", new(200, 0), Borders | ResizeX))
                {
                    if (regions_selected_index != -1 && regions_selected_index < CEDClient.Admin.Regions.Count)
                    {
                        var region = CEDClient.Admin.Regions[regions_selected_index];
                        for (var i = 0; i < region.Areas.Count; i++)
                        {
                            var area = region.Areas[i];
                            if (ImGui.Selectable($"{area}##{i}", regions_area_selected == i))
                            {
                                regions_area_selected = i;
                                regions_x1 = area.X1;
                                regions_y1 = area.Y1;
                                regions_x2 = area.X2;
                                regions_y2 = area.Y2;
                            }
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                ImGui.BeginGroup();
                if (regions_area_selected != -1)
                {
                    ImGui.NewLine();
                    UIManager.DragInt("X1", ref regions_x1, 1, 0, CEDClient.Width * 8);
                    ImGui.SameLine();
                    UIManager.DragInt("Y1", ref regions_y1, 1, 0, CEDClient.Height * 8);
                    UIManager.DragInt("X2", ref regions_x2, 1, 0, CEDClient.Width * 8);
                    ImGui.SameLine();
                    UIManager.DragInt("Y2", ref regions_y2, 1, 0, CEDClient.Height * 8);
                }
                ImGui.EndGroup();
            }
            ImGui.EndGroup();
            ImGui.EndTabItem();
        }
    }

    public void DrawArea(Vector2 currentPos)
    {
        if (!Show)
            return;
        if (regions_selected == default)
            return;

        for (var i = 0; i < regions_selected.Areas.Count; i++)
        {
            if (i == regions_area_selected)
                continue;

            var area = regions_selected.Areas[i];
            DrawRect(currentPos, area, UIManager.Green);
        }

        if (regions_area_selected != -1)
        {
            DrawRect(currentPos, new Rect((ushort)regions_x1, (ushort)regions_y1, (ushort)regions_x2, (ushort)regions_y2), UIManager.Pink);
        }
    }

    private void DrawRect(Vector2 currentPos, Rect area, Vector4 color)
    {
        ImGui.GetWindowDrawList().AddRect
        (
            currentPos + new Vector2(area.X1 / 8, area.Y1 / 8),
            currentPos + new Vector2(area.X2 / 8, area.Y2 / 8),
            ImGui.GetColorU32(color)
        );
    }
}