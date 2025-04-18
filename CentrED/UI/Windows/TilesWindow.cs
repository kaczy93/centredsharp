using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class TilesWindow : Window
{
    record struct TileInfo(int RealIndex, Texture2D Texture, Rectangle Bounds, string Name, string Flags, uint Height)
    {
        public static TileInfo INVALID = new(-1, null, default, "", "", 0);
    };
    private static readonly Random _random = new();
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };    
    private static string _excludedTilesFilePath = "excludeTiles.json";
    private int[] _excludedTiles = [];

    public TilesWindow()
    {
        for (int i = 0; i < tileDataFiltersCheckBoxes.Length; i++)
        {
            tileDataFiltersCheckBoxes[i] = true;
        }

        CEDClient.Connected += FilterTiles;

        List<int> excludedTiles = [];

        if (!File.Exists(_excludedTilesFilePath))
        {
            //Galleons pieces
        
            //Gargoyle
            excludedTiles.AddRange(Enumerable.Range(0x49FC, 0x4A8D + 1 - 0x49FC));
            excludedTiles.AddRange(Enumerable.Range(0x4AB9, 0x4AF4 + 1 - 0x4AB9));
            //Britannian
            excludedTiles.AddRange(Enumerable.Range(0x5780, 0x5BB4 + 1 - 0x5780));
            excludedTiles.AddRange(Enumerable.Range(0x5BB6, 0x5BD9 + 1 - 0x5BB6));
            excludedTiles.AddRange(Enumerable.Range(0x5BE1, 0x5C0F + 1 - 0x5BE1));
            excludedTiles.AddRange(Enumerable.Range(0x5C18, 0x5C45 + 1 - 0x5C18));
            excludedTiles.AddRange(Enumerable.Range(0x5C4E, 0x5C7B + 1 - 0x5C4E));
            excludedTiles.AddRange(Enumerable.Range(0x5C84, 0x5D1E + 1 - 0x5C84));
            excludedTiles.AddRange(Enumerable.Range(0x5D20, 0x5D28 + 1 - 0x5D20));
            excludedTiles.AddRange(Enumerable.Range(0x5D2A, 0x61D9 + 1 - 0x5D2A));
            excludedTiles.AddRange(Enumerable.Range(0x61E2, 0x620F + 1 - 0x61E2));
            excludedTiles.AddRange(Enumerable.Range(0x6218, 0x6245 + 1 - 0x6218));
            excludedTiles.AddRange(Enumerable.Range(0x624E, 0x627B + 1 - 0x624E));
            excludedTiles.AddRange(Enumerable.Range(0x6284, 0x62B2 + 1 - 0x6284));
            excludedTiles.AddRange(Enumerable.Range(0x62B4, 0x62BC + 1 - 0x62B4));
            excludedTiles.AddRange(Enumerable.Range(0x62BE, 0x62E8 + 1 - 0x62BE));
            excludedTiles.AddRange(Enumerable.Range(0x62EA, 0x62F2 + 1 - 0x62EA));
            excludedTiles.AddRange(Enumerable.Range(0x62F4, 0x631E + 1 - 0x62F4));
            excludedTiles.AddRange(Enumerable.Range(0x6320, 0x6328 + 1 - 0x6320));
            excludedTiles.AddRange(Enumerable.Range(0x632A, 0x6354 + 1 - 0x632A));
            excludedTiles.AddRange(Enumerable.Range(0x6356, 0x635E + 1 - 0x6356));
            excludedTiles.AddRange(Enumerable.Range(0x6360, 0x637F + 1 - 0x6360));
            //Orc
            excludedTiles.AddRange(Enumerable.Range(0x7530, 0x75BE + 1 - 0x7530));
            excludedTiles.AddRange(Enumerable.Range(0x75C6, 0x75F1 + 1 - 0x75C6));
            excludedTiles.AddRange(Enumerable.Range(0x7724, 0x7724 + 1 - 0x7724));
            excludedTiles.AddRange(Enumerable.Range(0x77BA, 0x77E5 + 1 - 0x77BA));
            excludedTiles.AddRange(Enumerable.Range(0x7918, 0x79A6 + 1 - 0x7918));
            excludedTiles.AddRange(Enumerable.Range(0x79AE, 0x79D9 + 1 - 0x79AE));
            excludedTiles.AddRange(Enumerable.Range(0x7B0C, 0x7B97 + 1 - 0x7B0C));
            excludedTiles.AddRange(Enumerable.Range(0x7B99, 0x7B9B + 1 - 0x7B99));
            excludedTiles.AddRange(Enumerable.Range(0x7BA2, 0x7BCD + 1 - 0x7BA2));
            excludedTiles.AddRange(Enumerable.Range(0x7BD4, 0x7C8B + 1 - 0x7BD4));
            excludedTiles.AddRange(Enumerable.Range(0x7C92, 0x7C94 + 1 - 0x7C92));
            excludedTiles.AddRange(Enumerable.Range(0x7C9C, 0x7D53 + 1 - 0x7C9C));
            excludedTiles.AddRange(0x7D5A, 0x7D5B, 0x7D5D);
            excludedTiles.AddRange(Enumerable.Range(0x7D64, 0x7E1B + 1 - 0x7D64));
            excludedTiles.AddRange(0x7E22, 0x7E23, 0x7E25);
            excludedTiles.AddRange(Enumerable.Range(0x7E2C, 0x7EE3 + 1 - 0x7E2C));
            excludedTiles.AddRange(Enumerable.Range(0x7EEB, 0x7EED + 1 - 0x7EEB));
            excludedTiles.AddRange(Enumerable.Range(0x7EF4, 0x7F78 + 1 - 0x7EF4));
            excludedTiles.AddRange(Enumerable.Range(0x7F7A, 0x7FAB + 1 - 0x7F7A));
            excludedTiles.AddRange(Enumerable.Range(0x7FB3, 0x7FB5 + 1 - 0x7FB3));
            excludedTiles.AddRange(Enumerable.Range(0x7FBC, 0x8040 + 1 - 0x7FBC));
            excludedTiles.AddRange(Enumerable.Range(0x8042, 0x8073 + 1 - 0x8042));
            excludedTiles.AddRange(Enumerable.Range(0x807B, 0x807D + 1 - 0x807B));
            excludedTiles.AddRange(Enumerable.Range(0x8084, 0x8108 + 1 - 0x8084));
            excludedTiles.AddRange(Enumerable.Range(0x810A, 0x813D + 1 - 0x810A));
            excludedTiles.AddRange(0x8142, 0x8144, 0x8145);
            excludedTiles.AddRange(Enumerable.Range(0x814C, 0x81D0 + 1 - 0x814C));
            excludedTiles.AddRange(Enumerable.Range(0x81D2, 0x81F4 + 1 - 0x81D2));
            excludedTiles.AddRange(Enumerable.Range(0x81F6, 0x8203 + 1 - 0x81F6));
            excludedTiles.AddRange(0x820A, 0x820B, 0x820D);
            //Gargoyle
            excludedTiles.AddRange(Enumerable.Range(0x8214, 0x82F1 + 1 - 0x8214));
            excludedTiles.AddRange(Enumerable.Range(0x82F5, 0x8342 + 1 - 0x82F5));
            excludedTiles.AddRange(Enumerable.Range(0x8344, 0x8470 + 1 - 0x8344));
            excludedTiles.AddRange(Enumerable.Range(0x8472, 0x859E + 1 - 0x8472));
            excludedTiles.AddRange(0x85A0);
            excludedTiles.AddRange(Enumerable.Range(0x85A2, 0x86E8 + 1 - 0x85A2));
            excludedTiles.AddRange(Enumerable.Range(0x86EA, 0x86FA + 1 - 0x86EA));
            excludedTiles.AddRange(Enumerable.Range(0x86FE, 0x87AB + 1 - 0x86FE));
            excludedTiles.AddRange(Enumerable.Range(0x87AD, 0x87DF + 1 - 0x87AD));
            excludedTiles.AddRange(Enumerable.Range(0x87E1, 0x87F1 + 1 - 0x87E1));
            excludedTiles.AddRange(Enumerable.Range(0x87F3, 0x87FA + 1 - 0x87F3));
            excludedTiles.AddRange(0x87FC);
            excludedTiles.AddRange(Enumerable.Range(0x87FF, 0x8818 + 1 - 0x87FF));
            excludedTiles.AddRange(Enumerable.Range(0x881A, 0x882A + 1 - 0x881A));
            excludedTiles.AddRange(Enumerable.Range(0x882E, 0x88CF + 1 - 0x882E));
            excludedTiles.AddRange(Enumerable.Range(0x88D1, 0x88DB + 1 - 0x88D1));
            excludedTiles.AddRange(Enumerable.Range(0x88DD, 0x8A09 + 1 - 0x88DD));
            excludedTiles.AddRange(Enumerable.Range(0x8A0B, 0x8A4F + 1 - 0x8A0B));
            excludedTiles.AddRange(Enumerable.Range(0x8A51, 0x8A58 + 1 - 0x8A51));
            excludedTiles.AddRange(Enumerable.Range(0x8A5A, 0x8A74 + 1 - 0x8A5A));
            excludedTiles.AddRange(Enumerable.Range(0x8A76, 0x8A86 + 1 - 0x8A76));
            excludedTiles.AddRange(Enumerable.Range(0x8A8A, 0x8B8A + 1 - 0x8A8A));
            excludedTiles.AddRange(0x8B8E,0x8B91,0x8B92);
            excludedTiles.AddRange(Enumerable.Range(0x8B94, 0x8B9F + 1 - 0x8B94));
            excludedTiles.AddRange(Enumerable.Range(0x8BA2, 0x8BB2 + 1 - 0x8BA2));
            excludedTiles.AddRange(Enumerable.Range(0x8BB6, 0x8C09 + 1 - 0x8BB6));
            excludedTiles.AddRange(Enumerable.Range(0x8C0C, 0x8C57 + 1 - 0x8C0C));
            excludedTiles.AddRange(Enumerable.Range(0x8C59, 0x8C60 + 1 - 0x8C59));
            excludedTiles.AddRange(Enumerable.Range(0x8C6D, 0x8C8B + 1 - 0x8C6D));
            excludedTiles.AddRange(Enumerable.Range(0x8C8D, 0x8C8E + 1 - 0x8C8D));
            excludedTiles.AddRange(Enumerable.Range(0x8C91, 0x8C92 + 1 - 0x8C91));
            excludedTiles.AddRange(Enumerable.Range(0x8C95, 0x8C97 + 1 - 0x8C95));
            excludedTiles.AddRange(Enumerable.Range(0x8C99, 0x8C9B + 1 - 0x8C99));
            excludedTiles.AddRange(Enumerable.Range(0x8C9D, 0x8CA7 + 1 - 0x8C9D));
            excludedTiles.AddRange(Enumerable.Range(0x8CB4, 0x8D85 + 1 - 0x8CB4));
            excludedTiles.AddRange(Enumerable.Range(0x8D87, 0x8D8E + 1 - 0x8D87));
            excludedTiles.AddRange(Enumerable.Range(0x8DA0, 0x8DBD + 1 - 0x8DA0));
            excludedTiles.AddRange(Enumerable.Range(0x8DBF, 0x8DC0 + 1 - 0x8DBF));
            excludedTiles.AddRange(Enumerable.Range(0x8DC3, 0x8DC5 + 1 - 0x8DC3));
            excludedTiles.AddRange(Enumerable.Range(0x8DC7, 0x8DC9 + 1 - 0x8DC7));
            excludedTiles.AddRange(Enumerable.Range(0x8DCB, 0x8DD5 + 1 - 0x8DCB));
            excludedTiles.AddRange(Enumerable.Range(0x8DE2, 0x8EB3 + 1 - 0x8DE2));
            excludedTiles.AddRange(Enumerable.Range(0x8EB5, 0x8EBC + 1 - 0x8EB5));
            excludedTiles.AddRange(Enumerable.Range(0x8ECA, 0x8EE7 + 1 - 0x8ECA));
            excludedTiles.AddRange(Enumerable.Range(0x8EE9, 0x8EEA + 1 - 0x8EE9));
            excludedTiles.AddRange(Enumerable.Range(0x8EED, 0x8EEE + 1 - 0x8EED));
            excludedTiles.AddRange(Enumerable.Range(0x8EF1, 0x8EF3 + 1 - 0x8EF1));
            excludedTiles.AddRange(Enumerable.Range(0x8EF5, 0x8EF7 + 1 - 0x8EF5));
            excludedTiles.AddRange(Enumerable.Range(0x8EF9, 0x8F03 + 1 - 0x8EF9));
            excludedTiles.AddRange(0x8F0F);
            excludedTiles.AddRange(Enumerable.Range(0x8F11, 0x8FE1 +1 - 0x8F11));
            excludedTiles.AddRange(Enumerable.Range(0x8FE3, 0x8FEA +1 - 0x8FE3));
            excludedTiles.AddRange(Enumerable.Range(0x8FF6, 0x9013 +1 - 0x8FF6));
            excludedTiles.AddRange(Enumerable.Range(0x9015, 0x9016 +1 - 0x9015));
            excludedTiles.AddRange(Enumerable.Range(0x9019, 0x901A +1 - 0x9019));
            excludedTiles.AddRange(Enumerable.Range(0x901D, 0x901F +1 - 0x901D));
            excludedTiles.AddRange(Enumerable.Range(0x9021, 0x9023 +1 - 0x9021));
            excludedTiles.AddRange(Enumerable.Range(0x9025, 0x902F + 1 - 0x9025));
            excludedTiles.AddRange(0x903B);
            //Tokuno
            excludedTiles.AddRange(Enumerable.Range(0x903D, 0x95F4 + 1 - 0x903D));
            excludedTiles.AddRange(Enumerable.Range(0x95F6, 0x9703 + 1 - 0x95F6));
            excludedTiles.AddRange(Enumerable.Range(0x9709, 0x978F + 1 - 0x9709));
            excludedTiles.AddRange(Enumerable.Range(0x9795, 0x981B + 1 - 0x9795));
            excludedTiles.AddRange(Enumerable.Range(0x9853, 0x9880 + 1 - 0x9853));
            excludedTiles.AddRange(Enumerable.Range(0x9885, 0x98B2 + 1 - 0x9885));
            excludedTiles.AddRange(Enumerable.Range(0x98B7, 0x98E4 + 1 - 0x98B7));
            excludedTiles.AddRange(Enumerable.Range(0x98E9, 0x98F0 + 1 - 0x98E9));
            //Gargoyle?
            excludedTiles.AddRange(Enumerable.Range(0x98F1, 0x992C + 1 - 0x98F1));
        
            File.WriteAllText(_excludedTilesFilePath, JsonSerializer.Serialize(excludedTiles));
        }
        
        var jsonText = File.ReadAllText(_excludedTilesFilePath);
        excludedTiles = JsonSerializer.Deserialize<List<int>>(jsonText, SerializerOptions);
        
        if (excludedTiles != null)
        { 
            _excludedTiles = excludedTiles.ToArray();
        }
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
    private bool excludeTiles = true;

    private int[] _matchedLandIds;
    private int[] _matchedStaticIds;

    public bool LandMode => !staticMode;
    public bool StaticMode
    {
        get => staticMode;
        set => staticMode = value;
    }

    public ushort SelectedId => (ushort)(_tileSetSelectedId > 0 ? _tileSetSelectedId : (LandMode ? SelectedLandId : SelectedStaticId));

    public ushort ActiveId
    {
        get
        {
            if (ActiveTileSetValues.Length == 0)
                return SelectedId;

            if (CEDGame.MapManager.UseRandomTileSet)
                return ActiveTileSetValues[_random.Next(ActiveTileSetValues.Length)];

            if (CEDGame.MapManager.UseSequentialTileSet)
            {
                // For preview, always show the first tile in the set
                return ActiveTileSetValues[0];
            }

            return SelectedId;
        }
    }

    public void ResetTileSetSelection()
    {
        // Reset to empty selection
        _tileSetIndex = 0;
        _tileSetName = string.Empty;
        _tileSetSelectedId = 0;
        ActiveTileSetValues = Empty;
    }

    // This method only advances when actually placing a tile
    public ushort GetNextSequentialId()
    {
        if (ActiveTileSetValues.Length == 0)
            return SelectedId;

        if (!CEDGame.MapManager.UseSequentialTileSet)
            return ActiveId;

        // Get the current tile ID
        ushort tileId = ActiveTileSetValues[CEDGame.MapManager._currentSequenceIndex];

        // Advance to next position for next call
        CEDGame.MapManager._currentSequenceIndex = (CEDGame.MapManager._currentSequenceIndex + 1) % ActiveTileSetValues.Length;

        return tileId;
    }

    private static readonly TileDataFlag[] tileDataFilters = Enum.GetValues<TileDataFlag>().Cast<TileDataFlag>().ToArray();

    private readonly bool[] tileDataFiltersCheckBoxes = new bool[tileDataFilters.Length];
    private bool tileDataFilterOn = false, tileDataFilterInclusive = true, tileDataFilterMatchAll = false;

    private void FilterTiles()
    {
        if (_filter.Length == 0 && !tileDataFilterOn)
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

                var name = TileDataLoader.Instance.StaticData[index].Name?.ToLower() ?? "";
                if (_filter.Length == 0 || name.Contains(filter) || $"{index}".Contains(_filter) || $"0x{index:x4}".Contains(filter))
                    toAdd = true;

                if (toAdd && tileDataFilterOn)
                {
                    TileFlag staticFlags = TileDataLoader.Instance.StaticData[index].Flags;

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

                if (toAdd && excludeTiles && _excludedTiles.Contains(index))
                {
                    toAdd = false;
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
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        if (ImGui.Button("Scroll to selected"))
        {
            UpdateScroll = true;
        }
        ImGui.Text("Filter");
        ImGui.InputText("##Filter", ref _filter, 64);

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
        if (staticMode)
        {
            ImGui.SameLine();
            ImGui.Checkbox("Exclude Statics", ref excludeTiles);
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
        if (ImGui.BeginChild("Tiles", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Initialized)
            {
                unsafe
                {
                    ImGuiListClipperPtr clipper = new ImGuiListClipperPtr
                        (ImGuiNative.ImGuiListClipper_ImGuiListClipper());
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
            if (ImGui.BeginTable("TilesTable", columnsNumber) && CEDClient.Initialized)
            {
                unsafe
                {
                    ImGuiListClipperPtr clipper = new ImGuiListClipperPtr
                        (ImGuiNative.ImGuiListClipper_ImGuiListClipper());
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
                                            (ImGui.GetCursorPos(), TilesDimensions, ImGui.GetColorU32(UIManager.Pink));
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
                        float itemPosY = clipper.StartPosY + TotalRowHeight * (Array.IndexOf
                            (ids, LandMode ? SelectedLandId : SelectedStaticId) / columnsNumber);
                        ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                        UpdateScroll = false;
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
            CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text($"0x{tileInfo.RealIndex - MaxLandIndex:X4}");
            ImGui.TextUnformatted(tileInfo.Name);
            ImGui.Separator();
            if (!LandMode)
            {
                ImGui.Text($"Height: {tileInfo.Height}");
            }
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

    // Helper method to get the appropriate tile sets collection
    private object GetCurrentTileSets()
    {
        return LandMode ?
            ProfileManager.ActiveProfile.LandTileSets :
            ProfileManager.ActiveProfile.StaticTileSets;
    }

    private void DrawTileSets()
    {
        if (ImGui.BeginChild("TileSets"))
        {
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
                if (ImGui.BeginTable("TileSetTable", 3) && CEDClient.Initialized)
                {
                    unsafe
                    {
                        ImGuiListClipperPtr clipper = new ImGuiListClipperPtr
                            (ImGuiNative.ImGuiListClipper_ImGuiListClipper());
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
                    }
                    ImGui.EndTable();
                }
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

        return new(index, spriteInfo.Texture, spriteInfo.UV, name, flags, 0);
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
            flags,
            TileDataLoader.Instance.StaticData[index].Height
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

            if (ImGui.BeginTable("TiledataFilterTable", columnsNumber) && CEDClient.Initialized)
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
