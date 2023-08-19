using System.Numerics;
using CentrED.Server;
using ImGuiNET;

namespace CentrED.UI; 

internal partial class UIManager {

    private const int ConnectWindowTextInputLength = 255;
    private bool _showConnectWindow;
    private string _connectWindowHostname = "127.0.0.1";
    private int _connectWindowPort = 2597;
    private string _connectWindowUsername = "admin";
    private string _connectWindowPassword = "admin";

    
    private void DrawConnectWindow() {
        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ), 
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Connect", ref _showConnectWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.InputText("Host", ref _connectWindowHostname, ConnectWindowTextInputLength);
        ImGui.InputInt("Port", ref _connectWindowPort);
        ImGui.InputText("Username", ref _connectWindowUsername, ConnectWindowTextInputLength);
        ImGui.InputText("Password", ref _connectWindowPassword, ConnectWindowTextInputLength, ImGuiInputTextFlags.Password);
        ImGui.BeginDisabled(_connectWindowHostname.Length == 0 || _connectWindowPassword.Length == 0 || _connectWindowUsername.Length == 0);
        if (ImGui.Button("Connect")) {
            _mapManager.Client.Connect(_connectWindowHostname, _connectWindowPort, _connectWindowUsername,_connectWindowPassword);
            _showConnectWindow = false;
        }
        ImGui.EndDisabled();
        ImGui.End();
    }


    private bool _showLocalServerWindow;
    private string _localServerWindowConfig = "Cedserver.xml";
    private bool _localServerAutoConnect = true;
    
    private void DrawLocalServerWindow() {
        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ), 
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Local Server", ref _showLocalServerWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.InputText("Config", ref _localServerWindowConfig, 512);
        ImGui.Checkbox("Auto connect", ref _localServerAutoConnect);
        if (_localServerAutoConnect) {
            ImGui.InputText("Username", ref _connectWindowUsername, ConnectWindowTextInputLength);
            ImGui.InputText("Password", ref _connectWindowPassword, ConnectWindowTextInputLength, ImGuiInputTextFlags.Password);
        }

        if (CentrED.Server != null && CentrED.Server.Running) {
            if (ImGui.Button("Stop")) {
                CentrED.Server.Quit = true;
            }
        }
        else {
            if (ImGui.Button("Start")) {
                CentrED.Server = new CEDServer(new[] { _localServerWindowConfig });
                new Task(() => {
                    try {
                        CentrED.Server.Run();
                    }
                    catch (Exception e) {
                        Console.WriteLine("Server stopped");
                        Console.WriteLine(e);
                    }
                }).Start();
                while (CentrED.Server is not { Running: true }) {
                    Thread.Sleep(1);
                }
                if(_localServerAutoConnect)
                    _mapManager.Client.Connect("127.0.0.1", CentrED.Server.Config.Port, _connectWindowUsername,_connectWindowPassword);
            }
        }
        ImGui.End();
    }
}