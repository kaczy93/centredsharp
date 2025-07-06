using System.Net.Sockets;
using System.Numerics;
using CentrED.IO;
using CentrED.IO.Models;
using ClassicUO.Utility;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ConnectWindow : Window
{
    public override string Name => "Connect";

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
        if (ImGui.Combo("Profile", ref _profileIndex, ProfileManager.ProfileNames, ProfileManager.Profiles.Count))
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
        if (ImGui.Button("Save"))
        {
            ImGui.OpenPopup("SaveProfile");
        }

        if (ImGui.BeginPopup("SaveProfile"))
        {
            ImGui.InputText("Name", ref _profileName, 128);
            if (ImGui.Button("Save"))
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

            ImGui.EndPopup();
        }

        ImGui.Text(""); // What is it?

        ImGui.InputText("Host", ref _hostname, TextInputLength);
        ImGui.InputInt("Port", ref _port);
        ImGui.InputText("Username", ref _username, TextInputLength);

        ImGui.InputText
        (
            "Password",
            ref _password,
            TextInputLength,
            _showPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password
        );
        ImGui.SameLine();
        if (ImGui.Button(_showPassword ? "Hide" : "Show"))
        {
            _showPassword = !_showPassword;
        }
        ImGui.InputText("ClientPath", ref _clientPath, TextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            var defaultPath = _clientPath.Length == 0 ? Environment.CurrentDirectory : _clientPath;
            if (TinyFileDialogs.TrySelectFolder("Select Directory", defaultPath, out var newPath))
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
            if (ImGui.Button("Disconnect"))
            {
                CEDClient.Disconnect();
                CEDGame.MapManager.Reset();
            }
        }
        else
        {
            if (ImGui.Button("Connect") || ImGui.IsWindowFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
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
                            Info = "Loading";
                            CEDGame.MapManager.Load(_clientPath);
                            Info = "Connecting";
                            CEDClient.Connect(_hostname, _port, _username, _password);
                        }
                        catch (SocketException)
                        {
                            Info = "Unable to connect";
                            InfoColor = ImGuiColor.Red;
                        }
                        catch (Exception e)
                        {
                            Info = "Unknown error " + e.GetType().Name + ". Check console log";
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