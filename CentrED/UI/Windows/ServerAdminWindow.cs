using System.Numerics;
using CentrED.Client;
using CentrED.Network;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static Hexa.NET.ImGui.ImGuiChildFlags;

namespace CentrED.UI.Windows;

public class ServerAdminWindow : Window
{
    public override bool Enabled => CEDClient.Running && CEDClient.AccessLevel >= AccessLevel.Administrator;
    public override string Name => "Server Administration";

    public override void OnShow()
    {
        if (CEDClient.Running)
        {
            CEDClient.Send(new ListUsersPacket());
            CEDClient.Send(new ListRegionsPacket());
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(LangEntry.NotConnected));
            return;
        }
        if (ImGui.Button("Server Save"u8))
        {
            CEDClient.Flush();
        }
        ImGui.SameLine();
        if (ImGuiEx.ConfirmButton("Stop Server", "Are you sure you want to stop the server?"))
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
        }
        ImGui.EndChild();
    }

    private int users_selected_index = -1;
    private User users_selected => 
        users_selected_index == -1 || users_selected_index >= CEDClient.Admin.Users.Count ? default : CEDClient.Admin.Users[users_selected_index];
    private string users_new_username = "";
    private string users_new_password = "";

    private void DrawUsersTab()
    {
        if (ImGui.BeginTabItem("Users"))
        {
            if (ImGui.BeginChild("UserList", new(150, 0), Borders | ResizeX))
            {
                for (var i = 0; i < CEDClient.Admin.Users.Count; i++)
                {
                    var user = CEDClient.Admin.Users[i];
                    if (ImGui.Selectable(user.Username, users_selected_index == i))
                    {
                        users_selected_index = i;
                    }
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginGroup();
            if (ImGui.Button("Refresh"))
            {
                CEDClient.Send(new ListUsersPacket());
            }
            if (ImGui.Button("Add User"))
            {
                ImGui.OpenPopup("AddUser");
            }
            if (ImGui.BeginPopupModal("AddUser", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("Username", ref users_new_username, 32);
                ImGui.InputText("Password", ref users_new_password, 32, ImGuiInputTextFlags.Password);
                
                ImGui.BeginDisabled(string.IsNullOrEmpty(users_new_username) || CEDClient.Admin.Users.Any(u => u.Username == users_new_username) || string.IsNullOrEmpty(users_new_password));
                if (ImGui.Button("Add"))
                {
                    CEDClient.Send(new ModifyUserPacket(users_new_username, users_new_password, AccessLevel.None, []));
                    ImGui.CloseCurrentPopup();
                    users_selected_index = CEDClient.Admin.Users.Count;
                    users_new_username = "";
                    users_new_password = "";
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.BeginDisabled(users_selected_index == -1);
            if (ImGuiEx.ConfirmButton
                    ("Delete User", $"Are you sure you want to delete user: {users_selected.Username}"))
            {
                CEDClient.Send(new DeleteUserPacket(users_selected.Username));
                users_selected_index -= 1;
            }
            ImGui.EndDisabled();

            ImGui.Separator();
            if (users_selected_index != -1 && users_selected_index < CEDClient.Admin.Users.Count)
            {
                var user = CEDClient.Admin.Users[users_selected_index];
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
                }
                if (ImGui.BeginPopupModal
                        ("ChangePassword", ImGuiWindowFlags.AlwaysAutoResize))
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
                ImGui.Text("Regions:"u8);
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
                }
                ImGui.EndChild();
            }
            ImGui.EndGroup();
            ImGui.EndTabItem();
        }
    }

    private int regions_selected_index = -1;
    private Region regions_selected => 
        regions_selected_index == -1 || regions_selected_index >= CEDClient.Admin.Regions.Count ? default : CEDClient.Admin.Regions[regions_selected_index];
    private string regions_new_region_name = "";
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
                        regions_area_selected = -1;
                    }
                }   
            }
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginGroup();
            if (ImGui.Button("Refresh"))
            {
                CEDClient.Send(new ListRegionsPacket());
            }
            if (ImGui.Button("Add Region"))
            {
                ImGui.OpenPopup("AddRegion");
            }
            if (ImGui.BeginPopupModal("AddRegion", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputText("Name", ref regions_new_region_name, 32);
                ImGui.BeginDisabled(string.IsNullOrEmpty(regions_new_region_name) || CEDClient.Admin.Regions.Any(r => r.Name == regions_new_region_name));
                if (ImGui.Button("Add"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_new_region_name, []));
                    ImGui.CloseCurrentPopup();
                    regions_selected_index = CEDClient.Admin.Regions.Count;
                    regions_new_region_name = "";
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.BeginDisabled(regions_selected_index == -1);
            if (ImGuiEx.ConfirmButton
                    ("Delete Region", $"Are you sure you want to delete region: {regions_selected.Name}"))
            {
                CEDClient.Send(new DeleteRegionPacket(regions_selected.Name));
                regions_selected_index -= 1;
            }
            ImGui.EndDisabled();

            ImGui.Separator();
            if(regions_selected_index != -1)
            {
                ImGui.Text("Areas:"u8);
                ImGui.SameLine();
                if (ImGui.Button("Add Area"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas.Append(new RectU16()).ToList()));
                }
                ImGui.SameLine();
                ImGui.BeginDisabled(regions_area_selected == -1);
                if (ImGui.Button("Remove Area"))
                {
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas.Where((_, i) => i != regions_area_selected).ToList()));
                    regions_area_selected -= 1;
                }
                ImGui.EndDisabled();
                var changedArea = false;
                if(regions_area_selected != -1)
                {
                    var curArea = regions_selected.Areas[regions_area_selected];
                    changedArea = regions_x1 != curArea.X1 || regions_y1 != curArea.Y1 || regions_x2 != curArea.X2 ||
                                      regions_y2 != curArea.Y2;
                }
                ImGui.BeginDisabled(!changedArea);
                ImGui.SameLine();
                if (ImGui.Button("Save Area"))
                {
                    var newArea = new RectU16
                        ((ushort)regions_x1, (ushort)regions_y1, (ushort)regions_x2, (ushort)regions_y2);
                    regions_selected.Areas[regions_area_selected] = newArea;
                    CEDClient.Send(new ModifyRegionPacket(regions_selected.Name, regions_selected.Areas));
                }
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
                }
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.BeginGroup();
                if (regions_area_selected != -1)
                {
                    ImGui.NewLine();
                    ImGuiEx.DragInt("X1", ref regions_x1, 1, 0, CEDClient.Width * 8);
                    ImGui.SameLine();
                    ImGuiEx.DragInt("Y1", ref regions_y1, 1, 0, CEDClient.Height * 8);
                    ImGuiEx.DragInt("X2", ref regions_x2, 1, 0, CEDClient.Width * 8);
                    ImGui.SameLine();
                    ImGuiEx.DragInt("Y2", ref regions_y2, 1, 0, CEDClient.Height * 8);
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
            DrawRect(currentPos, area, ImGuiColor.Green);
        }

        if (regions_area_selected != -1)
        {
            DrawRect(currentPos, new RectU16((ushort)regions_x1, (ushort)regions_y1, (ushort)regions_x2, (ushort)regions_y2), ImGuiColor.Pink);
        }
    }

    private void DrawRect(Vector2 currentPos, RectU16 area, Vector4 color)
    {
        ImGui.GetWindowDrawList().AddRect
        (
            currentPos + new Vector2(area.X1 / 8, area.Y1 / 8),
            currentPos + new Vector2(area.X2 / 8, area.Y2 / 8),
            ImGui.GetColorU32(color)
        );
    }
}