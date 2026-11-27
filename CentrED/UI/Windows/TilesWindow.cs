using CentrED.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.CentrED;
using static CentrED.LangEntry;
using static Hexa.NET.ImGui.ImGuiSelectableFlags;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class TilesWindow : Window
{
    public record struct TileInfo(uint RealIndex, Texture2D? Texture, Rectangle Bounds, string Name, string Flags, uint Height)
    {
        public static TileInfo INVALID = new(uint.MaxValue, null, default, "", "", 0);
    };
    
    private static readonly ConfigConverter<Dictionary<string, List<uint>>> TILE_SETS_CONVERTER =
        ConfigConverters.Validated<Dictionary<string, List<uint>>>(sets => sets.Values.All(ids => ids.All(id => id <= TileId.MAX)));

    private readonly ConfigValue<Dictionary<string, List<uint>>> _LandTileSets =  App.Profile.Active.Init("land_tile_sets", new Dictionary<string, List<uint>>(), TILE_SETS_CONVERTER);
    private readonly ConfigValue<Dictionary<string, List<uint>>> _StaticTileSets = App.Profile.Active.Init("static_tile_sets", new Dictionary<string, List<uint>>(), TILE_SETS_CONVERTER);

    public TilesWindow() : base(true)
    {
        App.Client.Connected += OnConnected;
        App.Client.Disconnected += OnDisconnected;
        App.Workspace.Changed += OnWorkspaceChanged;
    }

    private void OnWorkspaceChanged()
    {
        if (!App.Client.Running)
            return;
        UpdateTileSetNames();
        UpdateTileSetValues();
        FilterTiles();
        _updateScroll = true;
    }

    private void OnConnected()
    {
        UpdateTileSetNames();
        UpdateTileSetValues();
        FilterTiles();
        _recalculateTextWidth = true;
        //TODO: I don't like these events
        App.UIManager.FontChanged += RecalculateTiledataTextWidth;
    }

    private void OnDisconnected()
    {
        App.UIManager.FontChanged -= RecalculateTiledataTextWidth;
    }

    public override string Name => App.Lang.Get(TILES_WINDOW) + "###Tiles";

    private readonly ConfigValue<string> _filterText = App.Workspace.Active.Init("tiles.filter_text", "");
    private uint _lastSelectedTerrainId;
    private uint _lastSelectedObjectId;
    private bool _updateScroll;
    private readonly ConfigValue<bool> _objectMode = App.Workspace.Active.Init("tiles.object_mode", false); // Terrain/Object
    private readonly ConfigValue<bool> _gridMode = App.Workspace.Active.Init("tiles.grid_mode", false); // List/Grid
    private readonly ConfigValue<bool> _texMode = App.Workspace.Active.Init("tiles.tex_mode", false); // Art/Texmap
    
    public static Vector2 TilesDimensions = new(44, 44);
    public const string TERRAIN_DRAG_DROP_TYPE = "TerrainDragDrop";
    public const string OBJECT_DRAG_DROP_TYPE = "ObjectDragDrop";

    private List<uint> _matchedTerrainIds = [];
    private List<uint> _matchedObjectIds = [];

    public bool TerrainMode => !_objectMode.Value;
    public bool ObjectMode => _objectMode.Value;

    private MultiSelectStorage<uint> _terrainSelection = new([0]);
    private MultiSelectStorage<uint> _objectSelection = new([0]);
    private MultiSelectStorage<uint> Selection => ObjectMode ? _objectSelection : _terrainSelection;
    private uint LastSelectedId
    {
        get => ObjectMode ? _lastSelectedObjectId : _lastSelectedTerrainId;
        set {
            if (ObjectMode)
            {
                _lastSelectedObjectId = value;
            }
            else
            {
                _lastSelectedTerrainId = value;
            }
        }
    }

    public ICollection<uint> SelectedIds => Selection.Items;
        
    private void FilterTiles()
    {
        if (_filterText.Value.Length == 0 && !_tiledataFilterEnabled.Value)
        {
            _matchedTerrainIds = App.MapManager.ValidLandIds.ToList();
            _matchedObjectIds = App.MapManager.ValidStaticIds.ToList();
        }
        else
        {
            _matchedTerrainIds.Clear();
            foreach (var index in App.MapManager.ValidLandIds)
            {
                var tiledata = App.MapManager.UoFileManager.TileData.LandData[index];
                if (FilterTile(index, tiledata.Name ?? "", (ulong)tiledata.Flags))
                {
                    _matchedTerrainIds.Add(index);
                }
            }
        
            _matchedObjectIds.Clear();
            foreach (var index in App.MapManager.ValidStaticIds)
            {
                var tiledata = App.MapManager.UoFileManager.TileData.StaticData[index];
                if (FilterTile(index, tiledata.Name ?? "", (ulong)tiledata.Flags))
                {
                    _matchedObjectIds.Add(index);
                }
            }
        }
    }

    private bool FilterTile(uint id, string name, ulong flags)
    {
        if (!string.IsNullOrWhiteSpace(_filterText.Value) &&  
            !(name.Contains(_filterText.Value, StringComparison.InvariantCultureIgnoreCase) || 
            id.FormatId(NumberDisplayFormat.HEX).Contains(_filterText.Value, StringComparison.InvariantCultureIgnoreCase) || 
            id.FormatId(NumberDisplayFormat.DEC).Contains(_filterText.Value, StringComparison.InvariantCultureIgnoreCase)))
            return false;

        if (_tiledataFilterEnabled.Value)
        {
            var matched = _tiledataFilterMatchAll.Value ? 
                (flags & _tiledataFilterValue.Value) == _tiledataFilterValue.Value: 
                _tiledataFilterValue.Value == 0 || (flags & _tiledataFilterValue.Value) > 0;
            return !(_tiledataFilterInclusive.Value ^ matched);
        }

        return true;
    }

    protected override void InternalDraw()
    {
        if (!App.Client.Running)
        {
            ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            return;
        }
        if (ImGui.Button(App.Lang.Get(SCROLL_TO_SELECTED)))
        {
            _updateScroll = true;
        }
        ImGui.Text(App.Lang.Get(FILTER));
        if (ImGui.InputText("##Filter", ref _filterText.Value, 64))
        {
            FilterTiles();
        }

        if (ImGuiEx.TwoWaySwitch(App.Lang.Get(LAND), App.Lang.Get(OBJECTS), ref _objectMode.Value))
        {
            _updateScroll = true;
            UpdateTileSetNames();
            UpdateTileSetValues();
        }
        if (ImGuiEx.TwoWaySwitch(App.Lang.Get(LIST), App.Lang.Get(GRID), ref _gridMode.Value))
        {
            _updateScroll = true;
        }
        if (TerrainMode)
        {
            ImGuiEx.TwoWaySwitch(" Art", "Tex", ref _texMode.Value);
        }
        ImGui.Text(App.Lang.Get(TILEDATA_FILTER));
        ImGui.SameLine();
        if (ImGui.ArrowButton("tdf", _tiledataFilterEnabled.Value ? ImGuiDir.Down : ImGuiDir.Right))
            _tiledataFilterEnabled.Value = !_tiledataFilterEnabled.Value;
        if (_tiledataFilterEnabled.Value)
        {
            if(_recalculateTextWidth)
                RecalculateTiledataTextWidth();
            DrawTiledataFilter();
        }

        ImGui.SetNextWindowSizeConstraints(ImGuiEx.MIN_SIZE, ImGui.GetContentRegionAvail() - ImGuiEx.MIN_HEIGHT);
        if (_gridMode.Value)
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
        if (ImGui.BeginChild("Tiles", ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("TilesTable", 3) && App.Client.Running)
            {
                var clipper = ImGui.ImGuiListClipper();
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                var ids = ObjectMode ? _matchedObjectIds : _matchedTerrainIds;
                clipper.Begin(ids.Count);
                Selection.Begin(ids, clipper, ImGuiMultiSelectFlags.BoxSelect1D);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var tileIndex = ids[i];
                        var tileInfo = GetTileInfo(tileIndex);
                        DrawTileRow(i, tileIndex, tileInfo);
                        Tooltip(tileIndex, tileInfo);
                        DragDropSource();
                        TilesContextMenu(tileIndex);
                    }
                }
                Selection.End();
                if (_updateScroll)
                {
                    float itemPosY = (float)clipper.StartPosY + clipper.ItemsHeight * ids.IndexOf(LastSelectedId);
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    _updateScroll = false;
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawTilesGrid()
    {
        if (ImGui.BeginChild("Tiles", ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            int columns = (int)(ImGui.GetContentRegionAvail().X / (TilesDimensions.X + ImGui.GetStyle().ItemSpacing.X));
            if (ImGui.BeginTable("TilesTable", columns) && App.Client.Running)
            {
                var clipper = ImGui.ImGuiListClipper();
                var ids = ObjectMode ? _matchedObjectIds : _matchedTerrainIds;
                int rowsNumber = ids.Count / columns + 1;
                clipper.Begin(rowsNumber);
                Selection.Begin(ids, clipper, ImGuiMultiSelectFlags.BoxSelect2D);
                while (clipper.Step())
                {
                    for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                    {
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
                        for (int column = 0; column < columns; column++)
                        {
                            var index = column + columns * row;
                            if (index >= ids.Count)
                                break;
                            
                            var tileIndex = ids[index];
                            var tileInfo = GetTileInfo(tileIndex);
                            
                            ImGui.TableNextColumn();
                            var oldPos = ImGui.GetCursorPos();
                            DrawTileArt(tileInfo, TilesDimensions, TerrainMode);
                            ImGui.SetCursorPos(oldPos);
                            var selected = Selection.Contains(tileIndex);
                            ImGui.SetNextItemSelectionUserData(index);
                            if (ImGui.Selectable($"##tile{tileIndex}", selected, TilesDimensions))
                            {
                                LastSelectedId = tileIndex;
                            }
                            Tooltip(tileIndex, tileInfo);
                            TilesContextMenu(tileIndex);
                            DragDropSource();
                        }
                    }
                }
                Selection.End();
                if (_updateScroll)
                {
                    float itemPosY = (float)clipper.StartPosY + clipper.ItemsHeight * ids.IndexOf(LastSelectedId) / columns;
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    _updateScroll = false;
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void TilesContextMenu(uint tileIndex)
    {
        if (ImGui.BeginPopupContextItem())
        {
            if (ImGui.Button(App.Lang.Get(ADD_TO_SET)))
            {
                TileSetAddTile(tileIndex);
                ImGui.CloseCurrentPopup();
            }
            if (ObjectMode)
            {
                if (ImGui.Button(App.Lang.Get(ADD_TO_FILTER)))
                {
                    App.MapManager.ObjectIdFilter.Value.Add(tileIndex);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndPopup();
        }
    }

    private void DragDropSource()
    {
        var type = ObjectMode ? OBJECT_DRAG_DROP_TYPE : TERRAIN_DRAG_DROP_TYPE;
        ImGuiEx.DragDropSource(type, Selection.Items);
    }

    private void Tooltip(uint tileIndex, TileInfo tileInfo)
    {
        if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
        {
            if (ImGui.BeginTable($"##Tooltip{tileIndex}", 2, ImGuiTableFlags.BordersInner))
            {
                ImGui.TableNextColumn();
                ImGui.Text(tileIndex.FormatId());
                ImGui.TextUnformatted(tileInfo.Name);
                if (ObjectMode)
                {
                    ImGui.Text($"{App.Lang.Get(HEIGHT)}: {tileInfo.Height}");
                }
                ImGui.TextUnformatted(tileInfo.Flags);
                ImGui.TableNextColumn();
                DrawTileArt(tileInfo);
                ImGui.EndTable();
            }
            ImGui.EndTooltip();
        }
    }

    private int _tileSetIndexTerrain;
    private int _tileSetIndexObject;
    private int TileSetIndex
    {
        get => ObjectMode ? _tileSetIndexObject : _tileSetIndexTerrain;
        set
        {
            if (ObjectMode)
            {
                _tileSetIndexObject = value;
            }
            else
            {
                _tileSetIndexTerrain = value;
            }
        }
    }
    
    private string[] _tilesSetNames = [];
    private string _tileSetNewName = "";
    
    private List<uint> _tileSetTempTerrain = [];
    private List<uint> _tileSetTempObject = [];
    private List<uint> TileSetTemp => ObjectMode ? _tileSetTempObject : _tileSetTempTerrain;

    private int _tileSetRemoveAtIndex = -1;
    
    private Dictionary<string, List<uint>> TileSets => ObjectMode ? 
        _StaticTileSets.Value :
        _LandTileSets.Value;
    private string ActiveTileSetName => _tilesSetNames[TileSetIndex];
    private List<uint> ActiveTileSet => TileSetIndex == 0 ? TileSetTemp : TileSets[ActiveTileSetName];
    
    public List<uint> ActiveTileSetValues = [];

    private void DrawTileSets()
    {
        if (ImGui.BeginChild("TileSets"))
        {
            ImGui.Text(App.Lang.Get(TILE_SET));

            if (ImGui.Button(App.Lang.Get(NEW)))
            {
                ImGui.OpenPopup("AddSet");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(TileSetIndex == 0);
            if (ImGui.Button(App.Lang.Get(DELETE)))
            {
                ImGui.OpenPopup("DelSet");
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveTileSetValues.Count == 0);
            if (ImGui.Button(App.Lang.Get(CLEAR)))
            {
                ClearTileSet();
            }
            ImGui.EndDisabled();

            var index = TileSetIndex;
            if (ImGui.Combo("##TileSetCombo", ref index, _tilesSetNames, _tilesSetNames.Length))
            {
                TileSetIndex = index;
                UpdateTileSetValues();
            }
            if (ImGui.BeginChild("TileSetTable"))
            {
                if (ImGui.BeginTable("TileSetTable", 3) && App.Client.Running)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                    var ids = ActiveTileSetValues;
                    clipper.Begin(ids.Count);
                    Selection.Begin(ids, clipper, ImGuiMultiSelectFlags.BoxSelect1D);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            var tileIndex = ids[i];
                            var tileInfo = GetTileInfo(tileIndex);
                            DrawTileRow(i, tileIndex, tileInfo);
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Button(App.Lang.Get(MOVE_UP)))
                                {
                                    TileSetMoveTile(i, i-1);
                                    ImGui.CloseCurrentPopup();
                                }
                                if (ImGui.Button(App.Lang.Get(MOVE_DOWN)))
                                {
                                    TileSetMoveTile(i, i+1);
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.Separator();
                                
                                if (ImGui.Button(App.Lang.Get(REMOVE)))
                                {
                                    _tileSetRemoveAtIndex = i;
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    Selection.End();
                    ImGui.EndTable();
                }
                if (_tileSetRemoveAtIndex != -1)
                {
                    TileSetRemoveTile(_tileSetRemoveAtIndex);
                    _tileSetRemoveAtIndex = -1;
                }
            }
            ImGui.EndChild();
            if (ImGuiEx.DragDropTarget<uint>(ObjectMode ? OBJECT_DRAG_DROP_TYPE : TERRAIN_DRAG_DROP_TYPE, out var tileIds))
            {
                foreach (var id in tileIds)
                {
                    TileSetAddTile(id);
                }
            }
            if (ImGui.BeginPopupModal("AddSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGuiEx.InputText(App.Lang.Get(NAME), "##TileSetNewName", ref _tileSetNewName, 32);
                ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_tileSetNewName) || _tilesSetNames.Contains(_tileSetNewName));
                if (ImGui.Button(App.Lang.Get(CREATE)))
                {
                    TileSets.Add(_tileSetNewName, new List<uint>());
                    UpdateTileSetNames();
                    TileSetIndex = Array.IndexOf(_tilesSetNames, _tileSetNewName);
                    UpdateTileSetValues();
                    _tileSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button(App.Lang.Get(CANCEL)))
                {
                    _tileSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal("DelSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.TextUnformatted(string.Format(App.Lang.Get(DELETE_WARNING_1TYPE_2NAME), App.Lang.Get(TILE_SET).ToLower(), ActiveTileSetName));
                if (ImGui.Button(App.Lang.Get(YES)))
                {
                    TileSets.Remove(ActiveTileSetName);
                    TileSetIndex--;
                    UpdateTileSetNames();
                    UpdateTileSetValues();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(App.Lang.Get(NO)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
    }

    private void TileSetMoveTile(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= ActiveTileSet.Count || newIndex < 0 || newIndex >= ActiveTileSet.Count)
            return;
        
        var val = ActiveTileSet[oldIndex];
        ActiveTileSet.RemoveAt(oldIndex);
        ActiveTileSet.Insert(newIndex, val);
        UpdateTileSetValues();
    }

    private void TileSetRemoveTile(int index)
    {
        if (index < 0 || index >= ActiveTileSet.Count)
            return;
        
        ActiveTileSet.RemoveAt(index);
        UpdateTileSetValues();
    }

    private void TileSetAddTile(uint id)
    {
        ActiveTileSet.Add(id);
        UpdateTileSetValues();
    }

    private TileInfo GetTileInfo(uint index)
    {
        return ObjectMode ? GetObjectInfo(index) : GetTerrainInfo(index);
    }

    private TileInfo GetTerrainInfo(uint index)
    {
        if (!App.MapManager.IsValidLandId(index))
        {
            if(App.MapManager.DebugLogging)
                Console.WriteLine($"Requested invalid terrain info for index {index.FormatId()}");
            
            return TileInfo.INVALID;
        }
        
        SpriteInfo spriteInfo;
        if (_texMode.Value)
        {
            spriteInfo = App.MapManager.Texmaps.GetTexmap(App.MapManager.GetLandData(index).TexID);
        }
        else
        {
            var isArtValid = App.MapManager.UoFileManager.Arts.File.GetValidRefEntry(TileId.ToInt32OrInvalid(index)).Length > 0;
            var artIndex = isArtValid ? (uint)index : 0;
            spriteInfo = App.MapManager.Arts.GetLand(artIndex);
        }
        var landData = App.MapManager.GetLandData(index);
        var name = landData.Name;
        var flags = landData.Flags.ToString().Replace(", ", "\n");

        return new TileInfo(index, spriteInfo.Texture, spriteInfo.UV, name, flags, 0);
    }

    public TileInfo GetObjectInfo(uint index)
    {
        var realIndex = index + TileId.MAX_LAND_INDEX;
        var refEntry = App.MapManager.UoFileManager.Arts.File.GetValidRefEntry(TileId.ToInt32OrInvalid(realIndex));
        if (refEntry.Length <= 0)
        {
            return TileInfo.INVALID;
        }

        var spriteInfo = App.MapManager.Arts.GetArt((uint)(index + refEntry.AnimOffset));
        var realBounds = App.MapManager.Arts.GetRealArtBounds((uint)(index + refEntry.AnimOffset));
        var name = App.MapManager.GetStaticData(index).Name ?? "";
        var flags = App.MapManager.GetStaticData(index).Flags.ToString().Replace(", ", "\n");

        return new TileInfo
        (
            realIndex,
            spriteInfo.Texture,
            new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height),
            name,
            flags,
            App.MapManager.GetStaticData(index).Height
        );
    }

    private void DrawTileArt(TileInfo tileInfo, Vector2 sizeOverride = default, bool stretch = false)
    {
        if (tileInfo == TileInfo.INVALID)
        {
            ImGui.GetWindowDrawList().AddRect(ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(ImGuiColor.Pink));
        }
        else
        {
            var size = new Vector2(tileInfo.Bounds.Width, tileInfo.Bounds.Height);
            if (sizeOverride != default)
            {
                size = sizeOverride;
            }
            if (!App.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, size, stretch) &&
                App.MapManager.DebugLogging)
            {
                Console.WriteLine($"[TilesWindow] No texture found for tile {tileInfo.RealIndex.FormatId()}");
            }
        }
    }

    public void DrawTileRow(int index, uint tileId, TileInfo tileInfo)
    {
        ImGui.PushID(index);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2); //center vertically
        ImGui.Text($"{tileId.FormatId()}");

        ImGui.TableNextColumn();
        DrawTileArt(tileInfo, TilesDimensions, TerrainMode);
        
        ImGui.TableNextColumn();
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
        var selected = Selection.Contains(tileId);
        ImGui.SetNextItemSelectionUserData(index);
        if (ImGui.Selectable(tileInfo.Name, selected, SpanAllColumns, TilesDimensions with { X = 0 }))
        {
            LastSelectedId = tileId;
        }
        ImGui.PopStyleVar();
        ImGui.PopID();
    }

    public void UpdateSelection(TileObject mapObject)
    {
        if (mapObject is StaticObject)
        {
            _objectMode.Value = true;
        }
        else if (mapObject is LandObject)
        {
            _objectMode.Value = false;
        }
        else
        {
            return;
        }
        UpdateTileSetNames();
        UpdateTileSetValues();
        Selection.SetSelection(mapObject.Tile.Id);
        LastSelectedId = mapObject.Tile.Id;
        _updateScroll = true;
    }

    private void UpdateTileSetNames()
    {
        _tilesSetNames = TileSets.Keys.Prepend("").ToArray();
        _tileSetIndexTerrain = Math.Clamp(_tileSetIndexTerrain, 0, _LandTileSets.Value.Count);
        _tileSetIndexObject = Math.Clamp(_tileSetIndexObject, 0, _StaticTileSets.Value.Count);
    }
    
    private void UpdateTileSetValues()
    {
        ActiveTileSetValues = ActiveTileSet;
    }
    
    private void ClearTileSet()
    {
        ActiveTileSet.Clear();
        UpdateTileSetValues();
    }
    
    private readonly ConfigValue<bool> _tiledataFilterEnabled = App.Workspace.Active.Init("tiles.tiledata_filter.enabled", false);
    private readonly ConfigValue<bool> _tiledataFilterInclusive = App.Workspace.Active.Init("tiles.tiledata_filter.inclusive", true);
    private readonly ConfigValue<bool> _tiledataFilterMatchAll = App.Workspace.Active.Init("tiles.tiledata_filter.match_all", false);
    private readonly ConfigValue<ulong> _tiledataFilterValue = App.Workspace.Active.Init("tiles.tiledata_filter.value", 0ul);

    private bool _recalculateTextWidth;
    private int _maxTiledataTextWidth;
    
    private static readonly TileFlag[] TiledataFilterFlags = Enum.GetValues<TileFlag>().Where(f => f != TileFlag.None).ToArray();
    private static readonly ulong TiledataFilterAllValue = TiledataFilterFlags.Aggregate(0ul, (a, b) => a | (ulong)b);
    
    private void DrawTiledataFilter()
    {
        if (ImGui.BeginChild("TiledataFilter", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.Button(App.Lang.Get(CHECK_ALL)))
            {
                _tiledataFilterValue.Value = TiledataFilterAllValue;
            }
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(UNCHECK_ALL)))
            {
                _tiledataFilterValue.Value = 0ul;
            }
            ImGui.SameLine();
            ImGui.Checkbox(App.Lang.Get(INCLUSIVE), ref _tiledataFilterInclusive.Value);
            ImGui.SameLine();
            ImGui.Checkbox(App.Lang.Get(MATCH_ALL), ref _tiledataFilterMatchAll.Value);

            var columns = (int)(ImGui.GetContentRegionAvail().X / (_maxTiledataTextWidth + ImGui.GetStyle().ItemSpacing.X));
            var rows = TiledataFilterFlags.Length / columns + 1;
            if (ImGui.BeginTable("TiledataFilterTable", columns) && App.Client.Running)
            {
                for (var i = 0; i < columns; i++)
                {
                    ImGui.TableSetupColumn("col" + i, ImGuiTableColumnFlags.WidthFixed, _maxTiledataTextWidth);
                }
                for (var y = 0; y < rows; y++)
                {
                    ImGui.TableNextRow();
                    for (var x = 0; x < columns; x++)
                    {
                        var index = x + columns * y;
                        ImGui.TableNextColumn();
                        if (index < TiledataFilterFlags.Length)
                        {
                            var flag = TiledataFilterFlags[index];
                            var flagValue = (ulong)flag;
                            var enabled = (_tiledataFilterValue.Value & flagValue) > 0;
                            //TODO: Allocate all the flags names once
                            if (ImGui.Checkbox(flag.ToString(), ref enabled))
                            {
                                _tiledataFilterValue.Value ^= flagValue;
                                FilterTiles();
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void RecalculateTiledataTextWidth()
    {
        var max = 30;
        foreach (var flag in TiledataFilterFlags)
        {
            var size = ImGui.CalcTextSize(flag.ToString());
            var width = (int)(size.X + size.Y + ImGui.GetStyle().ItemSpacing.X);  //X + approx of checkbox.X
            max = Math.Max(width, max);
        }
        _maxTiledataTextWidth = max;
        _recalculateTextWidth = false;
    }
}