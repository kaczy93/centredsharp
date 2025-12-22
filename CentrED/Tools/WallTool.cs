using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.Tools;

public class WallTool : Tool
{
    public override string Name => "Wall";
    public override Keys Shortcut => Keys.F11;

    private enum State { READY, DRAWING }
    private enum WallDirection { North, South, Left, Right }

    private State _state = State.READY;

    private ushort _northTileId;
    private ushort _southTileId;
    private List<WallTileChance> _leftTiles = new();
    private List<WallTileChance> _rightTiles = new();

    private TileObject? _startTile;
    private TileObject? _endTile;
    private List<(TileObject parent, StaticObject ghost)> _ghosts = new();

    private TilesWindow? _tilesWindow;

    private string[] _wallSetNames = [""];
    private int _wallSetIndex;
    private string _wallSetNewName = "";
    private string _alertMessage = "";
    private bool _showAlert;

    private Dictionary<string, WallSet> WallSets => ProfileManager.ActiveProfile.WallSets;

    public override void PostConstruct(MapManager mapManager)
    {
        _tilesWindow = UIManager.GetWindow<TilesWindow>();
        UpdateWallSetNames();
    }

    public override void OnActivated(TileObject? o)
    {
        MapManager.UseVirtualLayer = true;
    }

    public override void OnDeactivated(TileObject? o)
    {
        ClearGhosts();
        MapManager.UseVirtualLayer = false;
        _state = State.READY;
        _startTile = null;
        _endTile = null;
    }

