using CentrED.Client;
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

    public bool HuesShowWindow;
    private bool _huesUpdateScroll;
    private string _huesFilter = "";
    private int _huesSelectedId;
    private float _huesTableWidth;
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
            _huesTableWidth = ImGui.GetContentRegionAvail().X;
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

            var selectableSize = new Vector2(_huesTableWidth, _huesRowHeight);
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