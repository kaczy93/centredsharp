using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using static CentrED.LangEntry;
using static Hexa.NET.ImGui.ImGuiSelectableFlags;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class TilesWindow : Window
{
    record struct TileInfo(int RealIndex, Texture2D? Texture, Rectangle Bounds, string Name, string Flags, uint Height)
    {
        public static TileInfo INVALID = new(-1, null, default, "", "", 0);
    };

    public TilesWindow()
    {
        CEDClient.Connected += OnConnected;
        CEDClient.Disconnected += OnDisconnected;
    }

    private void OnConnected()
    {
        UpdateTileSetNames();
        UpdateTileSetValues();
        FilterTiles();
        _recalculateTextWidth = true;
        //TODO: I don't like these events
        CEDGame.UIManager.FontChanged += RecalculateTiledataTextWidth;
    }

    private void OnDisconnected()
    {
        CEDGame.UIManager.FontChanged -= RecalculateTiledataTextWidth;
    }

    public override string Name => LangManager.Get(TILES_WINDOW) + "###Tiles";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    private string _filterText = "";
    private int _selectedTerrainId;
    private int _selectedObjectId;
    private bool _updateScroll;
    private bool _objectMode; // Terrain/Object
    private bool _gridMode; // List/Grid
    private bool _texMode; // Art/Texmap
    
    public const int MAX_TERRAIN_INDEX = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 TilesDimensions = new(44, 44);
    public const string TERRAIN_DRAG_DROP_TYPE = "TerrainDragDrop";
    public const string OBJECT_DRAG_DROP_TYPE = "ObjectDragDrop";

    private int[] _matchedTerrainIds = [];
    private int[] _matchedObjectIds = [];

    public bool TerrainMode => !_objectMode;
    public bool ObjectMode => _objectMode;

    public ushort SelectedId => (ushort)(ObjectMode ? _selectedObjectId : _selectedTerrainId);
        
    private void FilterTiles()
    {
        if (_filterText.Length == 0 && !_tiledataFilterEnabled)
        {
            _matchedTerrainIds = new int[CEDGame.MapManager.ValidLandIds.Length];
            CEDGame.MapManager.ValidLandIds.CopyTo(_matchedTerrainIds, 0);

            _matchedObjectIds = new int[CEDGame.MapManager.ValidStaticIds.Length];
            CEDGame.MapManager.ValidStaticIds.CopyTo(_matchedObjectIds, 0);
            return;
        }
        
        var matchedTerrainIds = new List<int>();
        foreach (var index in CEDGame.MapManager.ValidLandIds)
        {
            var tiledata = CEDGame.MapManager.UoFileManager.TileData.LandData[index];
            if (FilterTile(index, tiledata.Name ?? "", (ulong)tiledata.Flags))
            {
                matchedTerrainIds.Add(index);
            }
        }
        _matchedTerrainIds = matchedTerrainIds.ToArray();
        
        var matchedObjectIds = new List<int>();
        foreach (var index in CEDGame.MapManager.ValidStaticIds)
        {
            var tiledata = CEDGame.MapManager.UoFileManager.TileData.StaticData[index];
            if (FilterTile(index, tiledata.Name ?? "", (ulong)tiledata.Flags))
            {
                matchedObjectIds.Add(index);
            }
        }
        _matchedObjectIds = matchedObjectIds.ToArray();
    }

    private bool FilterTile(int id, string name, ulong flags)
    {
        if (!string.IsNullOrWhiteSpace(_filterText) &&  
            !(name.Contains(_filterText, StringComparison.InvariantCultureIgnoreCase) || 
            id.FormatId(NumberDisplayFormat.HEX).Contains(_filterText, StringComparison.InvariantCultureIgnoreCase) || 
            id.FormatId(NumberDisplayFormat.DEC).Contains(_filterText, StringComparison.InvariantCultureIgnoreCase)))
            return false;

        if (_tiledataFilterEnabled)
        {
            var matched = _tiledataFilterMatchAll ? 
                (flags & _tiledataFilterValue) == _tiledataFilterValue: 
                _tiledataFilterValue == 0 || (flags & _tiledataFilterValue) > 0;
            return !(_tiledataFilterInclusive ^ matched);
        }

        return true;
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        if (ImGui.Button(LangManager.Get(SCROLL_TO_SELECTED)))
        {
            _updateScroll = true;
        }
        ImGui.Text(LangManager.Get(FILTER));
        if (ImGui.InputText("##Filter", ref _filterText, 64))
        {
            FilterTiles();
        }

        if (ImGuiEx.TwoWaySwitch(LangManager.Get(LAND), LangManager.Get(OBJECTS), ref _objectMode))
        {
            _updateScroll = true;
            UpdateTileSetNames();
            UpdateTileSetValues();
        }
        if (ImGuiEx.TwoWaySwitch(LangManager.Get(LIST), LangManager.Get(GRID), ref _gridMode))
        {
            _updateScroll = true;
        }
        if (TerrainMode)
        {
            ImGuiEx.TwoWaySwitch(" Art", "Tex", ref _texMode);
        }
        ImGui.Text(LangManager.Get(TILEDATA_FILTER));
        ImGui.SameLine();
        if (ImGui.ArrowButton("tdf", _tiledataFilterEnabled ? ImGuiDir.Down : ImGuiDir.Right))
            _tiledataFilterEnabled = !_tiledataFilterEnabled;
        if (_tiledataFilterEnabled)
        {
            if(_recalculateTextWidth)
                RecalculateTiledataTextWidth();
            DrawTiledataFilter();
        }

        ImGui.SetNextWindowSizeConstraints(ImGuiEx.MIN_SIZE, ImGui.GetContentRegionAvail() - ImGuiEx.MIN_HEIGHT);
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
        if (ImGui.BeginChild("Tiles", ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Running)
            {
                var clipper = ImGui.ImGuiListClipper();
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                var ids = ObjectMode ? _matchedObjectIds : _matchedTerrainIds;
                clipper.Begin(ids.Length, TilesDimensions.Y);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var tileIndex = ids[i];
                        var tileInfo = GetTileInfo(tileIndex);
                        ImGui.PushID(i);
                        DrawTileRow(tileIndex, tileInfo);
                        ImGui.PopID();
                        Tooltip(tileInfo);
                        DragDropSource(tileIndex, tileInfo);
                        TilesContextMenu(tileIndex);
                    }
                }
                clipper.End();
                if (_updateScroll)
                {
                    float itemPosY = (float)clipper.StartPosY + TilesDimensions.Y * Array.IndexOf(ids, SelectedId);
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
            if (ImGui.BeginTable("TilesTable", columns) && CEDClient.Running)
            {
                var clipper = ImGui.ImGuiListClipper();
                var ids = ObjectMode ? _matchedObjectIds : _matchedTerrainIds;
                int rowsNumber = ids.Length / columns + 1;
                clipper.Begin(rowsNumber, TilesDimensions.Y);
                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
                        for (int column = 0; column < columns; column++)
                        {
                            int tileIndex = ids[column + (columns * i)];
                            if (tileIndex > ids.Length )
                                continue;
                           
                            var tileInfo = GetTileInfo(tileIndex);
                            
                            ImGui.TableNextColumn();
                            var oldPos = ImGui.GetCursorPos();
                            DrawTileArt(tileInfo, TilesDimensions, TerrainMode);
                            ImGui.SetCursorPos(oldPos);
                            var selected = ObjectMode ? _selectedObjectId : _selectedTerrainId;
                            if (ImGui.Selectable($"##tile{tileIndex}", selected == tileIndex, TilesDimensions))
                            {
                                UpdateSelectedId(tileIndex);
                            }
                            Tooltip(tileInfo);
                            TilesContextMenu(tileIndex);
                            DragDropSource(tileIndex, tileInfo);
                        }
                    }
                }
                clipper.End();
                if (_updateScroll)
                {
                    float itemPosY = (float)clipper.StartPosY + TilesDimensions.Y * (Array.IndexOf(ids, SelectedId) / columns);
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    _updateScroll = false;
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void TilesContextMenu(int tileIndex)
    {
        if (ImGui.BeginPopupContextItem())
        {
            if (TileSetIndex != 0 && ImGui.Button(LangManager.Get(ADD_TO_SET)))
            {
                TileSetAddTile((ushort)tileIndex);
                ImGui.CloseCurrentPopup();
            }
            if (ObjectMode)
            {
                if (ImGui.Button(LangManager.Get(ADD_TO_FILTER)))
                {
                    CEDGame.MapManager.StaticFilterIds.Add(tileIndex);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndPopup();
        }
    }

    private void DragDropSource(int tileIndex, TileInfo tileInfo)
    {
        if (ImGui.BeginDragDropSource())
        {
            var type = ObjectMode ? OBJECT_DRAG_DROP_TYPE : TERRAIN_DRAG_DROP_TYPE;
            unsafe
            {
                ImGui.SetDragDropPayload(type, &tileIndex, sizeof(int));
            }
            ImGui.Text(tileInfo.Name);
            DrawTileArt(tileInfo);
            ImGui.EndDragDropSource();
        }
    }

    public static bool DragDropTarget(string type, out ushort id)
    {
        var res = false;
        id = 0;
        if (ImGui.BeginDragDropTarget())
        {
            var payloadPtr = ImGui.AcceptDragDropPayload(type);
            unsafe
            {
                if (payloadPtr != ImGuiPayloadPtr.Null)
                {
                    id = *(ushort*)payloadPtr.Data;
                    res = true;
                }
            }
            ImGui.EndDragDropTarget();
        }
        return res;
    }
    

    private void Tooltip(TileInfo tileInfo)
    {
        if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
        {
            if (ImGui.BeginTable($"##Tooltip{tileInfo.RealIndex}", 2, ImGuiTableFlags.BordersInner))
            {
                ImGui.TableNextColumn();
                ImGui.Text((tileInfo.RealIndex - (ObjectMode ? MAX_TERRAIN_INDEX : 0)).FormatId());
                ImGui.TextUnformatted(tileInfo.Name);
                if (ObjectMode)
                {
                    ImGui.Text($"{LangManager.Get(HEIGHT)}: {tileInfo.Height}");
                }
                ImGui.Text(tileInfo.Flags);
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
    
    private List<ushort> _tileSetTempTerrain = [];
    private List<ushort> _tileSetTempObject = [];
    private List<ushort> TileSetTemp => ObjectMode ? _tileSetTempObject : _tileSetTempTerrain;
    
    private Dictionary<string, List<ushort>> TileSets => ObjectMode ? 
        ProfileManager.ActiveProfile.StaticTileSets : 
        ProfileManager.ActiveProfile.LandTileSets;
    private string ActiveTileSetName => _tilesSetNames[TileSetIndex];
    private List<ushort> ActiveTileSet => TileSetIndex == 0 ? TileSetTemp : TileSets[ActiveTileSetName];
    
    public ushort[] ActiveTileSetValues = [];

    private void DrawTileSets()
    {
        if (ImGui.BeginChild("TileSets"))
        {
            ImGui.Text(LangManager.Get(TILE_SET));

            if (ImGui.Button(LangManager.Get(NEW)))
            {
                ImGui.OpenPopup("AddSet");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(TileSetIndex == 0);
            if (ImGui.Button(LangManager.Get(DELETE)))
            {
                ImGui.OpenPopup("DelSet");
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveTileSetValues.Length == 0);
            if (ImGui.Button(LangManager.Get(CLEAR)))
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
                if (ImGui.BeginTable("TileSetTable", 3) && CEDClient.Running)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                    var ids = ActiveTileSetValues;
                    clipper.Begin(ids.Length, TilesDimensions.Y);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            var tileIndex = ids[i];
                            var tileInfo = GetTileInfo(tileIndex);
                            ImGui.PushID(i);
                            DrawTileRow(tileIndex, tileInfo);
                            ImGui.PopID();
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Button(LangManager.Get(MOVE_UP)))
                                {
                                    TileSetMoveTile(i, i-1);
                                    ImGui.CloseCurrentPopup();
                                }
                                if (ImGui.Button(LangManager.Get(MOVE_DOWN)))
                                {
                                    TileSetMoveTile(i, i+1);
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.Separator();
                                
                                if (ImGui.Button(LangManager.Get(REMOVE)))
                                {
                                    TileSetRemoveTile(i);
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
            if (DragDropTarget(ObjectMode ? OBJECT_DRAG_DROP_TYPE : TERRAIN_DRAG_DROP_TYPE, out var tileId))
            {
                TileSetAddTile(tileId);
            }
            if (ImGui.BeginPopupModal("AddSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGuiEx.InputText(LangManager.Get(NAME), "##TileSetNewName", ref _tileSetNewName, 32);
                ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_tileSetNewName) || _tilesSetNames.Contains(_tileSetNewName));
                if (ImGui.Button(LangManager.Get(CREATE)))
                {
                    TileSets.Add(_tileSetNewName, new List<ushort>());
                    UpdateTileSetNames();
                    TileSetIndex = Array.IndexOf(_tilesSetNames, _tileSetNewName);
                    UpdateTileSetValues();
                    ProfileManager.Save();
                    _tileSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button(LangManager.Get(CANCEL)))
                {
                    _tileSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal("DelSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(string.Format(LangManager.Get(DELETE_WARNING_1TYPE_2NAME), LangManager.Get(TILE_SET).ToLower(), ActiveTileSetName));
                if (ImGui.Button(LangManager.Get(YES)))
                {
                    TileSets.Remove(ActiveTileSetName);
                    UpdateTileSetNames();
                    TileSetIndex--;
                    UpdateTileSetValues();
                    ProfileManager.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(LangManager.Get(NO)))
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
        ProfileManager.Save();
    }

    private void TileSetRemoveTile(int index)
    {
        if (index < 0 || index >= ActiveTileSet.Count)
            return;
        
        ActiveTileSet.RemoveAt(index);
        UpdateTileSetValues();
        ProfileManager.Save();
    }

    private void TileSetAddTile(ushort id)
    {
        ActiveTileSet.Add(id);
        UpdateTileSetValues();
        ProfileManager.Save();
    }

    private TileInfo GetTileInfo(int index)
    {
        return ObjectMode ? GetObjectInfo(index) : GetTerrainInfo(index);
    }

    private TileInfo GetTerrainInfo(int index)
    {
        if (index > MAX_TERRAIN_INDEX)
        {
            if(CEDGame.MapManager.DebugLogging)
                Console.WriteLine($"Requested invalid terrain info for index {index.FormatId()}");
            
            return TileInfo.INVALID;
        }
        
        SpriteInfo spriteInfo;
        if (_texMode)
        {
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(CEDGame.MapManager.UoFileManager.TileData.LandData[index].TexID);
        }
        else
        {
            var isArtValid = CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(index).Length > 0;
            var artIndex = isArtValid ? (uint)index : 0;
            spriteInfo = CEDGame.MapManager.Arts.GetLand(artIndex);
        }
        var name = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Name;
        var flags = CEDGame.MapManager.UoFileManager.TileData.LandData[index].Flags.ToString().Replace(", ", "\n");

        return new TileInfo(index, spriteInfo.Texture, spriteInfo.UV, name, flags, 0);
    }

    private TileInfo GetObjectInfo(int index)
    {
        var realIndex = index + MAX_TERRAIN_INDEX;
        if (CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(realIndex).Length < 0)
        {
            return TileInfo.INVALID;
        }
        ref var indexEntry = ref CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(index + 0x4000);

        var spriteInfo = CEDGame.MapManager.Arts.GetArt((uint)(index + indexEntry.AnimOffset));
        var realBounds = CEDGame.MapManager.Arts.GetRealArtBounds((uint)(index + indexEntry.AnimOffset));
        var name = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Name;
        var flags = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Flags.ToString().Replace(", ", "\n");

        return new TileInfo
        (
            realIndex,
            spriteInfo.Texture,
            new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height),
            name,
            flags,
            CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Height
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
            if (!CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, size, stretch) &&
                CEDGame.MapManager.DebugLogging)
            {
                Console.WriteLine($"[TilesWindow] No texture found for tile {tileInfo.RealIndex.FormatId()}");
            }
        }
    }

    private void DrawTileRow(int index, TileInfo tileInfo)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2); //center vertically
        ImGui.Text($"{index.FormatId()}");

        ImGui.TableNextColumn();
        DrawTileArt(tileInfo, TilesDimensions, TerrainMode);
        
        ImGui.TableNextColumn();
        var selected = ObjectMode ? _selectedObjectId : _selectedTerrainId;
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
        if (ImGui.Selectable(tileInfo.Name, selected == index, SpanAllColumns, TilesDimensions with { X = 0 }))
        {
            UpdateSelectedId(index);
        }
        ImGui.PopStyleVar();
    }

    public void UpdateSelectedId(TileObject mapObject)
    {
        if (mapObject is StaticObject)
        {
            _selectedObjectId = mapObject.Tile.Id;
            _objectMode = true;
        }
        else if (mapObject is LandObject)
        {
            _selectedTerrainId = mapObject.Tile.Id;
            _objectMode = false;
        }
        _updateScroll = true;
    }

    public void UpdateSelectedId(int id)
    {
        if (ObjectMode)
            _selectedObjectId = id;
        else
            _selectedTerrainId = id;
    }

    private void UpdateTileSetNames()
    {
        _tilesSetNames = TileSets.Keys.Prepend("").ToArray();
    }
    
    private void UpdateTileSetValues()
    {
        ActiveTileSetValues = ActiveTileSet.ToArray();
    }
    
    private void ClearTileSet()
    {
        ActiveTileSet.Clear();
        UpdateTileSetValues();
        ProfileManager.Save();
    }
    
    private bool _tiledataFilterEnabled;
    private bool _tiledataFilterInclusive = true;
    private bool _tiledataFilterMatchAll;
    private ulong _tiledataFilterValue;

    private bool _recalculateTextWidth;
    private int _maxTiledataTextWidth;
    
    private static readonly TileFlag[] TiledataFilterFlags = Enum.GetValues<TileFlag>().Where(f => f != TileFlag.None).ToArray();
    private static readonly ulong TiledataFilterAllValue = TiledataFilterFlags.Aggregate(0ul, (a, b) => a | (ulong)b);
    
    private void DrawTiledataFilter()
    {
        if (ImGui.BeginChild("TiledataFilter", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.Button(LangManager.Get(CHECK_ALL)))
            {
                _tiledataFilterValue = TiledataFilterAllValue;
            }
            ImGui.SameLine();
            if (ImGui.Button(LangManager.Get(UNCHECK_ALL)))
            {
                _tiledataFilterValue = 0ul;
            }
            ImGui.SameLine();
            ImGui.Checkbox(LangManager.Get(INCLUSIVE), ref _tiledataFilterInclusive);
            ImGui.SameLine();
            ImGui.Checkbox(LangManager.Get(MATCH_ALL), ref _tiledataFilterMatchAll);

            var columns = (int)(ImGui.GetContentRegionAvail().X / (_maxTiledataTextWidth + ImGui.GetStyle().ItemSpacing.X));
            var rows = TiledataFilterFlags.Length / columns + 1;
            if (ImGui.BeginTable("TiledataFilterTable", columns) && CEDClient.Running)
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
                            var enabled = (_tiledataFilterValue & flagValue) > 0;
                            //TODO: Allocate all the flags names once
                            if (ImGui.Checkbox(flag.ToString(), ref enabled))
                            {
                                _tiledataFilterValue ^= flagValue;
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