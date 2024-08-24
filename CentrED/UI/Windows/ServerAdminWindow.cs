using CentrED.Client;
using ImGuiNET;
using static CentrED.Application;
using static ImGuiNET.ImGuiChildFlags;

namespace CentrED.UI.Windows;

public class ServerAdminWindow : Window
{
    public override bool Enabled => CEDClient.Initialized &&
                                    CEDClient.AccessLevel >= AccessLevel.Administrator;
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

    private static int users_selected = -1;
    private static string users_new_username = "";
    private static string users_new_password = "";
    private static bool users_show_add_user;
    private static bool users_show_remove_user;
    private static bool users_show_change_password;
    private void DrawUsersTab()
    {
        if (ImGui.BeginTabItem("Users"))
        {
            if (ImGui.BeginChild("UserList", new(150, 0), Border | ResizeX))
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
            if (ImGui.Button("Add"))
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
                    CEDClient.Send(new ModifyUserPacket(users_new_username, users_new_password, AccessLevel.None, new List<string>()));
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
            if(users_selected == -1)
                ImGui.BeginDisabled();
            if (ImGui.Button("Remove"))
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
            if(users_selected == -1)
                ImGui.EndDisabled();
            
            ImGui.Separator();
            if (users_selected != -1)
            {
                if (users_selected < CEDClient.Admin.Users.Count)
                {
                    var user = CEDClient.Admin.Users[users_selected];
                    var names = Enum.GetNames(typeof(AccessLevel));
                    var acessLevelIndex = Array.IndexOf(names, user.AccessLevel.ToString());
                    ImGui.PushItemWidth(120);
                    if (ImGui.Combo("Access Level", ref acessLevelIndex, string.Join('\0', names)))
                    {
                        if (AccessLevel.TryParse(names[acessLevelIndex], out AccessLevel newAccessLevel))
                        {
                            CEDClient.Send
                                (new ModifyUserPacket(user.Username, "", newAccessLevel, user.Regions));
                        }
                    }
                    if (ImGui.Button("Change Password"))
                    {
                        ImGui.OpenPopup("ChangePassword");
                        users_show_change_password = true;
                    }
                    if (ImGui.BeginPopupModal("ChangePassword", ref users_show_change_password, ImGuiWindowFlags.AlwaysAutoResize))
                    {
                        ImGui.InputText("NewPassword", ref users_new_password, 32, ImGuiInputTextFlags.Password);
                        if (ImGui.Button("Add"))
                        {
                            CEDClient.Send(new ModifyUserPacket(user.Username, users_new_password, user.AccessLevel, user.Regions));
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
                                var newRegionList = hasRegion ? user.Regions.Append(region.Name).ToList() : user.Regions.Where(r => r != region.Name).ToList();
                                CEDClient.Send(new ModifyUserPacket(user.Username, "", user.AccessLevel, newRegionList));
                            }
                        }
                        ImGui.EndChild();
                    }
                }
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawRegionsTab()
    {
        if (ImGui.BeginTabItem("Regions"))
        {
            ImGui.EndTabItem();    
        }
    }
}