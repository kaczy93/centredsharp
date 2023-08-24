using System.Numerics;
using CentrED.Server;
using ImGuiNET;

namespace CentrED.UI; 

internal partial class UIManager {

    private const int ConnectWindowTextInputLength = 255;
    private bool _connectShowWindow;
    private string _connectHostname = "127.0.0.1";
    private int _connectPort = 2597;
    private string _connectUsername = "admin";
    private string _connectPassword = "admin";
    
    private void DrawConnectWindow() {
        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ), 
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Connect", ref _connectShowWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.InputText("Host", ref _connectHostname, ConnectWindowTextInputLength);
        ImGui.InputInt("Port", ref _connectPort);
        ImGui.InputText("Username", ref _connectUsername, ConnectWindowTextInputLength);
        ImGui.InputText("Password", ref _connectPassword, ConnectWindowTextInputLength, ImGuiInputTextFlags.Password);
        ImGui.BeginDisabled(_connectHostname.Length == 0 || _connectPassword.Length == 0 || _connectUsername.Length == 0);
        if (ImGui.Button("Connect")) {
            _mapManager.Client.Connect(_connectHostname, _connectPort, _connectUsername,_connectPassword);
            _connectShowWindow = false;
        }
        ImGui.EndDisabled();
        ImGui.End();
    }

    private bool _localServerShowWindow;
    private string _localServerConfigPath = "Cedserver.xml";
    private bool _localServerAutoConnect = true;
    
    private void DrawLocalServerWindow() {
        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ), 
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Local Server", ref _localServerShowWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.InputText("Config File", ref _localServerConfigPath, 512);
        ImGui.Checkbox("Auto connect", ref _localServerAutoConnect);
        if (_localServerAutoConnect) {
            ImGui.InputText("Username", ref _connectUsername, ConnectWindowTextInputLength);
            ImGui.InputText("Password", ref _connectPassword, ConnectWindowTextInputLength, ImGuiInputTextFlags.Password);
        }

        if (CentrED.Server != null && CentrED.Server.Running) {
            if (ImGui.Button("Stop")) {
                CentrED.Client.Disconnect();
                CentrED.Server.Quit = true;
            }
        }
        else {
            if (ImGui.Button("Start")) {
                if (CentrED.Server != null) {
                    CentrED.Server.Dispose();
                }
                CentrED.Server = new CEDServer(new[] { _localServerConfigPath });
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
                    CentrED.Client.Connect("127.0.0.1", CentrED.Server.Config.Port, _connectUsername,_connectPassword);
            }
        }
        ImGui.End();
    }

    private bool _debugShowWindow;
    private int _debugTileX;
    private int _debugTileY;
    private bool _debugShowTestWindow;
    private void DrawDebugWindow() {
        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ), 
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Debug", ref _debugShowWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.Text($"Camera focus tile {_mapManager.Camera.LookAt / _mapManager.TILE_SIZE}");
        ImGui.Separator();
            
        ImGui.Checkbox("DrawLand", ref _mapManager.IsDrawLand);
        ImGui.Checkbox("DrawStatics", ref _mapManager.IsDrawStatic);
        ImGui.Checkbox("DrawShadows", ref _mapManager.IsDrawShadows);
        ImGui.SliderInt("Min Z render", ref _mapManager.MIN_Z, -127, 127);
        ImGui.SliderInt("Max Z render", ref _mapManager.MAX_Z, -127, 127);
        ImGui.SliderFloat("Zoom", ref _mapManager.Camera.Zoom, 0.2f, 10.0f);
        ImGui.Separator();
        ImGui.InputInt("Camera x", ref _debugTileX);
        ImGui.InputInt("Camera y", ref _debugTileY);
        if (ImGui.Button("Update pos")) {
            _mapManager.Camera.Position.X = _debugTileX * _mapManager.TILE_SIZE;
            _mapManager.Camera.Position.Y = _debugTileY * _mapManager.TILE_SIZE;
        }
        ImGui.Separator();
        if(ImGui.Button("Flush")) _mapManager.Client.Flush();
        if(ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
        if (ImGui.Button("Test Window")) _debugShowTestWindow = !_debugShowTestWindow;
        ImGui.End();
    }
}