    internal override void Draw()
    {
        DrawWallSets();

        ImGui.Separator();
        ImGui.Text("Wall Tiles:");
        ImGui.Separator();

        DrawTileSlot("North", ref _northTileId);
        DrawTileSlot("South", ref _southTileId);
        ImGui.Separator();
        DrawTileList("Left", _leftTiles);
        ImGui.Separator();
        DrawTileList("Right", _rightTiles);

        ImGui.Separator();

        if (_state == State.DRAWING)
        {
            ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), "Drawing...");
        }
        else
        {
            bool hasTiles = _northTileId > 0 || _southTileId > 0 || _leftTiles.Count > 0 || _rightTiles.Count > 0;
            if (hasTiles)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "Ready - Drag on map");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "Add tiles to begin");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            ResetTool();
        }

        ImGui.Separator();

        int z = MapManager.VirtualLayerZ;
        if (ImGuiEx.DragInt("Z Level", ref z, 1, sbyte.MinValue, sbyte.MaxValue))
        {
            MapManager.VirtualLayerZ = z;
        }
    }

    private void DrawWallSets()
    {
        ImGui.Text("Wall Set:");

        if (ImGui.Button("New"))
        {
            ImGui.OpenPopup("NewWallSet");
        }
        ImGui.SameLine();

        bool hasTiles = _northTileId > 0 || _southTileId > 0 || _leftTiles.Count > 0 || _rightTiles.Count > 0;
        bool canSave = _wallSetIndex > 0 && hasTiles;
        ImGui.BeginDisabled(!canSave);
        if (ImGui.Button("Save"))
        {
            SaveCurrentToWallSet();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();

        ImGui.BeginDisabled(_wallSetIndex == 0);
        if (ImGui.Button("Delete"))
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
            ImGuiEx.InputText("Name", "##WallSetNewName", ref _wallSetNewName, 32);
            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_wallSetNewName) || _wallSetNames.Contains(_wallSetNewName));
            if (ImGui.Button("Create"))
            {
                WallSets.Add(_wallSetNewName, new WallSet(_northTileId, _southTileId, new List<WallTileChance>(_leftTiles), new List<WallTileChance>(_rightTiles)));
                UpdateWallSetNames();
                _wallSetIndex = Array.IndexOf(_wallSetNames, _wallSetNewName);
                ProfileManager.Save();
                _wallSetNewName = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _wallSetNewName = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("DeleteWallSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text($"Delete wall set '{_wallSetNames[_wallSetIndex]}'?");
            if (ImGui.Button("Yes"))
            {
                WallSets.Remove(_wallSetNames[_wallSetIndex]);
                UpdateWallSetNames();
                _wallSetIndex = 0;
                ProfileManager.Save();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
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
            ImGui.Text(_alertMessage);
            if (ImGui.Button("OK"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private void UpdateWallSetNames()
    {
        _wallSetNames = WallSets.Keys.Prepend("").ToArray();
    }

    private void LoadWallSet()
    {
        if (_wallSetIndex == 0)
            return;

        var setName = _wallSetNames[_wallSetIndex];
        if (WallSets.TryGetValue(setName, out var wallSet))
        {
            if (!IsValidWallSet(wallSet))
            {
                _alertMessage = $"Wall set '{setName}' is invalid and has been deleted.";
                _showAlert = true;
                WallSets.Remove(setName);
                UpdateWallSetNames();
                _wallSetIndex = 0;
                ProfileManager.Save();
                return;
            }

            _northTileId = wallSet.North;
            _southTileId = wallSet.South;
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
        WallSets[setName] = new WallSet(_northTileId, _southTileId, new List<WallTileChance>(_leftTiles), new List<WallTileChance>(_rightTiles));
        ProfileManager.Save();
    }

    private void DrawTileSlot(string label, ref ushort tileId, bool optional = false)
    {
        ImGui.Text($"{label}:");
        ImGui.SameLine();

        var slotSize = TilesWindow.TilesDimensions;

        bool rendered = false;
        if (tileId > 0 && _tilesWindow != null)
        {
            try
            {
                var tileInfo = _tilesWindow.GetObjectInfo(tileId);
                if (tileInfo.Texture != null)
                {
                    CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, slotSize, false);
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
            if (tileId > 0)
            {
                ImGui.Button($"#{tileId}", slotSize);
            }
            else
            {
                var buttonLabel = optional ? "(Optional)" : "Drop Here";
                ImGui.Button(buttonLabel, slotSize);
            }
        }

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

        if (tileId > 0)
        {
            ImGuiEx.Tooltip($"ID: {tileId:X4}\nRight-click to clear");
        }
        else
        {
            var tip = optional ? "Optional - drag a tile for alternating walls" : "Drag a tile from the Tiles window";
            ImGuiEx.Tooltip(tip);
        }
    }

    private void DrawTileList(string label, List<WallTileChance> tiles)
    {
        int totalChance = tiles.Sum(t => t.Chance);
        int remainingChance = 100 - totalChance;

        ImGui.Text($"{label} Tiles ({totalChance}%):");

        if (totalChance > 100)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Over 100%!");
        }

        var slotSize = TilesWindow.TilesDimensions;

        int? removeIndex = null;
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            if (tile == null || tile.TileId == 0)
            {
                removeIndex = i;
                continue;
            }

            ImGui.PushID($"{label}_{i}");

            bool rendered = false;
            if (_tilesWindow != null && tile.TileId > 0)
            {
                try
                {
                    var tileInfo = _tilesWindow.GetObjectInfo(tile.TileId);
                    if (tileInfo.Texture != null)
                    {
                        CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, slotSize, false);
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
                ImGui.Button($"#{tile.TileId}", slotSize);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                removeIndex = i;
            }
            ImGuiEx.Tooltip($"ID: {tile.TileId:X4}\n{tile.Chance}%\nRight-click to remove");

            ImGui.SameLine();

            ImGui.PushItemWidth(100);
            int chance = tile.Chance;
            if (ImGui.SliderInt($"##Chance{i}", ref chance, 1, 100, "%d%%"))
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

        ImGui.Button("Drop to Add", slotSize);
        if (ImGuiEx.DragDropTarget(TilesWindow.OBJECT_DRAG_DROP_TYPE, out var ids))
        {
            if (ids.Length > 0)
            {
                int defaultChance = Math.Max(1, Math.Min(remainingChance, 50));
                tiles.Add(new WallTileChance(ids[0], defaultChance));
            }
        }
        ImGuiEx.Tooltip("Drag a tile from the Tiles window to add");
    }

    public override void OnMousePressed(TileObject? o)
    {
        if (_state != State.READY || o == null)
            return;

        _state = State.DRAWING;
        _startTile = o;
        _endTile = o;
        Client.BeginUndoGroup();

        ApplyGhosts();
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (_state != State.DRAWING || o == null)
            return;

        _endTile = o;
        ClearGhosts();
        ApplyGhosts();
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (_state != State.DRAWING)
            return;

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

        if (maxY != minY && maxX != minX)
        {
            yield return ((ushort)minX, (ushort)maxY, WallDirection.Left);
        }

        if (maxX != minX || maxY != minY)
        {
            yield return ((ushort)maxX, (ushort)maxY, WallDirection.South);
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

    private void ResetTool()
    {
        _northTileId = 0;
        _southTileId = 0;
        _leftTiles.Clear();
        _rightTiles.Clear();
        _state = State.READY;
        ClearGhosts();
        _startTile = null;
        _endTile = null;
    }
}
