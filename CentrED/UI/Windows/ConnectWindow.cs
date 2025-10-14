using System.Net.Sockets;
using System.Numerics;
using CentrED.IO;
using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ConnectWindow : Window
{
    public override string Name => LangManager.Get(CONNECT_WINDOW) + "###Connect";

    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    private const int TextInputLength = 255;
    private int _profileIndex = ProfileManager.Profiles.IndexOf(ProfileManager.ActiveProfile);
    private string _hostname = ProfileManager.ActiveProfile.Hostname;
    private int _port = ProfileManager.ActiveProfile.Port;
    private string _username = ProfileManager.ActiveProfile.Username;
    private string _password = "";
    private string _clientPath = ProfileManager.ActiveProfile.ClientPath;
    private bool _showPassword;
    private bool _buttonDisabled;
    internal string Info = "Not Connected";
    internal Vector4 InfoColor = ImGuiColor.Red;
    private string _profileName = "";

    protected override void InternalDraw()
    {
        if (!Show)
            return;

        ImGui.Begin(Name, ref _show, ImGuiWindowFlags.NoResize);
        ImGui.SetWindowSize(Name, new Vector2(510, 250));
        if (ImGui.Combo(LangManager.Get(PROFILE), ref _profileIndex, ProfileManager.ProfileNames, ProfileManager.Profiles.Count))
        {
            var profile = ProfileManager.Profiles[_profileIndex];
            _profileName = profile.Name;
            _hostname = profile.Hostname;
            _port = profile.Port;
            _username = profile.Username;
            _password = "";
            _clientPath = profile.ClientPath;
            Config.Instance.ActiveProfile = profile.Name;
        }
        ImGui.SameLine();
        if (ImGui.Button(LangManager.Get(SAVE)))
        {
            ImGui.OpenPopup("SaveProfile");
        }

        if (ImGui.BeginPopup("SaveProfile"))
        {
            ImGui.InputText(LangManager.Get(NAME), ref _profileName, 128);
            if (ImGui.Button(LangManager.Get(SAVE)))
            {
                _profileIndex = ProfileManager.Save
                (
                    new Profile
                    {
                        Name = _profileName,
                        Hostname = _hostname,
                        Port = _port,
                        Username = _username,
                        ClientPath = _clientPath,
                    }
                );

                ImGui.CloseCurrentPopup();
            }
            if (ImGui.Button(LangManager.Get(CANCEL)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        ImGui.NewLine();
        ImGui.InputText(LangManager.Get(HOSTNAME), ref _hostname, TextInputLength);
        ImGui.InputInt(LangManager.Get(PORT), ref _port);
        ImGui.InputText(LangManager.Get(USERNAME), ref _username, TextInputLength);

        ImGui.InputText
        (
            LangManager.Get(PASSWORD),
            ref _password,
            TextInputLength,
            _showPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password
        );
        ImGui.SameLine();
        if (ImGui.Button(_showPassword ? LangManager.Get(HIDE) : LangManager.Get(SHOW)))
        {
            _showPassword = !_showPassword;
        }
        ImGui.InputText(LangManager.Get(UO_DIRECTORY), ref _clientPath, TextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            var defaultPath = _clientPath.Length == 0 ? Environment.CurrentDirectory : _clientPath;
            if (TinyFileDialogs.TrySelectFolder(LangManager.Get(SELECT_DIRECTORY), defaultPath, out var newPath))
            {
                _clientPath = newPath;
            }
        }
        ImGui.TextColored(InfoColor, Info);
        ImGui.BeginDisabled
        (
            _hostname.Length == 0 || _password.Length == 0 || _username.Length == 0 || _clientPath.Length == 0 || _buttonDisabled
        );
        if (CEDClient.Running)
        {
            if (ImGui.Button(LangManager.Get(DISCONNECT)))
            {
                CEDClient.Disconnect();
                CEDGame.MapManager.Reset();
            }
        }
        else
        {
            if (ImGui.Button(LangManager.Get(CONNECT)) || ImGui.IsWindowFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                CEDGame.MapManager.Reset();
                _buttonDisabled = true;
                new Task
                (
                    () =>
                    {
                        try
                        {
                            InfoColor = ImGuiColor.Blue;
                            Info = LangManager.Get(LOADING);
                            CEDGame.MapManager.Load(_clientPath);
                            Info = LangManager.Get(CONNECTING);
                            CEDClient.Connect(_hostname, _port, _username, _password);
                        }
                        catch (SocketException)
                        {
                            Info = LangManager.Get(UNABLE_TO_CONNECT);
                            InfoColor = ImGuiColor.Red;
                        }
                        catch (Exception e)
                        {
                            Info = string.Format(LangManager.Get(UNKNOWN_ERROR_1NAME), e.GetType().Name);
                            InfoColor = ImGuiColor.Red;
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            _buttonDisabled = false;
                        }
                    }
                ).Start();
            }
        }
        if (CEDClient.Status != "")
        {
            Info = CEDClient.Status;
            InfoColor = CEDClient.Running ? ImGuiColor.Green : ImGuiColor.Red;
        }
        ImGui.EndDisabled();
        ImGui.End();
    }
}