using System.Net.Sockets;
using System.Numerics;
using CentrED.IO;
using ClassicUO.Utility;
using ImGuiNET;

namespace CentrED.UI.Windows;

public class ConnectWindow : Window {
    public override string Name => "Connect";
    
    private const int TextInputLength = 255;
    private int _profileIndex = ProfileManager.Profiles.IndexOf(ProfileManager.ActiveProfile);
    private string _hostname = ProfileManager.ActiveProfile.Hostname;
    private int _port = ProfileManager.ActiveProfile.Port;
    private string _username = ProfileManager.ActiveProfile.Username;
    private string _password = "";
    private string _clientPath = ProfileManager.ActiveProfile.ClientPath;
    private string _clientVersion = ProfileManager.ActiveProfile.ClientVersion;
    private bool _showPassword;
    private bool _buttonDisabled;
    private Vector4 _infoColor = UIManager.Blue;
    private string _info = "";
    private string _profileName = "";

    public ConnectWindow(UIManager uiManager) : base(uiManager){ }

    public override void Draw() {
        if (!Show) return;
        
        ImGui.Begin(Id, ref _show,  ImGuiWindowFlags.NoResize);
        ImGui.SetWindowSize(Id, new Vector2(510, 250));
        // CenterWindow();
        if (ImGui.Combo("Profile", ref _profileIndex, ProfileManager.ProfileNames,
                ProfileManager.Profiles.Count)) {
            var profile = ProfileManager.Profiles[_profileIndex];
            _profileName = profile.Name;
            _hostname = profile.Hostname;
            _port = profile.Port;
            _username = profile.Username;
            _password = "";
            _clientPath = profile.ClientPath;
            _clientVersion = profile.ClientVersion;
            Config.ActiveProfile = profile.Name;
        }
        ImGui.SameLine();
        if (ImGui.Button("Save")) {
            ImGui.OpenPopup("SaveProfile");
        }

        if (ImGui.BeginPopup("SaveProfile")) {
            ImGui.InputText("Name", ref _profileName, 128);
            if (ImGui.Button("Save")) {
                _profileIndex = ProfileManager.Save(new Profile {
                    Name = _profileName, Hostname = _hostname, Port = _port,
                    Username = _username, ClientPath = _clientPath, ClientVersion = _clientVersion
                });
                
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        
        ImGui.Text("");

        ImGui.InputText("Host", ref _hostname, TextInputLength);
        ImGui.InputInt("Port", ref _port);
        ImGui.InputText("Username", ref _username, TextInputLength);
        
        ImGui.InputText("Password", ref _password, TextInputLength, _showPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password);
        ImGui.SameLine();
        if (ImGui.Button(_showPassword? "Hide" : "Show")) {
            _showPassword = !_showPassword;
        }
        ImGui.InputText("ClientPath", ref _clientPath, TextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("...")) {
            ImGui.OpenPopup("open-dir");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-dir", ref isOpen, ImGuiWindowFlags.NoTitleBar)) {
            var picker = FilePicker.GetFolderPicker(this, _clientPath.Length == 0 ? Environment.CurrentDirectory : _clientPath);
            if (picker.Draw()) {
                _clientPath = picker.SelectedFile;
                FilePicker.RemoveFilePicker(this);
            }

            ImGui.EndPopup();
        }
        ImGui.InputText("ClientVersion", ref _clientVersion, TextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("Discover")) {
            if (ClientVersionHelper.TryParseFromFile(Path.Join(_clientPath, "client.exe"), out _clientVersion)) {
                _info = "Version discovered!";
                _infoColor = UIManager.Green;
            }
            else {
                _info = "Unable to discover client version";
                _infoColor = UIManager.Red;
                _clientVersion = "";
            }
        }
        ImGui.TextColored(_infoColor, _info);
        ImGui.BeginDisabled(
            _hostname.Length == 0 || _password.Length == 0 || _username.Length == 0 || 
            _clientPath.Length == 0 || _clientVersion.Length == 0 || _buttonDisabled);
        if (_uiManager._mapManager.Client.Running) {
            if (ImGui.Button("Disconnect")) {
                _uiManager._mapManager.Client.Disconnect();
                _uiManager._mapManager.Reset();
                _info = "Disconnected";
            }
        }
        else {
            if (ImGui.Button("Connect")) {
                _uiManager._mapManager.Reset();
                _buttonDisabled = true;
                new Task(() => {
                        try {
                            _infoColor = UIManager.Blue;
                            _info = "Loading";
                            _uiManager._mapManager.Load(_clientPath, _clientVersion);
                            _info = "Connecting";
                            _uiManager._mapManager.Client.Connect(_hostname, _port, _username,
                                _password);
                            _info = _uiManager._mapManager.Client.Status;
                            _infoColor = _uiManager._mapManager.Client.Running ? UIManager.Blue : UIManager.Red;
                        }
                        catch (SocketException e) {
                            _info = "Unable to connect";
                            _infoColor = UIManager.Red;
                        }
                        finally {
                            _buttonDisabled = false;
                        }
                    }
                ).Start();
            }
        }
        ImGui.EndDisabled();
        ImGui.End();
    }
}