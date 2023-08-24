using CentrED.Server;
using ClassicUO.Assets;
using ClassicUO.IO;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI;

internal partial class UIManager {

    private void DrawMainMenu() {
        if (ImGui.BeginMainMenuBar()) {
            if (ImGui.BeginMenu("CentrED")) {
                if (ImGui.MenuItem("Connect", !_mapManager.Client.Running)) _connectShowWindow = true;
                if (ImGui.MenuItem("Local Server")) _localServerShowWindow = true;
                if (ImGui.MenuItem("Disconnect", _mapManager.Client.Running)) _mapManager.Client.Disconnect();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools")) {
                ImGui.MenuItem("TileSelection", "", ref _tileSelectionShowWindow);
                ImGui.MenuItem("ToolBox", "", ref _toolboxShowWindow);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help")) {
                //Credits
                //About
                ImGui.Separator();
                if (ImGui.MenuItem("DebugWindow")) _debugShowWindow = !_debugShowWindow;
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private const int ConnectWindowTextInputLength = 255;
    private bool _connectShowWindow;
    private string _connectHostname = "127.0.0.1";
    private int _connectPort = 2597;
    private string _connectUsername = "admin";
    private string _connectPassword = "admin";

    private void DrawConnectWindow() {
        if (!_connectShowWindow) return;

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
        ImGui.BeginDisabled(
            _connectHostname.Length == 0 || _connectPassword.Length == 0 || _connectUsername.Length == 0);
        if (ImGui.Button("Connect")) {
            _mapManager.Client.Connect(_connectHostname, _connectPort, _connectUsername, _connectPassword);
            _connectShowWindow = false;
        }

        ImGui.EndDisabled();
        ImGui.End();
    }

    private bool _localServerShowWindow;
    private string _localServerConfigPath = "Cedserver.xml";
    private bool _localServerAutoConnect = true;

    private void DrawLocalServerWindow() {
        if (!_localServerShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2,
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2
            ),
            ImGuiCond.FirstUseEver);
        ImGui.Begin("Local Server", ref _localServerShowWindow,
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.InputText("Config File", ref _localServerConfigPath, 512);
        ImGui.Checkbox("Auto connect", ref _localServerAutoConnect);
        if (_localServerAutoConnect) {
            ImGui.InputText("Username", ref _connectUsername, ConnectWindowTextInputLength);
            ImGui.InputText("Password", ref _connectPassword, ConnectWindowTextInputLength,
                ImGuiInputTextFlags.Password);
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

                if (_localServerAutoConnect)
                    CentrED.Client.Connect("127.0.0.1", CentrED.Server.Config.Port, _connectUsername, _connectPassword);
            }
        }

        ImGui.End();
    }

    private bool _debugShowWindow;
    private int _debugTileX;
    private int _debugTileY;
    private bool _debugShowTestWindow;

    private void DrawDebugWindow() {
        if (!_debugShowWindow) return;

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
        if (ImGui.Button("Flush")) _mapManager.Client.Flush();
        if (ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
        if (ImGui.Button("Test Window")) _debugShowTestWindow = !_debugShowTestWindow;
        ImGui.End();
    }

    private bool _toolboxShowWindow;

    private void DrawToolBox() {
        if (!_toolboxShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("ToolBox", ref _toolboxShowWindow);
        ToolButton(_infoTool);
        ToolButton(_hueTool);
        ToolButton(_elevateTool);
        if (_mapManager.ActiveTool != null) {
            _mapManager.ActiveTool.DrawWindow();
        }

        ImGui.End();
    }

    private bool _tileSelectionShowWindow;
    private string _tileSelectionFilter = "";
    private bool _tileSelectionLandTab;
    private bool _tileSelectionStaticTab;
    private int _tileSelectionLand = -1;
    private int _tileSelectionStatic = -1;
    private bool _tileSelectionUpdateScroll;

    private void DrawTileSelection() {
        if (!_tileSelectionShowWindow) return;
        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("TileSelection", ref _tileSelectionShowWindow);
        if (ImGui.Button("Scroll to selected")) {
            _tileSelectionUpdateScroll = true;
        }
        ImGui.Text("Filter");
        ImGui.InputText("", ref _tileSelectionFilter, 64);
        if (ImGui.BeginTabBar("SelectionTabBar")) {
            if (ImGui.BeginTabItem("Land")) {
                _tileSelectionLandTab = true;
                _tileSelectionStaticTab = false;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Static")) {
                _tileSelectionLandTab = false;
                _tileSelectionStaticTab = true;
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.BeginChild("Tiles");
        if (ImGui.BeginTable("TilesTable", 3)) {
            var tableWidth = ImGui.GetContentRegionAvail().X;
            if (_tileSelectionLandTab) {
                for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++) {
                    if (ArtLoader.Instance.GetValidRefEntry(i).Equals(UOFileIndex.Invalid)) continue;
                    var tileData = TileDataLoader.Instance.LandData[i];
                    if(_tileSelectionFilter.Length > 0 && (!tileData.Name.Contains(_tileSelectionFilter))) continue;
                    var tex = ArtLoader.Instance.GetLandTexture((uint)i, out var bounds);
                    TileSelectionDraw(i, tex, bounds, tileData.Name, tableWidth, ref _tileSelectionLand);
                }
            }

            if (_tileSelectionStaticTab) {
                for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++) {
                    if (ArtLoader.Instance.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals(UOFileIndex.Invalid)) continue;
                    var tileData = TileDataLoader.Instance.StaticData[i];
                    if(_tileSelectionFilter.Length > 0 && (!tileData.Name.Contains(_tileSelectionFilter))) continue;
                    var tex = ArtLoader.Instance.GetStaticTexture((uint)i, out var bounds);
                    TileSelectionDraw(i, tex, bounds, tileData.Name, tableWidth, ref _tileSelectionStatic);
                }
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void TileSelectionDraw(int i, Texture2D texture, Rectangle bounds, string name, float tableWidth, ref int selectedIndex) {
        if (_tileSelectionUpdateScroll && selectedIndex == i) {
            ImGui.SetScrollHereY(0.5f);
            _tileSelectionUpdateScroll = false;
        }
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var startPos = ImGui.GetCursorPos();

        if (ImGui.Selectable($"##{i:X4}", selectedIndex == i,
                ImGuiSelectableFlags.SpanAllColumns, new Vector2(tableWidth, bounds.Height))) {
            selectedIndex = i;
        }

        ImGui.SetCursorPos(startPos with {
            Y = startPos.Y + (bounds.Height - ImGui.GetFontSize()) / 2
        });
        ImGui.Text($"0X{i:X4}");

        ImGui.TableNextColumn();
        DrawImage(texture, bounds);

        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (bounds.Height - ImGui.GetFontSize()) / 2);
        if(name is { Length: > 0 })
            ImGui.TextUnformatted(name);
    }
}