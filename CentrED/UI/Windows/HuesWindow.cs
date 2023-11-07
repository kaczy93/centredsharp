using ImGuiNET;
using Microsoft.Xna.Framework;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows; 

public class HuesWindow : Window{
    public HuesWindow(UIManager uiManager) : base(uiManager) {
        _mapManager.Client.Connected += FilterHues;
    }
    public override string Name => "Hues";
    
    private bool _updateScroll;
    private string _filter = "";
    private float _tableWidth;
    public int SelectedId { get; private set; }

    private const int _huesRowHeight = 20;
    private int[] _matchedHueIds;

    
    private void FilterHues() {
        var huesManager = HuesManager.Instance;
        if (_filter.Length == 0) {
            _matchedHueIds = new int[huesManager.HuesCount];
            for (int i = 0; i < huesManager.HuesCount; i++) {
                _matchedHueIds[i] = i;
            }
        }
        else {
            var matchedIds = new List<int>();
            for (int i = 0; i < huesManager.HuesCount; i++) {
                var name = huesManager.Names[i];
                if(name.Contains(_filter) || $"{i}".Contains(_filter) || $"0x{i:X4}".Contains(_filter))
                    matchedIds.Add(i);
            }
            _matchedHueIds = matchedIds.ToArray();
        }
    }
    public override void Draw() {
        if (!Show) return;
        ImGui.SetNextWindowSize(new Vector2(250, _uiManager._graphicsDevice.PresentationParameters.BackBufferHeight - _uiManager._mainMenuHeight), ImGuiCond.FirstUseEver);
        ImGui.Begin("Hues", ref _show);
        if (ImGui.Button("Scroll to selected")) {
            _updateScroll = true;
        }

        ImGui.Text("Filter");
        if (ImGui.InputText("", ref _filter, 64)) {
            FilterHues();
        }
        var huesPosY = ImGui.GetCursorPosY();
        ImGui.BeginChild("Hues", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.BeginTable("TilesTable", 2) && _mapManager.Client.Initialized) {
            unsafe {
                ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                _tableWidth = ImGui.GetContentRegionAvail().X;
                clipper.Begin(_matchedHueIds.Length, _huesRowHeight);
                while (clipper.Step()) {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) {
                        HuesDrawElement(_matchedHueIds[i]);
                    }
                }
                clipper.End();
                if (_updateScroll) {
                    float itemPosY = clipper.StartPosY + _huesRowHeight * Array.IndexOf(_matchedHueIds, SelectedId);
                    ImGui.SetScrollFromPosY(itemPosY - huesPosY);
                    _updateScroll = false;
                }
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.End();
    }
    
    private void HuesDrawElement(int index) {
        var name = HuesManager.Instance.Names[index];
        
        ImGui.TableNextRow(ImGuiTableRowFlags.None, _huesRowHeight);
        if (ImGui.TableNextColumn()) {
            var startPos = ImGui.GetCursorPos();

            var selectableSize = new Vector2(_tableWidth, _huesRowHeight);
            if (ImGui.Selectable($"##hue{index}", SelectedId == index,
                    ImGuiSelectableFlags.SpanAllColumns, selectableSize))
                SelectedId = index;

            ImGui.SetCursorPos(startPos with { Y = startPos.Y + (_huesRowHeight - ImGui.GetFontSize()) / 2 });
            ImGui.Text($"0x{index:X4}");
            if (ImGui.BeginItemTooltip()) {
                ImGui.Text(name);
                ImGui.EndTooltip();
            }
        }

        if (ImGui.TableNextColumn()) {
            _uiManager.DrawImage(HuesManager.Instance.Texture, new Rectangle(0,index, 32, 1), new Vector2(ImGui.GetContentRegionAvail().X, _huesRowHeight));
        }
    }

    public void UpdateSelectedHue(ushort staticTileHue) {
        SelectedId = staticTileHue - 1;
        _updateScroll = true;
    }
}