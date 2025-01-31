using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class TilesWindow : Window
{
    record struct TileInfo(int RealIndex, Texture2D Texture, Rectangle Bounds, string Name, string Flags)
    {
        public static TileInfo INVALID = new(-1, null, default, "", "");
    };
    private static readonly Random _random = new();

    public TilesWindow()
    {
        for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
        {
            tileDataFiltersCheckBoxes[i] = true;
        }

        foreach (var flag in tileDataFilters)
        {
            allFlags |= (ulong)flag;
        }

        CEDClient.Connected += FilterTiles;
    }

    public override string Name => "Tiles";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    private string _filter = "";
    internal int SelectedLandId;
    internal int SelectedStaticId;
    public bool UpdateScroll;
    private bool staticMode;
    private float _tableWidth;
    public const int MaxLandIndex = ArtLoader.MAX_LAND_DATA_INDEX_COUNT;
    private static readonly Vector2 TilesDimensions = new(44, 44);
    private static readonly float TotalRowHeight = TilesDimensions.Y + ImGui.GetStyle().ItemSpacing.Y;
    public const string Static_DragDrop_Target_Type = "StaticDragDrop";
    public const string Land_DragDrop_Target_Type = "LandDragDrop";
    private bool gridMode = false;
    private bool texMode = false;

    private int[] _matchedLandIds;
    private int[] _matchedStaticIds;

    public bool LandMode => !staticMode;
    public bool StaticMode
    {
        get => staticMode;
        set => staticMode = value;
    }

    public ushort SelectedId => (ushort)(_tileSetSelectedId > 0 ? _tileSetSelectedId : (LandMode ? SelectedLandId : SelectedStaticId));

    public ushort ActiveId =>
        ActiveTileSetValues.Length > 0 && CEDGame.MapManager.UseRandomTileSet ? ActiveTileSetValues[_random.Next(ActiveTileSetValues.Length)] : SelectedId;

    private static readonly TileDataFlag[] tileDataFilters = [
            TileDataFlag.None,
            TileDataFlag.Background  ,
            TileDataFlag.Translucent ,
            TileDataFlag.Wall        ,
            TileDataFlag.Damaging    ,
            TileDataFlag.Impassable  ,
            TileDataFlag.Wet         ,
            TileDataFlag.Surface     ,
            TileDataFlag.Bridge      ,
            TileDataFlag.Generic     ,
            TileDataFlag.Window      ,
            TileDataFlag.NoShoot     ,
            TileDataFlag.Foliage     ,
            TileDataFlag.PartialHue  ,
            TileDataFlag.Container   ,
            TileDataFlag.Wearable    ,
            TileDataFlag.LightSource ,
            TileDataFlag.Animation   ,
            TileDataFlag.Roof        ,
            TileDataFlag.Door        ,
            TileDataFlag.StairBack   ,
            TileDataFlag.StairRight
            ];

    ulong allFlags = 0;

    private readonly bool[] tileDataFiltersCheckBoxes = new bool[tileDataFilters.Length];
    private bool tileDataFilterOn = false, tileDataFilterOthers = true, tileDataFilterAlt = true;

    private void FilterTiles()
    {
        if (_filter.Length == 0 && (!tileDataFilterOn || !tileDataFiltersCheckBoxes.Contains(false)))
        {
            _matchedLandIds = new int[CEDGame.MapManager.ValidLandIds.Length];
            CEDGame.MapManager.ValidLandIds.CopyTo(_matchedLandIds, 0);

            _matchedStaticIds = new int[CEDGame.MapManager.ValidStaticIds.Length];
            CEDGame.MapManager.ValidStaticIds.CopyTo(_matchedStaticIds, 0);
        }
        else
        {
            var filter = _filter.ToLower();
            var matchedLandIds = new List<int>();
            foreach (var index in CEDGame.MapManager.ValidLandIds)
            {
                bool toAdd = false;

                var name = TileDataLoader.Instance.LandData[index].Name?.ToLower() ?? "";
                if (_filter.Length == 0 || name.Contains(filter) || $"{index}".Contains(_filter) || $"0x{index:x4}".Contains(filter))
                    toAdd = true;

                if (toAdd && tileDataFilterOn)
                {
                    TileFlag landFlags = TileDataLoader.Instance.LandData[index].Flags;

                    toAdd = tileDataFilterAlt;
                    for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                    {
                        if (tileDataFiltersCheckBoxes[i] == !tileDataFilterAlt && (((ulong)landFlags & (ulong)tileDataFilters[i]) != 0))
                        {
                            toAdd = !tileDataFilterAlt;
                            break;
                        }
                    }

                    //Other flags
                    if (landFlags > 0 && ((ulong)landFlags & allFlags) == 0)
                    {
                        toAdd = tileDataFilterOthers;
                    }

                    //None flag
                    if (landFlags == 0)
                    {
                        toAdd = tileDataFiltersCheckBoxes[0];
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

                var name = TileDataLoader.Instance.StaticData[index].Name?.ToLower() ?? "";
                if (_filter.Length == 0 || name.Contains(filter) || $"{index}".Contains(_filter) || $"0x{index:x4}".Contains(filter))
                    toAdd = true;

                if (toAdd && tileDataFilterOn)
                {
                    TileFlag staticFlags = TileDataLoader.Instance.StaticData[index].Flags;

                    toAdd = tileDataFilterAlt;
                    for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
                    {
                        if (tileDataFiltersCheckBoxes[i] == !tileDataFilterAlt && (((ulong)staticFlags & (ulong)tileDataFilters[i]) != 0))
                        {
                            toAdd = !tileDataFilterAlt;
                            break;
                        }
                    }

                    //Other flags
                    if (staticFlags > 0 && ((ulong)staticFlags & allFlags) == 0)
                    {
                        toAdd = tileDataFilterOthers;
                    }

                    //None flag
                    if (staticFlags == 0)
                    {
                        toAdd = tileDataFiltersCheckBoxes[0];
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
        if (!CEDGame.MapManager.Client.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        if (ImGui.Button("Scroll to selected"))
        {
            UpdateScroll = true;
        }
        ImGui.Text("Filter");
        ImGui.InputText("", ref _filter, 64);

        if (UIManager.TwoWaySwitch("Land", "Statics", ref staticMode))
        {
            UpdateScroll = true;
            _tileSetIndex = 0;
            ActiveTileSetValues = Empty;
            _tileSetSelectedId = 0;
        }
        if (UIManager.TwoWaySwitch("List", "Grid", ref gridMode))
        {
            UpdateScroll = true;
        }
        if (LandMode)
        {
            UIManager.TwoWaySwitch(" Art", "Tex", ref texMode);
        }
        ImGui.Text("Tiledata Filter");
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
        if (gridMode)
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
        ImGui.BeginChild("Tiles", new Vector2(), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeY);
        if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Initialized)
        {
            unsafe
            {
                ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
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
                                LandMode ? SelectedLandId == tileIndex : SelectedStaticId == tileIndex,
                                ImGuiSelectableFlags.SpanAllColumns,
                                new Vector2(0, TilesDimensions.Y)
                            ))
                        {
                            if (LandMode)
                                SelectedLandId = tileIndex;
                            else
                                SelectedStaticId = tileIndex;
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
                            if(StaticMode)
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
                                LandMode ? Land_DragDrop_Target_Type : Static_DragDrop_Target_Type,
                                (IntPtr)(&tileIndex),
                                sizeof(int)
                            );
                            ImGui.Text(tileInfo.Name);
                            CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
                            ImGui.EndDragDropSource();
                        }
                    }
                }
                clipper.End();
                if (UpdateScroll)
                {
                    float itemPosY = clipper.StartPosY + TotalRowHeight * Array.IndexOf
                        (ids, LandMode ? SelectedLandId : SelectedStaticId);
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    UpdateScroll = false;
                }
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    private void DrawTilesGrid()
    {
        ImGui.BeginChild("Tiles", new Vector2(), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeY);
        _tableWidth = ImGui.GetContentRegionAvail().X;
        int columnsNumber = (int)(_tableWidth / (TilesDimensions.X + ImGui.GetStyle().ItemSpacing.X));
        if (columnsNumber < 4)
        {
            columnsNumber = 4;
        }
        if (ImGui.BeginTable("TilesTable", columnsNumber) && CEDClient.Initialized)
        {
            unsafe
            {
                ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
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
                                    ImGui.GetWindowDrawList().AddRect(ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(UIManager.Pink));
                                }
                                else
                                {
                                    if (!CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, TilesDimensions, LandMode) 
                                        && CEDGame.MapManager.DebugLogging)
                                    {
                                        Console.WriteLine($"[TilesWindow] No texture found for tile 0x{tileIndex:X4}");
                                    }
                                }
                                ImGui.SetCursorPos(oldPos);
                            }
                            if (ImGui.Selectable
                                (
                                    $"##tile{tileInfo.RealIndex}",
                                    LandMode ? SelectedLandId == tileIndex : SelectedStaticId == tileIndex,
                                    ImGuiSelectableFlags.None,
                                    new Vector2(TilesDimensions.X, TilesDimensions.Y)
                                ))
                            {
                                if (LandMode)
                                    SelectedLandId = tileIndex;
                                else
                                    SelectedStaticId = tileIndex;
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
                                    LandMode ? Land_DragDrop_Target_Type : Static_DragDrop_Target_Type,
                                    (IntPtr)(&tileIndex),
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
                if (UpdateScroll)
                {
                    float itemPosY = clipper.StartPosY + TotalRowHeight * (Array.IndexOf(ids, LandMode ? SelectedLandId : SelectedStaticId) / columnsNumber);
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    UpdateScroll = false;
                }
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    private void DrawTooltip(TileInfo tileInfo)
    {
        if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
        {
            CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text($"0x{tileInfo.RealIndex:X4}");
            ImGui.TextUnformatted(tileInfo.Name);
            ImGui.Separator();
            ImGui.Text(tileInfo.Flags);
            ImGui.EndGroup();
            ImGui.EndTooltip();
        }
    }

    private int _tileSetIndex;
    private string _tileSetName;
    private ushort _tileSetSelectedId;
    private bool _tileSetShowPopupNew;
    private bool _tileSetShowPopupDelete;
    private string _tileSetNewName = "";
    private static readonly ushort[] Empty = Array.Empty<ushort>();
    public ushort[] ActiveTileSetValues = Empty;

    private void DrawTileSets()
    {
        ImGui.BeginChild("TileSets");
        ImGui.Text("Tile Set");
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
        //Probably slow, optimize
        var names = new[] { String.Empty }.Concat(tileSets.Keys).ToArray();
        if (ImGui.Combo("", ref _tileSetIndex, names, names.Length))
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
        ImGui.BeginChild("TileSetTable");
        if (ImGui.BeginTable("TileSetTable", 3) && CEDClient.Initialized)
        {
            unsafe
            {
                ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, TilesDimensions.X);
                _tableWidth = ImGui.GetContentRegionAvail().X;
                var ids = ActiveTileSetValues; //We copy the array here to not crash when removing, please fix :)
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
                                $"##tileset{tileInfo.RealIndex}",
                               _tileSetSelectedId == tileIndex,
                                ImGuiSelectableFlags.SpanAllColumns,
                                new Vector2(0, TilesDimensions.Y)
                            ))
                        {
                            _tileSetSelectedId = tileIndex;
                        }
                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGui.Button("Remove"))
                            {
                                RemoveFromTileSet(tileIndex);
                                ImGui.CloseCurrentPopup();
                            }
                            ImGui.EndPopup();
                        }
                    }
                }
                clipper.End();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
        if (_tileSetIndex != 0 && ImGui.BeginDragDropTarget())
        {
            var payloadPtr = ImGui.AcceptDragDropPayload
                (LandMode ? Land_DragDrop_Target_Type : Static_DragDrop_Target_Type);
            unsafe
            {
                if (payloadPtr.NativePtr != null)
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
            ImGui.Text("Name");
            ImGui.SameLine();
            ImGui.InputText("", ref _tileSetNewName, 32);
            if (ImGui.Button("Add"))
            {
                tileSets.Add(_tileSetNewName, new SortedSet<ushort>());
                _tileSetIndex = Array.IndexOf(tileSets.Keys.ToArray(), _tileSetNewName) + 1;
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
                tileSets.Remove(_tileSetName);
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
        ImGui.EndChild();
    }

    private void AddToTileSet(ushort id)
    {
        var tileSets = LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;
        var tileSet = tileSets[_tileSetName];
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
        if (ArtLoader.Instance.GetValidRefEntry(index).Length < 0)
        {
            return TileInfo.INVALID;
        }
        SpriteInfo spriteInfo;
        if (texMode)
        {
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(TileDataLoader.Instance.LandData[index].TexID);
        }
        else
        {
            spriteInfo = CEDGame.MapManager.Arts.GetLand((uint)index);
        }
        var name = TileDataLoader.Instance.LandData[index].Name;
        var flags = TileDataLoader.Instance.LandData[index].Flags.ToString().Replace(", ", "\n");

        return new(index, spriteInfo.Texture, spriteInfo.UV, name, flags);
    }

    private TileInfo StaticInfo(int index)
    {
        var realIndex = index + MaxLandIndex;
        if (ArtLoader.Instance.GetValidRefEntry(realIndex).Length < 0)
        {
            return TileInfo.INVALID;
        }
        ref var indexEntry = ref ArtLoader.Instance.GetValidRefEntry(index + 0x4000);
        
        var spriteInfo = CEDGame.MapManager.Arts.GetArt((uint)(index + indexEntry.AnimOffset));
        var realBounds = CEDGame.MapManager.Arts.GetRealArtBounds((uint)index);
        var name = TileDataLoader.Instance.StaticData[index].Name;
        var flags = TileDataLoader.Instance.StaticData[index].Flags.ToString().Replace(", ", "\n");

        return new
        (
            realIndex,
            spriteInfo.Texture,
            new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height),
            name,
            flags
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
               ImGui.GetWindowDrawList().AddRect(ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(UIManager.Pink));
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
            SelectedStaticId = mapObject.Tile.Id;
            StaticMode = true;
        }
        else if (mapObject is LandObject)
        {
            SelectedLandId = mapObject.Tile.Id;
            StaticMode = false;
        }
        UpdateScroll = true;
    }

    private void DrawTiledataFilter()
    {        
        ImGui.BeginChild("TiledataFilter", new Vector2(), ImGuiChildFlags.Border | ImGuiChildFlags.ResizeY);

        if (ImGui.Button("Check All"))
        {
            for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
            {
                tileDataFiltersCheckBoxes[i] = true;
            }
            tileDataFilterOthers = true;
        }
        ImGui.SameLine();
        if (ImGui.Button("Uncheck All"))
        {
            for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
            {
                tileDataFiltersCheckBoxes[i] = false;
            }
            tileDataFilterOthers = false;
        }
        ImGui.SameLine();
        ImGui.Checkbox("Alternative Mode", ref tileDataFilterAlt);

        int checkboxWidth = 120;

        int columnsNumber = (int)(_tableWidth / (checkboxWidth + ImGui.GetStyle().ItemSpacing.X));
        if (columnsNumber < 4)
        {
            columnsNumber = 4;
        }

        if (ImGui.BeginTable("TiledataFilterTable", columnsNumber) && CEDClient.Initialized)
        {
            for (int i = 0; i < columnsNumber; i++)
            {
                ImGui.TableSetupColumn("col"+i, ImGuiTableColumnFlags.WidthFixed, checkboxWidth);
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
                        else
                        {
                            ImGui.Checkbox("Others", ref tileDataFilterOthers);
                            break;
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
    }
}