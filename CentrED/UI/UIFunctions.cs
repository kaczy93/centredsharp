using System.Text;
using CentrED.Client;
using CentrED.Map;
using CentrED.Server;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadarMap = CentrED.Map.RadarMap;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI;

public partial class UIManager {
    public static Vector4 Red = new (1, 0, 0, 1);
    public static Vector4 Green = new (0, 1, 0, 1);
    public static Vector4 Blue = new (0, 0, 1, 1);
    
    private void CenterWindow() {
        ImGui.SetWindowPos( 
            new Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth / 2 - ImGui.GetWindowSize().X / 2,
                _graphicsDevice.PresentationParameters.BackBufferHeight / 2 - ImGui.GetWindowSize().Y / 2)
            , ImGuiCond.FirstUseEver
        );
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