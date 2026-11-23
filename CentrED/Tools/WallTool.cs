using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.Tools;

public class WallTool : Tool
{
    public record WallTileChance(ushort TileId, int Chance);
    public record WallSet(ushort TopCornerTile, ushort BottomCornerTile, List<WallTileChance> LeftTiles, List<WallTileChance> RightTiles);
    
    public override string Name => App.Lang.Get(WALL_TOOL);
    public override Keys Shortcut => Keys.None;

    private enum State { READY, DRAWING, POLYGON_PENDING }
    private enum WallDirection { North, South, Left, Right }
    private enum DrawMode { Rectangle, Polygon }

    private State _state = State.READY;
    private DrawMode _drawMode = DrawMode.Rectangle;

    private List<(TileObject parent, StaticObject areaGhost)> _areaGhosts = new();
    private bool _eraseMode;

    private ushort _northTileId;
    private ushort _southTileId;
    private List<WallTileChance> _leftTiles = new();
    private List<WallTileChance> _rightTiles = new();

    private TileObject? _startTile;
    private TileObject? _endTile;

    private HashSet<(ushort x, ushort y)> _selectedArea = new();

    private List<HashSet<(ushort x, ushort y)>> _strokeHistory = new();
    private HashSet<(ushort x, ushort y)> _currentStroke = new();
    private bool _handledUndoThisFrame;

    private List<(TileObject parent, StaticObject ghost)> _ghosts = new();

    private TilesWindow? _tilesWindow;

    private string[] _wallSetNames = [""];
    private int _wallSetIndex;
    private string _wallSetNewName = "";
    private string _alertMessage = "";
    private bool _showAlert;

    private readonly ConfigValue<Dictionary<string, WallSet>> _wallSets = App.Profile.Active.Init
        ("wall_sets", new Dictionary<string, WallSet>(), ConfigConverters.Json<Dictionary<string, WallSet>>()); //TODO: can we do better than json

    public override void PostConstruct(MapManager mapManager)
    {
        _tilesWindow = UIManager.GetWindow<TilesWindow>();
        App.Profile.Changed += OnProfileChanged;
        OnProfileChanged();
    }

    private void OnProfileChanged()
    {
        _wallSetIndex = 0;
        UpdateWallSetNames();
    }

    public override void OnActivated(TileObject? o)
    {
        MapManager.UseVirtualLayer = true;
    }

    public override void OnDeactivated(TileObject? o)
    {
        ClearGhosts();
        ClearAreaGhosts();
        MapManager.UseVirtualLayer = false;
        _state = State.READY;
        _eraseMode = false;
        _startTile = null;
        _endTile = null;
        _selectedArea.Clear();
        _strokeHistory.Clear();
        _currentStroke.Clear();
    }

    public override void OnKeyPressed(Keys key)
    {
        if (key == Keys.LeftControl || key == Keys.RightControl)
        {
            _eraseMode = true;
        }

        if (key == Keys.Z && _eraseMode && _drawMode == DrawMode.Polygon &&
            _state == State.POLYGON_PENDING && _strokeHistory.Count > 0)
        {
            _handledUndoThisFrame = true;
            UndoLastStroke();
        }
    }
    public override bool HandlesUndo => _handledUndoThisFrame ||
                                        (_drawMode == DrawMode.Polygon &&
                                         _state == State.POLYGON_PENDING &&
                                         _strokeHistory.Count > 0);

    private void UndoLastStroke()
    {
        if (_strokeHistory.Count == 0)
            return;

        var lastStroke = _strokeHistory[^1];
        _strokeHistory.RemoveAt(_strokeHistory.Count - 1);

        foreach (var tile in lastStroke)
        {
            _selectedArea.Remove(tile);
        }

        UpdateAreaGhosts();

        if (_selectedArea.Count == 0)
        {
            _state = State.READY;
        }
    }

    public override void OnKeyReleased(Keys key)
    {
        if (key == Keys.LeftControl || key == Keys.RightControl)
        {
            _eraseMode = false;
        }
    }

