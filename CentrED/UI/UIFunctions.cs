using CentrED.Server;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI;

internal partial class UIManager {
    private float _mainMenuHeight; 
    
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
                ImGui.MenuItem("Hues", "", ref _huesShowWindow);
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

        _mainMenuHeight = ImGui.GetItemRectSize().Y;
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
    private bool _tilesLandVisible = true;
    private bool _tilesStaticVisible = true;
    private float _tilesTableWidth;
    private const int MaxLandIndex = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 _tilesDimensions = new(44, 44);


    private void FilterTiles() {
        if (_tilesFilter.Length == 0) {
            _matchedLandIds = new int[_validLandIds.Length];
            _validLandIds.CopyTo(_matchedLandIds, 0);
            
            _matchedStaticIds = new int[_validStaticIds.Length];
            _validStaticIds.CopyTo(_matchedStaticIds, 0);
        }
        else {
            var matchedLandIds = new List<int>();
            foreach (var index in _validLandIds) {
                var name = _tileDataLoader.LandData[index].Name;
                if(name.Contains(_tilesFilter) || $"{index}".Contains(_tilesFilter) || $"0x{index:X4}".Contains(_tilesFilter))
                    matchedLandIds.Add(index);
            }
            _matchedLandIds = matchedLandIds.ToArray();
            
            var matchedStaticIds = new List<int>();
            foreach (var index in _validStaticIds) {
                var name = _tileDataLoader.StaticData[index].Name;
                if(name.Contains(_tilesFilter) || $"{index}".Contains(_tilesFilter) || $"0x{index:X4}".Contains(_tilesFilter))
                    matchedStaticIds.Add(index);
            }
            _matchedStaticIds = matchedStaticIds.ToArray();
        }
    }
    
    private unsafe void DrawTilesWindow() {
        if (!_tilesShowWindow) return;
        ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(250, _graphicsDevice.PresentationParameters.BackBufferHeight - _mainMenuHeight), ImGuiCond.FirstUseEver);
        ImGui.Begin("Tiles", ref _tilesShowWindow);
        if (ImGui.Button("Scroll to selected")) {
            _tilesUpdateScroll = true;
        }
        
        ImGui.Text("Filter");
        if (ImGui.InputText("", ref _tilesFilter, 64)) {
            FilterTiles();
        }
        
        ImGui.Checkbox("Land", ref _tilesLandVisible);
        ImGui.SameLine();
        ImGui.Checkbox("Static", ref _tilesStaticVisible);

