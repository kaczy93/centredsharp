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
                ImGui.MenuItem("Toolbox", "", ref _toolboxShowWindow);
                ImGui.MenuItem("Tiles", "", ref _tilesShowWindow);
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

    private void DrawToolboxWindow() {
        if (!_toolboxShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Toolbox", ref _toolboxShowWindow);
        ToolButton(_infoTool);
        ToolButton(_hueTool);
        ToolButton(_elevateTool);
        if (_mapManager.ActiveTool != null) {
            _mapManager.ActiveTool.DrawWindow();
        }

        ImGui.End();
    }

    private bool _tilesShowWindow;
    private string _tilesFilter = "";
    private int _tilesSelectedId = -1;
    private bool _tilesUpdateScroll;
    private bool _tilesStaticTabSelected;
    private float _tilesTableWidth;
    private const int MaxLandIndex = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;

    private void DrawTilesWindow() {
        if (!_tilesShowWindow) return;
        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Tiles", ref _tilesShowWindow);
        if (ImGui.Button("Scroll to selected")) {
            ImGui.SetTabItemClosed("Tiles");
            if (_tilesSelectedId > ArtLoader.MAX_LAND_DATA_INDEX_COUNT)
                _tilesStaticTabSelected = true;
            _tilesUpdateScroll = true;
        }

        ImGui.Text("Filter");
        ImGui.InputText("", ref _tilesFilter, 64);
        if (ImGui.BeginTabBar("TilesTabBar", ImGuiTabBarFlags.AutoSelectNewTabs)) {
            if (ImGui.BeginTabItem("Land")) {
                _tilesStaticTabSelected = false;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Static")) {
                _tilesStaticTabSelected = true;
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.BeginChild("Tiles", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() + 10);
        }
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() - 10);
        }
        if (ImGui.BeginTable("TilesTable", 3)) {
            _tilesTableWidth = ImGui.GetContentRegionAvail().X;
            if (!_tilesStaticTabSelected) {
                for (int i = 0; i < TileDataLoader.Instance.LandData.Length; i++) {
                    TilesDrawLand(i);
                }
            }
            else {
                for (int i = 0; i < TileDataLoader.Instance.StaticData.Length; i++) {
                    TilesDrawStatic(i);
                }
            }
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void TilesDrawLand(int index) {
        if (ArtLoader.Instance.GetValidRefEntry(index).Equals(UOFileIndex.Invalid)) return;
        var texture = ArtLoader.Instance.GetLandTexture((uint)index, out var bounds);
        var name = TileDataLoader.Instance.LandData[index].Name;
        TilesDrawRow(index, index, texture, ref bounds, name);
    }
    
    private void TilesDrawStatic(int index) {
        var realIndex = index + MaxLandIndex;
        if (ArtLoader.Instance.GetValidRefEntry(realIndex).Equals(UOFileIndex.Invalid)) return;
        var texture = ArtLoader.Instance.GetStaticTexture((uint)index, out var bounds);
        var name = TileDataLoader.Instance.StaticData[index].Name;
        TilesDrawRow(index, realIndex, texture, ref bounds, name);
    }
    
    private void TilesDrawRow(int index, int realIndex, Texture2D texture, ref Rectangle bounds, string name) {
        if (_tilesFilter.Length > 0 && !name.Contains(_tilesFilter)) return;
        
        ImGui.TableNextRow();
        if (_tilesUpdateScroll && _tilesSelectedId == realIndex) {
            ImGui.SetScrollHereY(0f);
            _tilesUpdateScroll = false;
        }

        ImGui.TableNextColumn();
        var startPos = ImGui.GetCursorPos();
        var screenPos = ImGui.GetCursorScreenPos();
        var shouldDraw = ImGui.IsRectVisible(screenPos, new Vector2(screenPos.X + 1, screenPos.Y + 1));

        if(shouldDraw){
            if (ImGui.Selectable($"##tile{realIndex}", _tilesSelectedId == realIndex,
                    ImGuiSelectableFlags.SpanAllColumns, new Vector2(_tilesTableWidth, bounds.Height))) 
                _tilesSelectedId = realIndex;
            

            ImGui.SetCursorPos(startPos with {
                    Y = startPos.Y + (bounds.Height - ImGui.GetFontSize()) / 2
                });
            ImGui.Text($"0X{index:X4}");
        }
        ImGui.TableNextColumn();
        if(shouldDraw)
            DrawImage(texture, bounds);
        
        ImGui.TableNextColumn();
        if (shouldDraw) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (bounds.Height - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(name);
        }
    }
}