    internal override void Draw()
    {
        _handledUndoThisFrame = false;

        if (!App.Client.Running)
        {
            ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            return;
        }

        DrawWallSets();

        ImGui.Separator();

        ImGui.Text(App.Lang.Get(DRAW_MODE) + ":");
        int modeIndex = (int)_drawMode;
        if (ImGui.RadioButton(App.Lang.Get(RECTANGLE), ref modeIndex, 0))
        {
            if (_drawMode != DrawMode.Rectangle)
            {
                _drawMode = DrawMode.Rectangle;
                ClearPolygonState();
            }
        }
        ImGui.SameLine();
        if (ImGui.RadioButton(App.Lang.Get(POLYGON), ref modeIndex, 1))
        {
            _drawMode = DrawMode.Polygon;
        }

        int z = MapManager.VirtualLayerZ;
        if (ImGuiEx.DragInt(App.Lang.Get(Z_LEVEL), ref z, 1, sbyte.MinValue, sbyte.MaxValue))
        {
            MapManager.VirtualLayerZ = z;
        }

        if (_drawMode == DrawMode.Polygon)
        {
            ImGui.PushTextWrapPos(0);
            if (_state == State.DRAWING)
            {
                if (_eraseMode)
                {
                    ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), App.Lang.Get(ERASING));
                }
                else
                {
                    ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), App.Lang.Get(PAINTING));
                }
            }
            else if (_state == State.POLYGON_PENDING)
            {
                ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), string.Format(App.Lang.Get(AREA_TILES_CTRL_DELETE), _selectedArea.Count));
                if (ImGui.Button(App.Lang.Get(CONFIRM)))
                {
                    ApplyPolygonWalls();
                }
                ImGui.SameLine();
                if (ImGui.Button(App.Lang.Get(CANCEL)))
                {
                    ClearPolygonState();
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), App.Lang.Get(PAINT_AREA_HINT));
            }
            ImGui.PopTextWrapPos();
        }

        ImGui.Separator();
        ImGui.Text(App.Lang.Get(WALL_TILES) + ":");
        ImGui.Separator();

        DrawTileSlot(WallDirection.North, ref _northTileId);
        ImGui.Separator();
        DrawTileSlot(WallDirection.South, ref _southTileId);
        ImGui.Separator();
        DrawTileList(WallDirection.Left, _leftTiles);
        ImGui.Separator();
        DrawTileList(WallDirection.Right, _rightTiles);

        ImGui.Separator();

        bool hasTiles = _northTileId > 0 || _southTileId > 0 || _leftTiles.Count > 0 || _rightTiles.Count > 0;
        ImGui.PushTextWrapPos(0);
        if (_drawMode == DrawMode.Rectangle)
        {
            if (_state == State.DRAWING)
            {
                ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), App.Lang.Get(DRAWING));
            }
            else if (hasTiles)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), App.Lang.Get(READY_DRAG_ON_MAP));
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), App.Lang.Get(ADD_TILES_TO_BEGIN));
            }
        }
        else if (!hasTiles)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), App.Lang.Get(ADD_TILES_TO_BEGIN));
        }

        ImGui.PopTextWrapPos();

        if (ImGui.Button(App.Lang.Get(RESET)))
        {
            ResetTool();
        }
    }

    private void DrawWallSets()
    {
        ImGui.Text(App.Lang.Get(WALL_SET) + ":");

        if (ImGui.Button(App.Lang.Get(NEW)))
        {
            ImGui.OpenPopup("NewWallSet");
        }
        ImGui.SameLine();

        bool hasTiles = _northTileId > 0 || _southTileId > 0 || _leftTiles.Count > 0 || _rightTiles.Count > 0;
        bool canSave = _wallSetIndex > 0 && hasTiles;
        ImGui.BeginDisabled(!canSave);
        if (ImGui.Button(App.Lang.Get(SAVE)))
        {
            SaveCurrentToWallSet();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();

        ImGui.BeginDisabled(_wallSetIndex == 0);
        if (ImGui.Button(App.Lang.Get(DELETE)))
        {
            ImGui.OpenPopup("DeleteWallSet");
        }
        ImGui.EndDisabled();

        if (ImGui.Combo("##WallSetCombo", ref _wallSetIndex, _wallSetNames, _wallSetNames.Length))
        {
            LoadWallSet();
        }

        if (ImGui.BeginPopupModal("NewWallSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGuiEx.InputText(App.Lang.Get(NAME), "##WallSetNewName", ref _wallSetNewName, 32);
            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_wallSetNewName) || _wallSetNames.Contains(_wallSetNewName));
            if (ImGui.Button(App.Lang.Get(CREATE)))
            {
                _wallSets.Value.Add(_wallSetNewName, new WallSet(_northTileId, _southTileId, new List<WallTileChance>(_leftTiles), new List<WallTileChance>(_rightTiles)));
                UpdateWallSetNames();
                _wallSetIndex = Array.IndexOf(_wallSetNames, _wallSetNewName);
                _wallSetNewName = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(CANCEL)))
            {
                _wallSetNewName = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("DeleteWallSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextUnformatted(string.Format(App.Lang.Get(DELETE_WARNING_1TYPE_2NAME), App.Lang.Get(WALL_SET).ToLower(), _wallSetNames[_wallSetIndex]));
            if (ImGui.Button(App.Lang.Get(YES)))
            {
                _wallSets.Value.Remove(_wallSetNames[_wallSetIndex]);
                UpdateWallSetNames();
                _wallSetIndex = 0;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(NO)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        if (_showAlert)
        {
            ImGui.OpenPopup("WallSetAlert");
            _showAlert = false;
        }

        if (ImGui.BeginPopupModal("WallSetAlert", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextUnformatted(_alertMessage);
            if (ImGui.Button(App.Lang.Get(OK)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private void UpdateWallSetNames()
    {
        _wallSetNames = _wallSets.Value.Keys.Prepend("").ToArray();
    }

    private void LoadWallSet()
    {
        if (_wallSetIndex == 0)
            return;

        var setName = _wallSetNames[_wallSetIndex];
        if (_wallSets.Value.TryGetValue(setName, out var wallSet))
        {
            if (!IsValidWallSet(wallSet))
            {
                _alertMessage = $"Wall set '{setName}' is invalid and has been deleted.";
                _showAlert = true;
                _wallSets.Value.Remove(setName);
                UpdateWallSetNames();
                _wallSetIndex = 0;
                return;
            }

            _northTileId = wallSet.TopCornerTile;
            _southTileId = wallSet.BottomCornerTile;
            _leftTiles = new List<WallTileChance>(wallSet.LeftTiles);
            _rightTiles = new List<WallTileChance>(wallSet.RightTiles);
        }
    }

    private bool IsValidWallSet(WallSet wallSet)
    {
        try
        {
            if (wallSet.LeftTiles == null || wallSet.RightTiles == null)
                return false;

            foreach (var tile in wallSet.LeftTiles)
            {
                if (tile == null || tile.Chance < 0 || tile.Chance > 100)
                    return false;
            }

            foreach (var tile in wallSet.RightTiles)
            {
                if (tile == null || tile.Chance < 0 || tile.Chance > 100)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SaveCurrentToWallSet()
    {
        if (_wallSetIndex == 0)
            return;

        var setName = _wallSetNames[_wallSetIndex];
        _wallSets.Value[setName] = new WallSet(_northTileId, _southTileId, new List<WallTileChance>(_leftTiles), new List<WallTileChance>(_rightTiles));
    }

    // What the user is asked for in each slot:
    // North = post of the top corner, South = piece with two walls of the bottom corner,
    // Left = wall along Y ('/' on screen), Right = wall along X ('\' on screen)
    private static (LangEntry Title, LangEntry Hint) GetSlotTexts(WallDirection dir) => dir switch
    {
        WallDirection.North => (NORTH, WALL_POST_HINT),
        WallDirection.South => (SOUTH, WALL_CORNER_HINT),
        WallDirection.Left => (LEFT, WALL_ALONG_Y_HINT),
        _ => (RIGHT, WALL_ALONG_X_HINT)
    };

    private void DrawTileSlot(WallDirection dir, ref ushort tileId)
    {
        // All slots look the same to ImGui, so each one needs its own ID scope
        ImGui.PushID((int)dir);

        if (tileId > 0)
            DrawTileButton(tileId);
        else
            DrawEmptySlot(dir);

        if (ImGuiEx.DragDropTarget(TilesWindow.OBJECT_DRAG_DROP_TYPE, out var ids))
        {
            if (ids.Length > 0)
            {
                tileId = ids[0];
            }
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && tileId > 0)
        {
            tileId = 0;
        }

        ImGuiEx.Tooltip(tileId > 0
            ? $"ID: {tileId:X4}\n{App.Lang.Get(RIGHT_CLICK_TO_CLEAR)}"
            : App.Lang.Get(DRAG_TILE_FROM_TILES_WINDOW));

        ImGui.SameLine();
        DrawSlotDescription(dir);
        ImGui.PopID();
    }

    private void DrawTileList(WallDirection dir, List<WallTileChance> tiles)
    {
        int totalChance = tiles.Sum(t => t.Chance);
        int remainingChance = 100 - totalChance;

        ImGui.PushID((int)dir);

        DrawEmptySlot(dir, true);
        if (ImGuiEx.DragDropTarget(TilesWindow.OBJECT_DRAG_DROP_TYPE, out var ids))
        {
            if (ids.Length > 0)
            {
                int defaultChance = Math.Max(1, Math.Min(remainingChance, 50));
                tiles.Add(new WallTileChance(ids[0], defaultChance));
            }
        }
        ImGuiEx.Tooltip(App.Lang.Get(DRAG_TILE_FROM_TILES_WINDOW));

        ImGui.SameLine();
        DrawSlotDescription(dir, totalChance > 0 ? $" ({totalChance}%)" : "");

        if (totalChance > 100)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), ImGuiEx.EscapeFormat(App.Lang.Get(OVER_100_PERCENT)));
        }

        int? removeIndex = null;
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            if (tile == null || tile.TileId == 0)
            {
                removeIndex = i;
                continue;
            }

            ImGui.PushID(i);

            DrawTileButton(tile.TileId);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                removeIndex = i;
            }
            ImGuiEx.Tooltip($"ID: {tile.TileId:X4}\n{tile.Chance}%\n{App.Lang.Get(RIGHT_CLICK_TO_REMOVE)}");

            ImGui.SameLine();

            ImGui.PushItemWidth(100);
            int chance = tile.Chance;
            if (ImGui.SliderInt("##Chance", ref chance, 1, 100, "%d%%"))
            {
                tiles[i] = new WallTileChance(tile.TileId, chance);
            }
            ImGui.PopItemWidth();

            ImGui.PopID();
        }

        if (removeIndex.HasValue)
        {
            tiles.RemoveAt(removeIndex.Value);
        }

        ImGui.PopID();
    }

    private void DrawTileButton(ushort tileId)
    {
        var slotSize = TilesWindow.TilesDimensions;

        // Group it so that the whole slot is the item, not just the image
        ImGui.BeginGroup();
        bool rendered = false;
        if (_tilesWindow != null)
        {
            try
            {
                var tileInfo = _tilesWindow.GetObjectInfo(tileId);
                if (tileInfo.Texture != null)
                {
                    // Scale to fit, when cropped a corner piece looks the same as a wall
                    var artSize = new Vector2(tileInfo.Bounds.Width, tileInfo.Bounds.Height);
                    var scale = Math.Min(1, Math.Min(slotSize.X / artSize.X, slotSize.Y / artSize.Y));
                    var pos = ImGui.GetCursorPos();
                    ImGui.Dummy(slotSize);
                    ImGui.SetCursorPos(pos + (slotSize - artSize * scale) / 2);
                    App.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, artSize * scale, true);
                    rendered = true;
                }
            }
            catch
            {
                // Invalid tile, will show button fallback
            }
        }

        if (!rendered)
        {
            ImGui.Button($"#{tileId}", slotSize);
        }
        ImGui.EndGroup();
    }

    // An empty slot shows the shape of the piece to drop in it
    private static void DrawEmptySlot(WallDirection dir, bool addsToList = false)
    {
        var size = TilesWindow.TilesDimensions;
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Button("##DropSlot", size);

        var drawList = ImGui.GetWindowDrawList();
        var unit = size / 44f;
        Vector2 Point(float x, float y) => pos + new Vector2(x, y) * unit;

        var line = ImGui.GetColorU32(ImGuiCol.Text);
        var fill = ImGui.GetColorU32(ImGuiCol.Text, 0.35f);
        var floor = ImGui.GetColorU32(ImGuiCol.Text, 0.3f);

        Vector2 left = Point(7, 31), top = Point(22, 23.5f), right = Point(37, 31), bottom = Point(22, 38.5f);
        var up = new Vector2(0, -18 * unit.Y);
        drawList.AddQuad(left, top, right, bottom, floor);

        void Wall(Vector2 from, Vector2 to)
        {
            drawList.AddQuadFilled(from, from + up, to + up, to, fill);
            drawList.AddQuad(from, from + up, to + up, to, line);
        }

        if (dir is WallDirection.Right or WallDirection.South)
            Wall(left, bottom);
        if (dir is WallDirection.Left or WallDirection.South)
            Wall(bottom, right);
        if (dir is WallDirection.North or WallDirection.South)
            drawList.AddLine(bottom, bottom + up, line, 4 * unit.X);

        if (addsToList)
        {
            drawList.AddLine(Point(34, 7), Point(42, 7), line);
            drawList.AddLine(Point(38, 3), Point(38, 11), line);
        }
    }

    private static void DrawSlotDescription(WallDirection dir, string suffix = "")
    {
        var (title, hint) = GetSlotTexts(dir);

        ImGui.BeginGroup();
        ImGui.AlignTextToFramePadding();
        DrawPlacementIcon(dir);
        ImGui.SameLine();
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(App.Lang.Get(title) + suffix);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        ImGui.TextUnformatted(App.Lang.Get(hint));
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();
        ImGui.EndGroup();
    }

    // A room as seen on the map, with the part built by this slot highlighted
    private static void DrawPlacementIcon(WallDirection dir)
    {
        var height = ImGui.GetTextLineHeight();
        var size = new Vector2(height * 1.7f, height);
        var pos = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetStyle().FramePadding.Y);
        ImGui.Dummy(size);

        var drawList = ImGui.GetWindowDrawList();
        var top = pos + new Vector2(size.X / 2, 0);
        var left = pos + new Vector2(0, size.Y / 2);
        var bottom = pos + new Vector2(size.X / 2, size.Y);
        var right = pos + new Vector2(size.X, size.Y / 2);
        var highlight = ImGui.GetColorU32(new Vector4(1, 0.8f, 0, 1));

        drawList.AddQuad(top, left, bottom, right, ImGui.GetColorU32(ImGuiCol.Text, 0.4f));
        switch (dir)
        {
            case WallDirection.North:
                drawList.AddCircleFilled(top, height * 0.25f, highlight);
                break;
            case WallDirection.South:
                drawList.AddLine((left + bottom) / 2, bottom, highlight, 3);
                drawList.AddLine(bottom, (bottom + right) / 2, highlight, 3);
                break;
            case WallDirection.Left:
                drawList.AddLine(top, left, highlight, 3);
                drawList.AddLine(bottom, right, highlight, 3);
                break;
            case WallDirection.Right:
                drawList.AddLine(top, right, highlight, 3);
                drawList.AddLine(left, bottom, highlight, 3);
                break;
        }
    }

    public override void OnMousePressed(TileObject? o)
    {
        if (o == null)
            return;

        if (_drawMode == DrawMode.Rectangle)
        {
            if (_state != State.READY)
                return;

            _state = State.DRAWING;
            Client.BeginUndoGroup();
            _startTile = o;
            _endTile = o;
            ClearGhosts();
            ApplyGhosts();
        }
        else
        {
            if (_state != State.READY && _state != State.POLYGON_PENDING)
                return;

            if (_state == State.READY && !_eraseMode)
            {
                _selectedArea.Clear();
                _strokeHistory.Clear();
                ClearAreaGhosts();
            }

            _state = State.DRAWING;

            _currentStroke = new HashSet<(ushort x, ushort y)>();

            if (_eraseMode)
            {
                if (_selectedArea.Remove((o.Tile.X, o.Tile.Y)))
                {
                    UpdateAreaGhosts();
                }
            }
            else
            {
                if (_selectedArea.Add((o.Tile.X, o.Tile.Y)))
                {
                    _currentStroke.Add((o.Tile.X, o.Tile.Y));
                    AddAreaGhost(o.Tile.X, o.Tile.Y);
                }
            }
        }
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (_state != State.DRAWING || o == null)
            return;

        if (_drawMode == DrawMode.Rectangle)
        {
            _endTile = o;
            ClearGhosts();
            ApplyGhosts();
        }
        else
        {
            if (_eraseMode)
            {
                if (_selectedArea.Remove((o.Tile.X, o.Tile.Y)))
                {
                    UpdateAreaGhosts();
                }
            }
            else
            {
                if (_selectedArea.Add((o.Tile.X, o.Tile.Y)))
                {
                    _currentStroke.Add((o.Tile.X, o.Tile.Y));
                    AddAreaGhost(o.Tile.X, o.Tile.Y);
                }
            }
        }
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_state != State.DRAWING)
            return;

        if (_drawMode == DrawMode.Rectangle)
        {
            foreach (var (_, ghost) in _ghosts)
            {
                Client.Add(ghost.StaticTile);
            }

            ClearGhosts();
            Client.EndUndoGroup();

            _state = State.READY;
            _startTile = null;
            _endTile = null;
        }
        else
        {
            if (!_eraseMode && _currentStroke.Count > 0)
            {
                _strokeHistory.Add(_currentStroke);
            }
            _currentStroke = new HashSet<(ushort x, ushort y)>();

            _state = State.POLYGON_PENDING;
        }
    }

    private void ApplyGhosts()
    {
        if (_startTile == null || _endTile == null)
            return;

        foreach (var (x, y, dir) in GetPerimeterTiles())
        {
            ushort tileId = dir switch
            {
                WallDirection.North => _northTileId,
                WallDirection.South => _southTileId,
                WallDirection.Left => GetRandomTile(_leftTiles),
                WallDirection.Right => GetRandomTile(_rightTiles),
                _ => 0
            };

            if (tileId == 0) continue;

            var tile = new StaticTile(
                tileId,
                x,
                y,
                (sbyte)MapManager.VirtualLayerZ,
                0
            );

            var ghostObj = new StaticObject(tile);

            var parent = MapManager.LandTiles[x, y];
            if (parent != null)
            {
                MapManager.StaticsManager.AddGhost(parent, ghostObj);
                _ghosts.Add((parent, ghostObj));
            }
        }
    }

    private ushort GetRandomTile(List<WallTileChance> tiles)
    {
        if (tiles.Count == 0) return 0;
        if (tiles.Count == 1) return tiles[0].TileId;

        int totalChance = tiles.Sum(t => t.Chance);
        if (totalChance == 0) return tiles[0].TileId;

        int roll = Random.Shared.Next(totalChance);
        int cumulative = 0;

        foreach (var tile in tiles)
        {
            cumulative += tile.Chance;
            if (roll < cumulative)
                return tile.TileId;
        }

        return tiles.Last().TileId;
    }

    private IEnumerable<(ushort x, ushort y, WallDirection dir)> GetPerimeterTiles()
    {
        if (_drawMode == DrawMode.Rectangle)
        {
            foreach (var tile in GetRectanglePerimeterTiles())
                yield return tile;
        }
        else
        {
            foreach (var tile in GetPolygonPerimeterTiles())
                yield return tile;
        }
    }

    private IEnumerable<(ushort x, ushort y, WallDirection dir)> GetRectanglePerimeterTiles()
    {
        var x1 = _startTile!.Tile.X;
        var y1 = _startTile!.Tile.Y;
        var x2 = _endTile!.Tile.X;
        var y2 = _endTile!.Tile.Y;

        var minX = Math.Min(x1, x2);
        var maxX = Math.Max(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxY = Math.Max(y1, y2);

        yield return ((ushort)minX, (ushort)minY, WallDirection.North);

        for (var x = minX + 1; x <= maxX; x++)
            yield return ((ushort)x, (ushort)minY, WallDirection.Right);

        for (var y = minY + 1; y < maxY; y++)
            yield return ((ushort)minX, (ushort)y, WallDirection.Left);

        if (maxX != minX)
        {
            for (var y = minY + 1; y < maxY; y++)
                yield return ((ushort)maxX, (ushort)y, WallDirection.Left);
        }

        if (maxY != minY)
        {
            for (var x = minX + 1; x < maxX; x++)
                yield return ((ushort)x, (ushort)maxY, WallDirection.Right);
        }

        if (maxY != minY)
        {
            yield return ((ushort)minX, (ushort)maxY, WallDirection.Left);
        }

        // South is a corner with two walls, a straight wall has none
        if (maxX != minX && maxY != minY)
        {
            yield return ((ushort)maxX, (ushort)maxY, WallDirection.South);
        }
    }

    // UO walls stand on the south and east edge of their tile, all meeting at its SE corner:
    // Right is the south edge (joins the tile to the west), Left is the east edge (joins the tile to the north),
    // South is both edges and North is just the post.
    private IEnumerable<(ushort x, ushort y, WallDirection dir)> GetPolygonPerimeterTiles()
    {
        bool Selected(int x, int y) =>
            x >= 0 && x <= ushort.MaxValue && y >= 0 && y <= ushort.MaxValue &&
            _selectedArea.Contains(((ushort)x, (ushort)y));

        // A tile is floor when it and its N, W, NW neighbours are selected
        bool Floor(int x, int y) =>
            Selected(x, y) && Selected(x - 1, y) && Selected(x, y - 1) && Selected(x - 1, y - 1);

        // No wall where there is floor on both sides
        bool SouthEdge(int x, int y) =>
            Selected(x, y) && Selected(x - 1, y) && !(Floor(x, y) && Floor(x, y + 1));

        bool EastEdge(int x, int y) =>
            Selected(x, y) && Selected(x, y - 1) && !(Floor(x, y) && Floor(x + 1, y));

        foreach (var (x, y) in _selectedArea)
        {
            bool south = SouthEdge(x, y);
            bool east = EastEdge(x, y);

            if (south && east)
                yield return (x, y, WallDirection.South);
            else if (south)
                yield return (x, y, WallDirection.Right);
            else if (east)
                yield return (x, y, WallDirection.Left);
            else if (SouthEdge(x + 1, y) || EastEdge(x, y + 1))
                yield return (x, y, WallDirection.North); // walls only start here
            else if (!Selected(x - 1, y) && !Selected(x + 1, y) && !Selected(x, y - 1) && !Selected(x, y + 1))
                yield return (x, y, WallDirection.North); // single tile
        }
    }

    private void ClearGhosts()
    {
        foreach (var (parent, _) in _ghosts)
        {
            MapManager.StaticsManager.ClearGhost(parent);
        }
        _ghosts.Clear();
    }
    private void AddAreaGhost(ushort x, ushort y)
    {
        if (MapManager?.LandTiles == null)
            return;

        var parent = MapManager.LandTiles[x, y];
        if (parent == null)
            return;

        var tile = new StaticTile(
            0x0495,
            x,
            y,
            (sbyte)MapManager.VirtualLayerZ,
            0x0044
        );

        var ghostObj = new StaticObject(tile);
        MapManager.StaticsManager?.AddGhost(parent, ghostObj);
        _areaGhosts.Add((parent, ghostObj));
    }

    private void UpdateAreaGhosts()
    {
        ClearAreaGhosts();
        foreach (var (x, y) in _selectedArea)
        {
            AddAreaGhost(x, y);
        }
    }

    private void ClearAreaGhosts()
    {
        foreach (var (parent, _) in _areaGhosts)
        {
            MapManager.StaticsManager.ClearGhost(parent);
        }
        _areaGhosts.Clear();
    }

    private void ClearPolygonState()
    {
        ClearAreaGhosts();
        _selectedArea.Clear();
        _strokeHistory.Clear();
        _currentStroke.Clear();
        _state = State.READY;
    }

    private void ApplyPolygonWalls()
    {
        if (_selectedArea.Count == 0)
            return;

        Client.BeginUndoGroup();

        foreach (var (x, y, dir) in GetPolygonPerimeterTiles())
        {
            ushort tileId = dir switch
            {
                WallDirection.North => _northTileId,
                WallDirection.South => _southTileId,
                WallDirection.Left => GetRandomTile(_leftTiles),
                WallDirection.Right => GetRandomTile(_rightTiles),
                _ => 0
            };

            if (tileId == 0) continue;

            var tile = new StaticTile(
                tileId,
                x,
                y,
                (sbyte)MapManager.VirtualLayerZ,
                0
            );

            Client.Add(tile);
        }

        Client.EndUndoGroup();

        ClearPolygonState();
    }

    private void ResetTool()
    {
        _northTileId = 0;
        _southTileId = 0;
        _leftTiles.Clear();
        _rightTiles.Clear();
        _state = State.READY;
        ClearGhosts();
        ClearAreaGhosts();
        _startTile = null;
        _endTile = null;
        _selectedArea.Clear();
        _strokeHistory.Clear();
        _currentStroke.Clear();
    }
}