        ImGui.BeginChild("Tiles", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() + 10);
        }
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() - 10);
        }
        if (ImGui.BeginTable("TilesTable", 3)) {
            ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            ImGui.TableSetupColumn("Id" ,ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
            ImGui.TableSetupColumn("Graphic" ,ImGuiTableColumnFlags.WidthFixed, _tilesDimensions.X);
            _tilesTableWidth = ImGui.GetContentRegionAvail().X;
            if (_tilesLandVisible) {
                clipper.Begin(_matchedLandIds.Length, _tilesDimensions.Y);
                while (clipper.Step()) {
                    for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++){
                        TilesDrawLand(_matchedLandIds[row]);
                    }
                }
                clipper.End();
            }
            if(_tilesStaticVisible) {
                clipper.Begin(_matchedStaticIds.Length, _tilesDimensions.Y);
                while (clipper.Step()) {
                    for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++){
                        TilesDrawStatic(_matchedStaticIds[row]);
                    }
                }
                clipper.End();
            }
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void TilesDrawLand(int index) {
        var texture = _artLoader.GetLandTexture((uint)index, out var bounds);
        var name = _tileDataLoader.LandData[index].Name;
        TilesDrawRow(index, index, texture, bounds, name);
    }
    
    private void TilesDrawStatic(int index) {
        var realIndex = index + MaxLandIndex;
        var texture = _artLoader.GetStaticTexture((uint)index, out var bounds);
        var realBounds = _artLoader.GetRealArtBounds(index);
        var name = _tileDataLoader.StaticData[index].Name;
        TilesDrawRow(index, realIndex, texture, new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height), name);
    }
    
    private void TilesDrawRow(int index, int realIndex, Texture2D texture, Rectangle bounds, string name) {
        if (_tilesFilter.Length > 0  && 
            !(
                name.Contains(_tilesFilter) || 
                $"{index}".Contains(_tilesFilter) || 
                $"0x{index:X4}".Contains(_tilesFilter)
                )
            ) return;
        
        ImGui.TableNextRow(ImGuiTableRowFlags.None, _tilesDimensions.Y);
        
        if (_tilesUpdateScroll && _tilesSelectedId == realIndex) {
            ImGui.SetScrollHereY(0.45f);
            _tilesUpdateScroll = false;
        }

        if (ImGui.TableNextColumn()) {
            var startPos = ImGui.GetCursorPos();
            var selectableSize = new Vector2(_tilesTableWidth, _tilesDimensions.Y);
            if (ImGui.Selectable($"##tile{realIndex}", _tilesSelectedId == realIndex,
                    ImGuiSelectableFlags.SpanAllColumns, selectableSize))
                _tilesSelectedId = realIndex;

            ImGui.SetCursorPos(startPos with { Y = startPos.Y + (_tilesDimensions.Y - ImGui.GetFontSize()) / 2 });
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn()) {
            DrawImage(texture, bounds, _tilesDimensions);
        }

        if (ImGui.TableNextColumn()) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (_tilesDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(name);
        }
    }

    private bool _huesShowWindow;
    private bool _huesUpdateScroll;
    private string _huesFilter = "";
    private int _huesSelectedId;
    private const int _huesRowHeight = 20;
    
    private unsafe void DrawHuesWindow() {
        if (!_huesShowWindow) return;
        ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(250, _graphicsDevice.PresentationParameters.BackBufferHeight - _mainMenuHeight), ImGuiCond.FirstUseEver);
        ImGui.Begin("Hues", ref _huesShowWindow);
        if (ImGui.Button("Scroll to selected")) {
            _huesUpdateScroll = true;
        }

        ImGui.Text("Filter");
        ImGui.InputText("", ref _huesFilter, 64);
        
        ImGui.BeginChild("Hues", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.BeginTable("TilesTable", 2)) {
            ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            ImGui.TableSetupColumn("Id" ,ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
            _tilesTableWidth = ImGui.GetContentRegionAvail().X;
            clipper.Begin(HuesLoader.Instance.HuesCount, _huesRowHeight);
            while (clipper.Step()) {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++){
                    HuesDrawElement(i);
                }
            }
            clipper.End();
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void HuesDrawElement(int index) {
        // if (_tilesFilter.Length > 0  && 
        //     !(
        //         name.Contains(_tilesFilter) || 
        //         $"{index}".Contains(_tilesFilter) || 
        //         $"0x{index:X4}".Contains(_tilesFilter)
        //     )
        //    ) return;
        
        ImGui.TableNextRow(ImGuiTableRowFlags.None, _huesRowHeight);
        if (_huesUpdateScroll && _huesSelectedId == index) {
            ImGui.SetScrollHereY(0.45f);
            _huesUpdateScroll = false;
        }

        if (ImGui.TableNextColumn()) {
            var startPos = ImGui.GetCursorPos();

            var selectableSize = new Vector2(_tilesTableWidth, _huesRowHeight);
            if (ImGui.Selectable($"##hue{index}", _huesSelectedId == index,
                    ImGuiSelectableFlags.SpanAllColumns, selectableSize))
                _huesSelectedId = index;

            ImGui.SetCursorPos(startPos with { Y = startPos.Y + (_huesRowHeight - ImGui.GetFontSize()) / 2 });
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn()) {
            DrawImage(CentrEDGame._hueSampler, new Rectangle(0,index, 32, 1), new Vector2(ImGui.GetContentRegionAvail().X, _huesRowHeight));
        }
    }
}