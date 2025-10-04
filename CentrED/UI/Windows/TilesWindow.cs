using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class TilesWindow : Window
{
    record struct TileInfo(int RealIndex, Texture2D Texture, Rectangle Bounds, string Name, string Flags, uint Height)
    {
        public static TileInfo INVALID = new(-1, null, default, "", "", 0);
    };

    public TilesWindow()
    {
        Array.Fill(tileDataFiltersCheckBoxes, true);

        CEDClient.Connected += FilterTiles;
    }

    public override string Name => "Tiles";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    private string _filterText = "";
    private int _selectedLandId;
    private int _selectedStaticId;
    private bool _updateScroll;
    private bool _staticMode; // Land/Static
    private bool _gridMode; // List/Grid
    private bool _texMode; // Art/Texmap
    
    private float _tableWidth;
    public const int MAX_LAND_INDEX = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 TilesDimensions = new(44, 44);
    private static readonly float TotalRowHeight = TilesDimensions.Y + ImGui.GetStyle().ItemSpacing.Y;
    public const string STATIC_DRAG_DROP_TYPE = "StaticDragDrop";
    public const string LAND_DRAG_DROP_TYPE = "LandDragDrop";
    
    private int[] _matchedLandIds = [];
    private int[] _matchedStaticIds = [];

    public bool StaticMode => _staticMode;
    public bool LandMode => !_staticMode;
    
    private ushort SelectedId => (ushort)(StaticMode ? _selectedStaticId : _selectedLandId);

    public ushort ActiveId
    {
        get
        {
            if (ActiveTileSetValues.Length == 0)
                return SelectedId;

            if (CEDGame.MapManager.UseRandomTileSet)
                return ActiveTileSetValues[Random.Shared.Next(ActiveTileSetValues.Length)];

            if (CEDGame.MapManager.UseSequentialTileSet)
            {
                // For preview, always show the first tile in the set
                return ActiveTileSetValues[0];
            }

            return SelectedId;
        }
    }

    public ushort GetNextSequentialId()
    {
        if (ActiveTileSetValues.Length == 0)
            return SelectedId;

        if (!CEDGame.MapManager.UseSequentialTileSet)
            return ActiveId;

        ushort tileId = ActiveTileSetValues[CEDGame.MapManager._currentSequenceIndex];

        CEDGame.MapManager._currentSequenceIndex = (CEDGame.MapManager._currentSequenceIndex + 1) % ActiveTileSetValues.Length;

        return tileId;
    }

    private static readonly TileDataFlag[] tileDataFilters = Enum.GetValues<TileDataFlag>();

    private readonly bool[] tileDataFiltersCheckBoxes = new bool[tileDataFilters.Length];
    private bool tileDataFilterOn;
    private bool tileDataFilterInclusive = true;
    private bool tileDataFilterMatchAll;

    private void FilterTiles()
    {
        if (_filterText.Length == 0 && !tileDataFilterOn)
        {
            _matchedLandIds = new int[CEDGame.MapManager.ValidLandIds.Length];
            CEDGame.MapManager.ValidLandIds.CopyTo(_matchedLandIds, 0);

            _matchedStaticIds = new int[CEDGame.MapManager.ValidStaticIds.Length];
            CEDGame.MapManager.ValidStaticIds.CopyTo(_matchedStaticIds, 0);
        }
        else
        {
            ulong allSelectedFlag = 0;
            if (tileDataFilterMatchAll)
            {
                for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                {
                    if (tileDataFiltersCheckBoxes[i])
                        allSelectedFlag |= (ulong)tileDataFilters[i];
                }
            }

            var filter = _filterText.ToLower();
            var matchedLandIds = new List<int>();
            foreach (var index in CEDGame.MapManager.ValidLandIds)
            {
                bool toAdd = false;

                var name = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Name?.ToLower() ?? "";
                if (_filterText.Length == 0 || name.Contains(filter) || $"{index}".Contains(_filterText) || $"0x{index:x4}".Contains(filter))
                    toAdd = true;

                if (toAdd && tileDataFilterOn)
                {
                    TileFlag landFlags = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Flags;

                    toAdd = !tileDataFilterInclusive;

                    if (tileDataFilterMatchAll)
                    {
                        if ((ulong)landFlags == allSelectedFlag)
                        {
                            toAdd = tileDataFilterInclusive;
                        }
                    }
                    else
                    {

                        for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                        {
                            if (tileDataFiltersCheckBoxes[i] == true && (((ulong)landFlags & (ulong)tileDataFilters[i]) != 0))
                            {
                                toAdd = tileDataFilterInclusive;
                                if (tileDataFilterInclusive)
                                    break;
                            }
                        }
                    }

                    //None flag
                    if (landFlags == 0)
                    {
                        toAdd = !(tileDataFiltersCheckBoxes[0] ^ tileDataFilterInclusive);
                    }
                }

                if (toAdd)
                {
                    matchedLandIds.Add(index);
                }
            }
            _matchedLandIds = matchedLandIds.ToArray();

            var matchedStaticIds = new List<int>();
            foreach (var index in CEDGame.MapManager.ValidStaticIds)
            {
                bool toAdd = false;

                var name = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Name?.ToLower() ?? "";
                if (_filterText.Length == 0 || name.Contains(filter) || $"{index}".Contains(_filterText) || $"0x{index:x4}".Contains(filter))
                    toAdd = true;

                if (toAdd && tileDataFilterOn)
                {
                    TileFlag staticFlags = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Flags;

                    toAdd = !tileDataFilterInclusive;

                    if (tileDataFilterMatchAll)
                    {
                        if ((ulong)staticFlags == allSelectedFlag)
                        {
                            toAdd = tileDataFilterInclusive;
                        }
                    }
                    else
                    {
                    
                        for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                        {
                            if (tileDataFiltersCheckBoxes[i] == true && (((ulong)staticFlags & (ulong)tileDataFilters[i]) != 0))
                            {
                                toAdd = tileDataFilterInclusive;
                                if (tileDataFilterInclusive)
                                    break;
                            }
                        }
                    }

                    //None flag
                    if (staticFlags == 0)
                    {
                        toAdd = !(tileDataFiltersCheckBoxes[0] ^ tileDataFilterInclusive);
                    }

                }

                if (toAdd)
                {
                    matchedStaticIds.Add(index);
                }
            }
            _matchedStaticIds = matchedStaticIds.ToArray();
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text("Not connected"u8);
            return;
        }
        if (ImGui.Button("Scroll to selected"))
        {
            _updateScroll = true;
        }
        ImGui.Text("Filter"u8);
        ImGui.InputText("##Filter", ref _filterText, 64);

        if (ImGuiEx.TwoWaySwitch("Land", "Statics", ref _staticMode))
        {
            _updateScroll = true;
            _tileSetIndex = 0;
            ActiveTileSetValues = Empty;
            _tileSetSelectedId = 0;
        }
        if (ImGuiEx.TwoWaySwitch("List", "Grid", ref _gridMode))
        {
            _updateScroll = true;
        }
        if (LandMode)
        {
            ImGuiEx.TwoWaySwitch(" Art", "Tex", ref _texMode);
        }
        ImGui.Text("Tiledata Filter"u8);
        ImGui.SameLine();
        if (tileDataFilterOn)
        {

            if (ImGui.ArrowButton("tdfDown", ImGuiDir.Down))
                tileDataFilterOn = !tileDataFilterOn;
        }
        else
        {
            if (ImGui.ArrowButton("tdfRight", ImGuiDir.Right))
                tileDataFilterOn = !tileDataFilterOn;
        }
        if (tileDataFilterOn)
        {
            DrawTiledataFilter();
        }

        FilterTiles();
        if (_gridMode)
        {
            DrawTilesGrid();
        }
        else
        {
            DrawTilesList();
        }
        DrawTileSets();
    }

    private void DrawTilesList()
    {
        if (ImGui.BeginChild("Tiles", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Running)
            {
                unsafe
                {
                    var clipper = ImGui.ImGuiListClipper();
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                    _tableWidth = ImGui.GetContentRegionAvail().X;
                    var ids = LandMode ? _matchedLandIds : _matchedStaticIds;
                    clipper.Begin(ids.Length, TotalRowHeight);
                    while (clipper.Step())
                    {
                        for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            int tileIndex = ids[rowIndex];
                            var tileInfo = LandMode ? LandInfo(tileIndex) : StaticInfo(ids[rowIndex]);
                            var posY = ImGui.GetCursorPosY();
                            DrawTileRow(tileIndex, tileInfo);
                            ImGui.SetCursorPosY(posY);
                            if (ImGui.Selectable
                                (
                                    $"##tile{tileInfo.RealIndex}",
                                    LandMode ? _selectedLandId == tileIndex : _selectedStaticId == tileIndex,
                                    ImGuiSelectableFlags.SpanAllColumns,
                                    new Vector2(0, TilesDimensions.Y)
                                ))
                            {
                                if (LandMode)
                                    _selectedLandId = tileIndex;
                                else
                                    _selectedStaticId = tileIndex;
                                _tileSetSelectedId = 0;
                            }
                            DrawTooltip(tileInfo);
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (_tileSetIndex != 0 && ImGui.Button("Add to set"))
                                {
                                    AddToTileSet((ushort)tileIndex);
                                    ImGui.CloseCurrentPopup();
                                }
                                if (StaticMode)
                                {
                                    if (ImGui.Button("Filter"))
                                    {
                                        CEDGame.MapManager.StaticFilterIds.Add(tileIndex);
                                        ImGui.CloseCurrentPopup();
                                    }
                                }
                                ImGui.EndPopup();
                            }
                            if (ImGui.BeginDragDropSource())
                            {
                                ImGui.SetDragDropPayload
                                (
                                    LandMode ? LAND_DRAG_DROP_TYPE : STATIC_DRAG_DROP_TYPE,
                                    &tileIndex,
                                    sizeof(int)
                                );
                                ImGui.Text(tileInfo.Name);
                                CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
                                ImGui.EndDragDropSource();
                            }
                        }
                    }
                    clipper.End();
                    if (_updateScroll)
                    {
                        float itemPosY = (float)clipper.StartPosY + TotalRowHeight * Array.IndexOf
                            (ids, LandMode ? _selectedLandId : _selectedStaticId);
                        ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                        _updateScroll = false;
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawTilesGrid()
    {
        if (ImGui.BeginChild("Tiles", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            _tableWidth = ImGui.GetContentRegionAvail().X;
            int columnsNumber = (int)(_tableWidth / (TilesDimensions.X + ImGui.GetStyle().ItemSpacing.X));
            if (columnsNumber < 4)
            {
                columnsNumber = 4;
            }
            if (ImGui.BeginTable("TilesTable", columnsNumber) && CEDClient.Running)
            {
                unsafe
                {
                    var clipper = ImGui.ImGuiListClipper();
                    for (int i = 0; i < columnsNumber; i++)
                    {
                        ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                    }
                    _tableWidth = ImGui.GetContentRegionAvail().X;
                    var ids = LandMode ? _matchedLandIds : _matchedStaticIds;
                    int rowsNumber = (ids.Length / columnsNumber) + 1;
                    clipper.Begin(rowsNumber, TotalRowHeight);
                    while (clipper.Step())
                    {
                        for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
                            for (int columnIndex = 0; columnIndex < columnsNumber; columnIndex++)
                            {
                                if (columnIndex + (columnsNumber * rowIndex) > ids.Length - 1)
                                {
                                    continue;
                                }
                                int tileIndex = ids[columnIndex + (columnsNumber * rowIndex)];
                                var tileInfo = LandMode ? LandInfo(tileIndex) : StaticInfo(tileIndex);
                                if (ImGui.TableNextColumn())
                                {
                                    var oldPos = ImGui.GetCursorPos();
                                    if (tileInfo == TileInfo.INVALID)
                                    {
                                        ImGui.GetWindowDrawList().AddRect
                                            (ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(ImGuiColor.Pink));
                                    }
                                    else
                                    {
                                        if (!CEDGame.UIManager.DrawImage
                                                (tileInfo.Texture, tileInfo.Bounds, TilesDimensions, LandMode) &&
                                            CEDGame.MapManager.DebugLogging)
                                        {
                                            Console.WriteLine
                                                ($"[TilesWindow] No texture found for tile 0x{tileIndex:X4}");
                                        }
                                    }
                                    ImGui.SetCursorPos(oldPos);
                                }
                                if (ImGui.Selectable
                                    (
                                        $"##tile{tileInfo.RealIndex}",
                                        LandMode ? _selectedLandId == tileIndex : _selectedStaticId == tileIndex,
                                        ImGuiSelectableFlags.None,
                                        new Vector2(TilesDimensions.X, TilesDimensions.Y)
                                    ))
                                {
                                    if (LandMode)
                                        _selectedLandId = tileIndex;
                                    else
                                        _selectedStaticId = tileIndex;
                                    _tileSetSelectedId = 0;
                                }
                                DrawTooltip(tileInfo);
                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (_tileSetIndex != 0 && ImGui.Button("Add to set"))
                                    {
                                        AddToTileSet((ushort)tileIndex);
                                        ImGui.CloseCurrentPopup();
                                    }
                                    if (StaticMode)
                                    {
                                        if (ImGui.Button("Filter"))
                                        {
                                            CEDGame.MapManager.StaticFilterIds.Add(tileIndex);
                                            ImGui.CloseCurrentPopup();
                                        }
                                    }
                                    ImGui.EndPopup();
                                }
                                if (ImGui.BeginDragDropSource())
                                {
                                    ImGui.SetDragDropPayload
                                    (
                                        LandMode ? LAND_DRAG_DROP_TYPE : STATIC_DRAG_DROP_TYPE,
                                        &tileIndex,
                                        sizeof(int)
                                    );
                                    ImGui.Text(tileInfo.Name);
                                    CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
                                    ImGui.EndDragDropSource();
                                }
                            }
                        }
                    }
                    clipper.End();
                    if (_updateScroll)
                    {
                        float itemPosY = (float)clipper.StartPosY + TotalRowHeight * (Array.IndexOf
                            (ids, LandMode ? _selectedLandId : _selectedStaticId) / columnsNumber);
                        ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                        _updateScroll = false;
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawTooltip(TileInfo tileInfo)
    {
        if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
        {
            if (ImGui.BeginTable($"##Tooltip{tileInfo.RealIndex}", 2, ImGuiTableFlags.BordersInner))
            {
                ImGui.TableNextColumn();
                ImGui.Text($"0x{tileInfo.RealIndex - (StaticMode ? MAX_LAND_INDEX : 0):X4}");
                ImGui.TextUnformatted(tileInfo.Name);
                if (!LandMode)
                {
                    ImGui.Text($"Height: {tileInfo.Height}");
                }
                ImGui.Text(tileInfo.Flags);
                ImGui.TableNextColumn();
                CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
                ImGui.EndTable();
            }
            ImGui.EndTooltip();
        }
    }

    private int _tileSetIndex;
    private string _tileSetName = "";
    private ushort _tileSetSelectedId;
    private bool _tileSetShowPopupNew;
    private bool _tileSetShowPopupDelete;
    private string _tileSetNewName = "";
    private static readonly ushort[] Empty = Array.Empty<ushort>();
    public ushort[] ActiveTileSetValues = Empty;

    private void DrawTileSets()
    {
        if (ImGui.BeginChild("TileSets"))
        {
            ImGui.Text("Tile Set"u8);

            if (ImGui.Button("New"))
            {
                ImGui.OpenPopup("NewTileSet");
                _tileSetShowPopupNew = true;
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_tileSetIndex == 0);
            if (ImGui.Button("Delete"))
            {
                ImGui.OpenPopup("DeleteTileSet");
                _tileSetShowPopupDelete = true;
            }
            ImGui.EndDisabled();

            var tileSets = LandMode ?
                ProfileManager.ActiveProfile.LandTileSets :
                ProfileManager.ActiveProfile.StaticTileSets;

            string[] names = new[] { String.Empty }.Concat(tileSets.Keys).ToArray();

            if (ImGui.Combo("##TileSetCombo", ref _tileSetIndex, names, names.Length))
            {
                _tileSetName = names[_tileSetIndex];
                if (_tileSetIndex == 0)
                {
                    ActiveTileSetValues = Empty;
                }
                else
                {
                    ActiveTileSetValues = tileSets[_tileSetName].ToArray();
                }
                _tileSetSelectedId = 0;
            }

            if (ImGui.BeginChild("TileSetTable"))
            {
                if (ImGui.BeginTable("TileSetTable", 3) && CEDClient.Running)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                    _tableWidth = ImGui.GetContentRegionAvail().X;
                    var ids =
                        ActiveTileSetValues; //We copy the array here to not crash when removing, please fix :)
                    clipper.Begin(ids.Length, TotalRowHeight);
                    while (clipper.Step())
                    {
                        for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            var tileIndex = ids[rowIndex];
                            var tileInfo = LandMode ? LandInfo(tileIndex) : StaticInfo(tileIndex);
                            var posY = ImGui.GetCursorPosY();
                            DrawTileRow(tileIndex, tileInfo);
                            ImGui.SetCursorPosY(posY);
                            if (ImGui.Selectable
                                (
                                    $"##tileset{tileInfo.RealIndex}_{rowIndex}", // Add rowIndex to make ID unique
                                    _tileSetSelectedId == tileIndex,
                                    ImGuiSelectableFlags.SpanAllColumns,
                                    new Vector2(0, TilesDimensions.Y)
                                ))
                            {
                                _tileSetSelectedId = tileIndex;
                            }
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Button("Move Up"))
                                {
                                    MoveSequentialTileAtIndex(rowIndex); // Use array index instead of tile ID
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.SameLine();
                                if (ImGui.Button("Move Down"))
                                {
                                    MoveSequentialTileAtIndex(rowIndex + 1); // Use array index instead of tile ID
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.Separator();


                                if (ImGui.Button("Remove"))
                                {
                                    RemoveFromTileSetAtIndex(rowIndex); // Use array index instead of tile ID
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    clipper.End();
                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
            if (_tileSetIndex != 0 && ImGui.BeginDragDropTarget())
            {
                var payloadPtr = ImGui.AcceptDragDropPayload
                    (LandMode ? LAND_DRAG_DROP_TYPE : STATIC_DRAG_DROP_TYPE);
                unsafe
                {
                    if (payloadPtr != ImGuiPayloadPtr.Null)
                    {
                        var dataPtr = (int*)payloadPtr.Data;
                        int id = dataPtr[0];
                        AddToTileSet((ushort)id);
                    }
                }
                ImGui.EndDragDropTarget();
            }
            if (ImGui.BeginPopupModal
                (
                    "NewTileSet",
                    ref _tileSetShowPopupNew,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text("Name"u8);
                ImGui.SameLine();
                ImGui.InputText("##TileSetNewName", ref _tileSetNewName, 32);
                if (ImGui.Button("Add"))
                {
                    var currentTileSets = LandMode ?
                        ProfileManager.ActiveProfile.LandTileSets :
                        ProfileManager.ActiveProfile.StaticTileSets;

                    currentTileSets.Add(_tileSetNewName, new List<ushort>());
                    _tileSetIndex = Array.IndexOf(currentTileSets.Keys.ToArray(), _tileSetNewName) + 1;
                    _tileSetName = _tileSetNewName;
                    ActiveTileSetValues = Empty;
                    _tileSetSelectedId = 0;
                    ProfileManager.Save();
                    _tileSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal
                (
                    "DeleteTileSet",
                    ref _tileSetShowPopupDelete,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text($"Are you sure you want to delete tile set '{_tileSetName}'?");
                if (ImGui.Button("Yes"))
                {
                    var currentTileSets = LandMode ?
                        ProfileManager.ActiveProfile.LandTileSets :
                        ProfileManager.ActiveProfile.StaticTileSets;

                    currentTileSets.Remove(_tileSetName);
                    ProfileManager.Save();
                    _tileSetIndex--;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
    }

    private void MoveSequentialTileAtIndex(int index)
    {
        var tileSets = LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;

        var tileSet = tileSets[_tileSetName];

        // Cannot move up if already at the top
        if (index <= 0 || index >= tileSet.Count)
            return;

        // Create a new list since we need to modify the order
        var newOrder = tileSet.ToList();

        // Swap with the item above
        var temp = newOrder[index];
        newOrder[index] = newOrder[index - 1];
        newOrder[index - 1] = temp;

        // Replace the tile set with the reordered list
        tileSet.Clear();
        foreach (var tile in newOrder)
            tileSet.Add(tile);

        ActiveTileSetValues = tileSet.ToArray();
        ProfileManager.Save();
    }

    private void RemoveFromTileSetAtIndex(int index)
    {
        if (index < 0)
            return;

        var tileSets = LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;

        var tileSet = tileSets[_tileSetName];

        if (index < tileSet.Count)
        {
            var newOrder = tileSet.ToList();
            newOrder.RemoveAt(index);

            tileSet.Clear();
            foreach (var tile in newOrder)
                tileSet.Add(tile);

            ActiveTileSetValues = tileSet.ToArray();
            ProfileManager.Save();
        }
    }

    private void AddToTileSet(ushort id)
    {
        var tileSets = LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;

        var tileSet = tileSets[_tileSetName];

        // Always add the tile (allows duplicates)
        tileSet.Add(id);

        ActiveTileSetValues = tileSet.ToArray();
        ProfileManager.Save();
    }

    private void RemoveFromTileSet(ushort id)
    {
        var tileSets = LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;

        var tileSet = tileSets[_tileSetName];
        tileSet.Remove(id);
        ActiveTileSetValues = tileSet.ToArray();
        ProfileManager.Save();
    }

    private TileInfo LandInfo(int index)
    {
        if (CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(index).Length < 0)
        {
            return TileInfo.INVALID;
        }
        SpriteInfo spriteInfo;
        if (_texMode)
        {
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(CEDGame.MapManager.UoFileManager.TileData.LandData[index].TexID);
        }
        else
        {
            spriteInfo = CEDGame.MapManager.Arts.GetLand((uint)index);
        }
        var name = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Name;
        var flags = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Flags.ToString().Replace(", ", "\n");

        return new(index, spriteInfo.Texture, spriteInfo.UV, name, flags, 0);
    }

    private TileInfo StaticInfo(int index)
    {
        var realIndex = index + MAX_LAND_INDEX;
        if (CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(realIndex).Length < 0)
        {
            return TileInfo.INVALID;
        }
        ref var indexEntry = ref CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(index + 0x4000);

        var spriteInfo = CEDGame.MapManager.Arts.GetArt((uint)(index + indexEntry.AnimOffset));
        var realBounds = CEDGame.MapManager.Arts.GetRealArtBounds((uint)(index + indexEntry.AnimOffset));
        var name = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Name;
        var flags = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Flags.ToString().Replace(", ", "\n");

        return new
        (
            realIndex,
            spriteInfo.Texture,
            new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height),
            name,
            flags,
            CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Height
        );
    }

    private void DrawTileRow(int index, TileInfo tileInfo)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY
                (ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2); //center vertically
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn())
        {
            if (tileInfo == TileInfo.INVALID)
            {
               ImGui.GetWindowDrawList().AddRect(ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(ImGuiColor.Pink));
            }
            else
            {
                if (!CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, TilesDimensions, LandMode) &&
                    CEDGame.MapManager.DebugLogging)
                {
                    Console.WriteLine($"[TilesWindow] No texture found for tile 0x{index:X4}");
                }
            }
        }

        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(tileInfo.Name);
        }
    }

    public void UpdateSelectedId(TileObject mapObject)
    {
        if (mapObject is StaticObject)
        {
            _selectedStaticId = mapObject.Tile.Id;
            _staticMode = true;
        }
        else if (mapObject is LandObject)
        {
            _selectedLandId = mapObject.Tile.Id;
            _staticMode = false;
        }
        _updateScroll = true;
    }

    private void DrawTiledataFilter()
    {
        if (ImGui.BeginChild("TiledataFilter", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.Button("Check All"))
            {
                for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                {
                    tileDataFiltersCheckBoxes[i] = true;
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Uncheck All"))
            {
                for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                {
                    tileDataFiltersCheckBoxes[i] = false;
                }
            }
            ImGui.SameLine();
            ImGui.Checkbox("Inclusive", ref tileDataFilterInclusive);
            ImGui.SameLine();
            ImGui.Checkbox("Match All", ref tileDataFilterMatchAll);

            int checkboxWidth = 120;

            int columnsNumber = (int)(_tableWidth / (checkboxWidth + ImGui.GetStyle().ItemSpacing.X));
            if (columnsNumber < 4)
            {
                columnsNumber = 4;
            }

            if (ImGui.BeginTable("TiledataFilterTable", columnsNumber) && CEDClient.Running)
            {
                for (int i = 0; i < columnsNumber; i++)
                {
                    ImGui.TableSetupColumn("col" + i, ImGuiTableColumnFlags.WidthFixed, checkboxWidth);
                }
                int rowsNumber = (tileDataFilters.Length / columnsNumber) + 1;

                for (int rowIndex = 0; rowIndex < rowsNumber; rowIndex++)
                {
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 20);
                    for (int columnIndex = 0; columnIndex < columnsNumber; columnIndex++)
                    {
                        int index = columnIndex + (columnsNumber * rowIndex);
                        if (ImGui.TableNextColumn())
                        {
                            if (index < tileDataFilters.Length)
                            {
                                ImGui.Checkbox(tileDataFilters[index].ToString(), ref tileDataFiltersCheckBoxes[index]);
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }
}