using System.Net.Sockets;
using System.Text;
using CentrED.Client;
using CentrED.Map;
using CentrED.Server;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadarMap = CentrED.Map.RadarMap;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI;

internal partial class UIManager {
    private static Vector4 Red = new (1, 0, 0, 1);
    private static Vector4 Green = new (0, 1, 0, 1);
    private static Vector4 Blue = new (0, 0, 1, 1);
    
    private void CenterWindow() {
        ImGui.SetWindowPos( 
            new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2 - ImGui.GetWindowSize().X / 2,
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2 - ImGui.GetWindowSize().Y / 2)
            , ImGuiCond.FirstUseEver
        );
    }
    
    private float _mainMenuHeight; 
    
    private void DrawMainMenu() {
        if (ImGui.BeginMainMenuBar()) {
            if (ImGui.BeginMenu("CentrED")) {
                if (ImGui.MenuItem("Connect")) _connectShowWindow = true;
                if (ImGui.MenuItem("Local Server")) _localServerShowWindow = true;
                ImGui.Separator();
                if (ImGui.MenuItem("Options")) _optionsShowWindow = true;
                ImGui.Separator();
                if (ImGui.MenuItem("Quit")) _game.Exit();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools")) {
                ImGui.MenuItem("Info", "", ref InfoShowWindow);
                ImGui.MenuItem("Toolbox", "", ref _toolboxShowWindow);
                ImGui.MenuItem("Tiles", "", ref _tilesShowWindow);
                ImGui.MenuItem("Hues", "", ref HuesShowWindow);
                ImGui.MenuItem("Minimap", "", ref _minimapShowWindow);
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
    private int _connectProfileIndex = ProfileManager.Profiles.IndexOf(ProfileManager.ActiveProfile);
    private string _connectHostname = ProfileManager.ActiveProfile.Hostname;
    private int _connectPort = ProfileManager.ActiveProfile.Port;
    private string _connectUsername = ProfileManager.ActiveProfile.Username;
    private string _connectPassword = "";
    private string _connectClientPath = ProfileManager.ActiveProfile.ClientPath;
    private string _connectClientVersion = ProfileManager.ActiveProfile.ClientVersion;
    private bool _connectShowPassword;
    private bool _connectButtonDisabled;
    private Vector4 _connectInfoColor = Blue;
    private string _connectInfo = "";

    private string _ProfileSaveName = "";

    private void DrawConnectWindow() {
        if (!_connectShowWindow) return;
        
        ImGui.Begin("Connect", ref _connectShowWindow,  ImGuiWindowFlags.NoResize);
        ImGui.SetWindowSize("Connect", new Vector2(510, 250));
        CenterWindow();
        if (ImGui.Combo("Profile", ref _connectProfileIndex, ProfileManager.ProfileNames,
                ProfileManager.Profiles.Count)) {
            var profile = ProfileManager.Profiles[_connectProfileIndex];
            _ProfileSaveName = profile.Name;
            _connectHostname = profile.Hostname;
            _connectPort = profile.Port;
            _connectUsername = profile.Username;
            _connectPassword = "";
            _connectClientPath = profile.ClientPath;
            _connectClientVersion = profile.ClientVersion;
            Config.ActiveProfile = profile.Name;
        }
        ImGui.SameLine();
        if (ImGui.Button("Save")) {
            ImGui.OpenPopup("SaveProfile");
        }

        if (ImGui.BeginPopup("SaveProfile")) {
            ImGui.InputText("Name", ref _ProfileSaveName, 128);
            if (ImGui.Button("Save")) {
                _connectProfileIndex = ProfileManager.Save(new Profile {
                    Name = _ProfileSaveName, Hostname = _connectHostname, Port = _connectPort,
                    Username = _connectUsername, ClientPath = _connectClientPath, ClientVersion = _connectClientVersion
                });
                
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        
        ImGui.Text("");

        ImGui.InputText("Host", ref _connectHostname, ConnectWindowTextInputLength);
        ImGui.InputInt("Port", ref _connectPort);
        ImGui.InputText("Username", ref _connectUsername, ConnectWindowTextInputLength);
        
        ImGui.InputText("Password", ref _connectPassword, ConnectWindowTextInputLength, _connectShowPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password);
        ImGui.SameLine();
        if (ImGui.Button(_connectShowPassword? "Hide" : "Show")) {
            _connectShowPassword = !_connectShowPassword;
        }
        ImGui.InputText("ClientPath", ref _connectClientPath, ConnectWindowTextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("...")) {
            ImGui.OpenPopup("open-dir");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-dir", ref isOpen, ImGuiWindowFlags.NoTitleBar)) {
            var picker = FilePicker.GetFolderPicker(this, _connectClientPath.Length == 0 ? Environment.CurrentDirectory : _connectClientPath);
            if (picker.Draw()) {
                _connectClientPath = picker.SelectedFile;
                FilePicker.RemoveFilePicker(this);
            }

            ImGui.EndPopup();
        }
        ImGui.InputText("ClientVersion", ref _connectClientVersion, ConnectWindowTextInputLength);
        ImGui.SameLine();
        if (ImGui.Button("Discover")) {
            if (ClientVersionHelper.TryParseFromFile(Path.Join(_connectClientPath, "client.exe"), out _connectClientVersion)) {
                _connectInfo = "Version discovered!";
                _connectInfoColor = Green;
            }
            else {
                _connectInfo = "Unable to discover client version";
                _connectInfoColor = Red;
                _connectClientVersion = "";
            }
        }
        ImGui.TextColored(_connectInfoColor, _connectInfo);
        ImGui.BeginDisabled(
            _connectHostname.Length == 0 || _connectPassword.Length == 0 || _connectUsername.Length == 0 || 
            _connectClientPath.Length == 0 || _connectClientVersion.Length == 0 || _connectButtonDisabled);
        if (_mapManager.Client.Running) {
            if (ImGui.Button("Disconnect")) {
                _mapManager.Client.Disconnect();
                _mapManager.Reset();
                _connectInfo = "Disconnected";
            }
        }
        else {
            if (ImGui.Button("Connect")) {
                _mapManager.Reset();
                _connectButtonDisabled = true;
                new Task(() => {
                        try {
                            _connectInfoColor = Blue;
                            _connectInfo = "Loading";
                            _mapManager.Load(_connectClientPath, _connectClientVersion);
                            _connectInfo = "Connecting";
                            _mapManager.Client.Connect(_connectHostname, _connectPort, _connectUsername,
                                _connectPassword);
                            OnConnect();
                            _connectInfo = _mapManager.Client.Status;
                            _connectInfoColor = _mapManager.Client.Running ? Blue : Red;
                        }
                        catch (SocketException e) {
                            _connectInfo = "Unable to connect";
                            _connectInfoColor = Red;
                        }
                        finally {
                            _connectButtonDisabled = false;
                        }
                    }
                ).Start();
            }
        }
        ImGui.EndDisabled();
        ImGui.End();
    }

    private bool _localServerShowWindow;
    private string _localServerConfigPath = Config.ServerConfigPath;
    private StreamReader? _localServerLogReader;
    private StringBuilder _localServerLog = new();

    private void DrawLocalServerWindow() {
        if (!_localServerShowWindow) return;

        ImGui.Begin("Local Server", ref _localServerShowWindow );
        ImGui.InputText("Config File", ref _localServerConfigPath, 512);
        ImGui.SameLine();
        if (ImGui.Button("...")) {
            ImGui.OpenPopup("open-file");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar)) {
            var picker = FilePicker.GetFilePicker(this, Environment.CurrentDirectory, ".xml");
            if (picker.Draw()) {
                _localServerConfigPath = picker.SelectedFile;
                Config.ServerConfigPath = _localServerConfigPath;
                FilePicker.RemoveFilePicker(this);
            }
            ImGui.EndPopup();
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

                _localServerLog.Clear();
                Config.ServerConfigPath = _localServerConfigPath;
                CentrED.Server = new CEDServer(new[] { _localServerConfigPath }, new StreamWriter(File.Open("cedserver.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)){AutoFlush = true});
                new Task(() => {
                    try {
                        CentrED.Server.Run();
                    }
                    catch (Exception e) {
                        Console.WriteLine("Server stopped");
                        Console.WriteLine(e);
                    }
                }).Start();
                _localServerLogReader = new StreamReader(File.Open("cedserver.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            }
        }
        
        ImGui.Separator();
        ImGui.Text("Server Log:");
        ImGui.BeginChild("ServerLogRegion");
        if (_localServerLogReader != null) {
            do {
                var line = _localServerLogReader.ReadLine();
                if (line == null) break;
                _localServerLog.AppendLine(line);
            } while (true);
        }
        ImGui.TextUnformatted(_localServerLog.ToString());
        ImGui.EndChild();

        ImGui.End();
    }

    private bool _optionsShowWindow;
    private void DrawOptionsWindow() {
        if (!_optionsShowWindow) return;
        
        ImGui.Begin("Options", ref _optionsShowWindow, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        CenterWindow();
        ImGui.Text("Nothing to see here (yet) :)");
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
        ImGui.Text($"FPS: {_framesPerSecond:F1}");
        ImGui.Text(
            $"Resolution: {_graphicsDevice.PresentationParameters.BackBufferWidth}x{_graphicsDevice.PresentationParameters.BackBufferHeight}");
        ImGui.Text($"Land tiles: {_mapManager.LandTiles.Count}");
        ImGui.Text($"Static tiles: {_mapManager.StaticTiles.Count}");
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
            _mapManager.SetPos((ushort)_debugTileX, (ushort)_debugTileY);
        }

        ImGui.Separator();
        if (ImGui.Button("Server Flush")) _mapManager.Client.Flush();
        if (ImGui.Button("Clear cache")) _mapManager.Reset();
        // if (ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
        if (ImGui.Button("Test Window")) _debugShowTestWindow = !_debugShowTestWindow;
        ImGui.End();
    }

    public bool InfoShowWindow;
    public MapObject? InfoSelectedTile;

    private void DrawInfoWindow() {
        if (!InfoShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Info", ref InfoShowWindow);
        if (InfoSelectedTile is LandObject lo) {
            var land = lo.Tile;
            ImGui.Text("Land");
            var texture = ArtLoader.Instance.GetLandTexture(land.Id, out var bounds);
            DrawImage(texture, bounds);
            ImGui.Text($"x:{land.X} y:{land.Y} z:{land.Z}");
            ImGui.Text($"id: 0x{land.Id:X4} ({land.Id})");
        }
        else if (InfoSelectedTile is StaticObject so) {
            var staticTile = so.StaticTile;
            ImGui.Text("Static");
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
            DrawImage(texture, new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height));
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"hue: 0x{staticTile.Hue - 1:X4} ({staticTile.Hue - 1})");
        }
        ImGui.End();
    }

    private bool _toolboxShowWindow;

    private void DrawToolboxWindow() {
        if (!_toolboxShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Toolbox", ref _toolboxShowWindow);
        ToolButton(_selectTool);
        ToolButton(_drawTool);
        ToolButton(_removeTool);
        ToolButton(_moveTool);
        ToolButton(_elevateTool);
        ToolButton(_hueTool);
        ImGui.End();
    }

    private bool _tilesShowWindow;
    private string _tilesFilter = "";
    private int _tilesSelectedId;
    public int TilesSelectedId => _tilesSelectedId;
    private bool _tilesUpdateScroll;
    private bool _tilesLandVisible = true;
    private bool _tilesStaticVisible = true;
    private float _tilesTableWidth;
    public const int MaxLandIndex = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 _tilesDimensions = new(44, 44);

    public bool IsLandTile(int id) => id < MaxLandIndex;


    private void FilterTiles() {
        if (_tilesFilter.Length == 0) {
            _matchedLandIds = new int[_mapManager.ValidLandIds.Length];
            _mapManager.ValidLandIds.CopyTo(_matchedLandIds, 0);
            
            _matchedStaticIds = new int[_mapManager.ValidStaticIds.Length];
            _mapManager.ValidStaticIds.CopyTo(_matchedStaticIds, 0);
        }
        else {
            var filter = _tilesFilter.ToLower();
            var matchedLandIds = new List<int>();
            foreach (var index in _mapManager.ValidLandIds) {
                var name = TileDataLoader.Instance.LandData[index].Name?.ToLower() ?? "";
                if(name.Contains(filter) || $"{index}".Contains(_tilesFilter) || $"0x{index:x4}".Contains(filter))
                    matchedLandIds.Add(index);
            }
            _matchedLandIds = matchedLandIds.ToArray();
            
            var matchedStaticIds = new List<int>();
            foreach (var index in _mapManager.ValidStaticIds) {
                var name = TileDataLoader.Instance.StaticData[index].Name?.ToLower() ?? "";
                if(name.Contains(filter) || $"{index}".Contains(_tilesFilter) || $"0x{index:x4}".Contains(filter))
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
        if (ImGui.BeginTable("TilesTable", 3) && _mapManager.Client.Initialized) {
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
        var texture = ArtLoader.Instance.GetLandTexture((uint)index, out var bounds);
        var name = TileDataLoader.Instance.LandData[index].Name;
        TilesDrawRow(index, index, texture, bounds, name);
    }
    
    private void TilesDrawStatic(int index) {
        var realIndex = index + MaxLandIndex;
        var texture = ArtLoader.Instance.GetStaticTexture((uint)index, out var bounds);
        var realBounds = ArtLoader.Instance.GetRealArtBounds(index);
        var name = TileDataLoader.Instance.StaticData[index].Name;
        TilesDrawRow(index, realIndex, texture, new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height), name);
    }
    
    private void TilesDrawRow(int index, int realIndex, Texture2D texture, Rectangle bounds, string name) {
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

    public bool HuesShowWindow;
    private bool _huesUpdateScroll;
    private string _huesFilter = "";
    private int _huesSelectedId;
    public int HuesSelectedId => _huesSelectedId;
    private const int _huesRowHeight = 20;
    
    private void FilterHues() {
        var huesManager = HuesManager.Instance;
        if (_huesFilter.Length == 0) {
            _matchedHueIds = new int[huesManager.HuesCount];
            for (int i = 0; i < huesManager.HuesCount; i++) {
                _matchedHueIds[i] = i;
            }
        }
        else {
            var matchedIds = new List<int>();
            for (int i = 0; i < huesManager.HuesCount; i++) {
                var name = huesManager.Names[i];
                if(name.Contains(_huesFilter) || $"{i}".Contains(_huesFilter) || $"0x{i:X4}".Contains(_huesFilter))
                    matchedIds.Add(i);
            }
            _matchedHueIds = matchedIds.ToArray();
        }
    }
    
    private unsafe void DrawHuesWindow() {
        if (!HuesShowWindow) return;
        ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(250, _graphicsDevice.PresentationParameters.BackBufferHeight - _mainMenuHeight), ImGuiCond.FirstUseEver);
        ImGui.Begin("Hues", ref HuesShowWindow);
        if (ImGui.Button("Scroll to selected")) {
            _huesUpdateScroll = true;
        }

        ImGui.Text("Filter");
        if (ImGui.InputText("", ref _huesFilter, 64)) {
            FilterHues();
        }
        
        ImGui.BeginChild("Hues", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.BeginTable("TilesTable", 2) && _mapManager.Client.Initialized) {
            ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            ImGui.TableSetupColumn("Id" ,ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
            _tilesTableWidth = ImGui.GetContentRegionAvail().X;
            clipper.Begin(_matchedHueIds.Length, _huesRowHeight);
            while (clipper.Step()) {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++){
                    HuesDrawElement(_matchedHueIds[i]);
                }
            }
            clipper.End();
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void HuesDrawElement(int index) {
        var name = HuesManager.Instance.Names[index];
        
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
            if (ImGui.BeginItemTooltip()) {
                ImGui.Text(name);
                ImGui.EndTooltip();
            }
        }

        if (ImGui.TableNextColumn()) {
            DrawImage(HuesManager.Instance.Texture, new Rectangle(0,index, 32, 1), new Vector2(ImGui.GetContentRegionAvail().X, _huesRowHeight));
        }
    }
    
    private bool _minimapShowWindow;

    private void DrawMinimapWindow() {
        if (!_minimapShowWindow) return;

        ImGui.SetNextWindowPos(new Vector2(100, 20), ImGuiCond.FirstUseEver);
        ImGui.Begin("Minimap", ref _minimapShowWindow);
        if (ImGui.Button("Reload")) {
            _mapManager.Client.Send(new RequestRadarMapPacket());
        }
        if (_mapManager.Client.Initialized) {
            var currentPos = ImGui.GetCursorScreenPos();
            var tex = RadarMap.Instance.Texture;
            DrawImage(tex, tex.Bounds);
            if (ImGui.IsMouseHoveringRect(currentPos, new(currentPos.X + tex.Bounds.Width, currentPos.Y + tex.Bounds.Height), true)) {
                if (ImGui.IsWindowFocused() && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                    var coords = ImGui.GetMousePos() - currentPos;
                    _mapManager.SetPos((ushort)(coords.X * 8), (ushort)(coords.Y * 8));
                }
            }
            _mapManager.CalculateViewRange(_mapManager.Camera, out var rect);
            var p1 = currentPos + new Vector2(rect.Left / 8, rect.Top / 8);
            var p2 = currentPos + new Vector2(rect.Right / 8, rect.Bottom / 8);
            ImGui.GetWindowDrawList().AddRect(p1, p2, ImGui.GetColorU32(Red));
        }
        ImGui.End();
    }
}