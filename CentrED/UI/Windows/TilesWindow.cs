using CentrED.Map;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows; 

public class TilesWindow : Window{
    public TilesWindow() {
        CEDClient.Connected += FilterTiles;
    }
    public override string Name => "Tiles";
    
    private string _filter = "";
    internal int _selectedId;
    private bool _updateScroll;
    private bool _landVisible = true;
    private bool _staticVisible = true;
    private float _tableWidth;
    public const int MaxLandIndex = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 _tilesDimensions = new(44, 44);
    
    private int[] _matchedLandIds;
    private int[] _matchedStaticIds;
    
    private void FilterTiles() {
        if (_filter.Length == 0) {
            _matchedLandIds = new int[CEDGame.MapManager.ValidLandIds.Length];
            CEDGame.MapManager.ValidLandIds.CopyTo(_matchedLandIds, 0);
            
            _matchedStaticIds = new int[CEDGame.MapManager.ValidStaticIds.Length];
            CEDGame.MapManager.ValidStaticIds.CopyTo(_matchedStaticIds, 0);
        }
        else {
            var filter = _filter.ToLower();
            var matchedLandIds = new List<int>();
            foreach (var index in CEDGame.MapManager.ValidLandIds) {
                var name = TileDataLoader.Instance.LandData[index].Name?.ToLower() ?? "";
                if(name.Contains(filter) || $"{index}".Contains(_filter) || $"0x{index:x4}".Contains(filter))
                    matchedLandIds.Add(index);
            }
            _matchedLandIds = matchedLandIds.ToArray();
            
            var matchedStaticIds = new List<int>();
            foreach (var index in CEDGame.MapManager.ValidStaticIds) {
                var name = TileDataLoader.Instance.StaticData[index].Name?.ToLower() ?? "";
                if(name.Contains(filter) || $"{index}".Contains(_filter) || $"0x{index:x4}".Contains(filter))
                    matchedStaticIds.Add(index);
            }
            _matchedStaticIds = matchedStaticIds.ToArray();
        }
    }
    public override void Draw() {
         if (!Show) return;
        ImGui.SetNextWindowSize(new Vector2(250, CEDGame._gdm.GraphicsDevice.PresentationParameters.BackBufferHeight - CEDGame.UIManager._mainMenuHeight), ImGuiCond.FirstUseEver);
        ImGui.Begin(Name, ref _show);
        if (ImGui.Button("Scroll to selected")) {
            _updateScroll = true;
        }
        
        ImGui.Text("Filter");
        if (ImGui.InputText("", ref _filter, 64)) {
            FilterTiles();
        }
        
        ImGui.Checkbox("Land", ref _landVisible);
        ImGui.SameLine();
        ImGui.Checkbox("Static", ref _staticVisible);

        ImGui.BeginChild("Tiles", new Vector2(), false, ImGuiWindowFlags.Modal);
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() + 10);
        }
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
            ImGui.SetScrollY(ImGui.GetScrollY() - 10);
        }

        var tilesPosY = ImGui.GetCursorPosY();
        if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Initialized) {
            unsafe {
                ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, _tilesDimensions.X);
                _tableWidth = ImGui.GetContentRegionAvail().X;
                if (_landVisible) {
                    clipper.Begin(_matchedLandIds.Length, _tilesDimensions.Y);
                    while (clipper.Step()) {
                        for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++) {
                            TilesDrawLand(_matchedLandIds[row]);
                        }
                    }
                    clipper.End();
                }

                if (IsLandTile(_selectedId) && _updateScroll) {
                    float itemPosY = clipper.StartPosY + _tilesDimensions.Y * Array.IndexOf(_matchedLandIds, _selectedId);
                    ImGui.SetScrollFromPosY(itemPosY - tilesPosY);
                    _updateScroll = false;
                }
                if (_staticVisible) {
                    clipper.Begin(_matchedStaticIds.Length, _tilesDimensions.Y);
                    while (clipper.Step()) {
                        for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++) {
                            TilesDrawStatic(_matchedStaticIds[row]);
                        }
                    }
                    clipper.End();
                }
                if (_updateScroll) {
                    float itemPosY = clipper.StartPosY + _tilesDimensions.Y * Array.IndexOf(_matchedStaticIds, _selectedId - MaxLandIndex);
                    ImGui.SetScrollFromPosY(itemPosY - tilesPosY);
                    _updateScroll = false;
                }
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
        if (ImGui.TableNextColumn()) {
            var startPos = ImGui.GetCursorPos();
            var selectableSize = new Vector2(_tableWidth, _tilesDimensions.Y);
            if (ImGui.Selectable($"##tile{realIndex}", _selectedId == realIndex,
                    ImGuiSelectableFlags.SpanAllColumns, selectableSize))
                _selectedId = realIndex;
            ImGui.SetCursorPos(startPos with { Y = startPos.Y + (_tilesDimensions.Y - ImGui.GetFontSize()) / 2 });
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn()) {
            CEDGame.UIManager.DrawImage(texture, bounds, _tilesDimensions);
        }

        if (ImGui.TableNextColumn()) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (_tilesDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(name);
        }
    }
    
    public static bool IsLandTile(int id) => id < ArtLoader.MAX_LAND_DATA_INDEX_COUNT;

    public void UpdateSelectedId(MapObject mapObject) {
        _selectedId = mapObject.Tile.Id;
        if (mapObject is StaticObject)
             _selectedId += MaxLandIndex;
        _updateScroll = true;
    }
}