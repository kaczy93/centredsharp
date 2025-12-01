using CentrED.IO.Models;
using CentrED.Map;
using CentrED.UI;
using CentrED.Utils;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

record struct CoastlineTransition(Direction fullMatch, Direction partialMatch, ushort[] tileIds);

public class CoastlineTool : BaseTool
{
    private static MapManager mapManager => Application.CEDGame.MapManager;
    public override string Name => "Coastline";

    public override Keys Shortcut => Keys.F10;

    private sbyte _waterZ = -5;
    private bool _overwriteExistingObjects = true;
    private bool _tweakTerrain = true;
    private bool _drawBrownShoreDepth = false;
    private string _customBottomTilesText = "";

    private List<CoastlineTransition> _transitionTiles = new();
    private List<ushort> _terrainBottomTiles = [];
    private List<ushort> _customTerrainBottomTiles = [];
    private List<ushort> _terrainWaterTiles = [];
    private List<ushort> _objectWaterTiles = [];
    
    Direction[] _sideUpEdge =
    [
        Direction.Left | Direction.West,
        Direction.Right | Direction.North,
    ];
    
    public CoastlineTool()
    {
        _transitionTiles.Add(new(Direction.West, Direction.Left | Direction.Up, [0x179D, 0x179E]));
        _transitionTiles.Add(new(Direction.South, Direction.Left | Direction.Down, [0x179F, 0x17A0]));
        _transitionTiles.Add(new(Direction.North, Direction.Up | Direction.Right, [0x17A1, 0x17A2]));
        _transitionTiles.Add(new(Direction.East, Direction.Right | Direction.Down, [0x17A3, 0x17A4]));
        _transitionTiles.Add(new(Direction.Left, Direction.None, [0x17A5]));
        _transitionTiles.Add(new(Direction.Down, Direction.None, [0x17A6]));
        _transitionTiles.Add(new(Direction.Up, Direction.None, [0x17A7]));
        _transitionTiles.Add(new(Direction.Right, Direction.None, [0x17A8]));
        _transitionTiles.Add
            (new(Direction.South | Direction.Left | Direction.West, Direction.Down | Direction.Up, [0x17A9]));
        _transitionTiles.Add
            (new(Direction.West | Direction.Up | Direction.North, Direction.Left | Direction.Right, [0x17AA]));
        _transitionTiles.Add
            (new(Direction.North | Direction.Right | Direction.East, Direction.Down | Direction.Up, [0x17AB]));
        _transitionTiles.Add
            (new(Direction.East | Direction.Down | Direction.South, Direction.Left | Direction.Right, [0x17AC]));

        _terrainWaterTiles.AddRange([0x00A8, 0x00A9, 0x00AA, 0x0AB, 0x0136, 0x0137]);
        _objectWaterTiles.AddRange([0x1559, 0x1797, 0x1798, 0x1799, 0x179A, 0x179B, 0x179C]);
        for (ushort i = 0x004C; i <= 0x006F; i++)
        {
            _terrainBottomTiles.Add(i);
        }
    }

    internal override void Draw()
    {
        int waterLevel = _waterZ;
        if (ImGuiEx.DragInt("Water level", ref waterLevel, 1, sbyte.MinValue, sbyte.MaxValue))
        {
            _waterZ = (sbyte)Math.Clamp(waterLevel, sbyte.MinValue, sbyte.MaxValue);
            mapManager.VirtualLayerZ = _waterZ;
        }
        ImGui.Checkbox("Overwrite Existing Objects", ref _overwriteExistingObjects);
        ImGui.Checkbox("Tweak Terrain", ref _tweakTerrain);
        
        // Custom input always visible
        if (ImGui.InputText("Custom bottom tiles", ref _customBottomTilesText, 256))
        {
            ParseCustomBottomTiles();
        }
        ImGui.SetItemTooltip("Define additional bottom tiles (hex: 0x4C or decimal: 76) separated by commas. Used for recognition logic and drawing if enabled.");

        ImGui.Checkbox("Draw shore depth tiles", ref _drawBrownShoreDepth);
        
        ImGui.Separator();
        base.Draw();
    }

    private void ParseCustomBottomTiles()
    {
        _customTerrainBottomTiles.Clear();
        
        if (string.IsNullOrWhiteSpace(_customBottomTilesText))
        {
            return;
        }

        try
        {
            var ids = _customBottomTilesText.Split(',').Select(UshortParser.Apply).ToArray();
            _customTerrainBottomTiles.AddRange(ids);
        }
        catch
        {
            _customTerrainBottomTiles.Clear();
        }
    }

    // Used for CHECKING existing terrain (Logic: Default Tiles OR Custom Tiles)
    private bool IsBottomTile(ushort tileId)
    {
        return _terrainBottomTiles.Contains(tileId) || _customTerrainBottomTiles.Contains(tileId);
    }

    // Used for DRAWING new tiles (Logic: Custom Tiles ONLY if present, otherwise Default)
    private List<ushort> GetDrawingBottomTiles()
    {
        return _customTerrainBottomTiles.Count > 0 ? _customTerrainBottomTiles : _terrainBottomTiles;
    }

    public override void OnActivated(TileObject? o)
    {
        mapManager.UseVirtualLayer = true;
        mapManager.VirtualLayerZ = _waterZ;
    }

    public override void OnDeactivated(TileObject? o)
    {
        mapManager.UseVirtualLayer = false;
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o == null)
            return;

        var selectedTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if (selectedTile == null)
            return;

        if (_terrainWaterTiles.Contains(selectedTile.Tile.Id))
            return;

        // Handle brown shore depth barrier when enabled
        if (_drawBrownShoreDepth && _tweakTerrain)
        {
            // Check all surrounding tiles
            var around = DirectionHelper.All.ToDictionary
                (dir => dir, dir => mapManager.GetLandTile
                    (selectedTile.LandTile.X + dir.Offset().Item1, selectedTile.LandTile.Y + dir.Offset().Item2));

            // IsLand Check: Checks against ALL known bottom tiles (Default + Custom)
            bool isLand = !_terrainWaterTiles.Contains(selectedTile.Tile.Id) && !IsBottomTile(selectedTile.Tile.Id);

            if (isLand)
            {
                // Use specific drawing list (Custom ONLY if set)
                var tilesToDraw = GetDrawingBottomTiles();

                foreach (var kvp in around)
                {
                    if (kvp.Value != null && _terrainWaterTiles.Contains(kvp.Value.Id))
                    {
                        var waterLandObject = mapManager.LandTiles[kvp.Value.X, kvp.Value.Y];
                        
                        if (waterLandObject != null)
                        {
                            if (!MapManager.GhostLandTiles.ContainsKey(waterLandObject))
                            {
                                ushort brownShoreTileId = tilesToDraw[Random.Shared.Next(tilesToDraw.Count)];
                                sbyte brownShoreZ = (sbyte)(_waterZ - 10);
                                
                                var waterTile = waterLandObject.Tile;
                                waterLandObject.Visible = false;
                                var newTile = new LandTile(brownShoreTileId, waterTile.X, waterTile.Y, brownShoreZ);
                                MapManager.GhostLandTiles[waterLandObject] = new LandObject(newTile);
                                MapManager.OnLandTileElevated(newTile, newTile.Z);
                            }
                        }
                    }
                }
            }
        }

        var selectedDirection = GetWaterDirection(selectedTile.LandTile);
        
        //Due to the height difference, we check what's around tile that's above the target
        var contextOffset = Direction.Up.Offset();
        var contextTile = mapManager.GetLandTile(o.Tile.X + contextOffset.Item1, o.Tile.Y + contextOffset.Item2);
        if (contextTile == null)
            return;

        var contextDirection = GetWaterDirection(contextTile);
        
        var newLandZ = selectedTile.Tile.Z;
        
        // Terrain elevation logic uses IsBottomTile (checks both default and custom lists)
        if (IsBottomTile(selectedTile.Tile.Id) || 
            selectedDirection.Contains(Direction.West | Direction.Up | Direction.North) ||
            (!selectedDirection.Contains(Direction.Up) && _sideUpEdge.Any(e => selectedDirection.Contains(e)))
            )
        {
            newLandZ = (sbyte)(_waterZ - 10);
        }
        else if (selectedDirection == Direction.Up)
        {
            newLandZ = _waterZ; //Otherwise water sticks through terrain on up facing land edge
        }
        else if (selectedDirection.Contains(Direction.Up) || _sideUpEdge.Any(e => e == selectedDirection))
        {
            newLandZ = (sbyte)(_waterZ - 4);
        }
        else if (!selectedDirection.Contains(Direction.Down) && _sideUpEdge.Any(c => selectedDirection.Contains(c)))
        {
            newLandZ = (sbyte)(_waterZ - 10);
        }
        else if (selectedDirection is Direction.Left or Direction.Right)
        {
            newLandZ = (sbyte)(_waterZ + 2); //To not hide statics because of terrain
        }
        
        var tile = selectedTile.Tile;
        if (_tweakTerrain && tile.Z != newLandZ)
        {
            selectedTile.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, newLandZ);
            MapManager.GhostLandTiles[selectedTile] = new LandObject(newTile);
            MapManager.OnLandTileElevated(newTile, newTile.Z);
        }
      
        if (contextDirection.Contains(Direction.Up) || _sideUpEdge.Any(e => e == contextDirection || e == selectedDirection) )
        {
            contextDirection = selectedDirection; //We no longer look at tile above, as there is water above context tile
            contextTile = selectedTile.LandTile;
        }

        if (selectedDirection is Direction.None)
            return;

        ushort newId;
        // Object placement logic uses IsBottomTile (checks both default and custom lists)
        if (_terrainWaterTiles.Contains(contextTile.Id) || IsBottomTile(contextTile.Id))
        {
            newId = _objectWaterTiles[Random.Shared.Next(_objectWaterTiles.Count)];
        }
        else
        {
            newId = _transitionTiles
                    //Full match
                    .Where(m => contextDirection.Contains(m.fullMatch))
                    //Doesn't have anything else than full match and partial match
                    .Where(m => (contextDirection & ~(m.fullMatch | m.partialMatch)) == Direction.None)
                    .Select(m => m.tileIds[Random.Shared.Next(m.tileIds.Length)]).FirstOrDefault((ushort)0);
        }

        if (newId == 0)
        {
            return;
        }
        if (mapManager.StaticsManager.Get(selectedTile.Tile.X, selectedTile.Tile.Y).Any(so => so.Tile.Id == newId))
        {
            return;
        }
        var ghost = new StaticTile(newId, o.Tile.X, o.Tile.Y, o.Tile.Z, 0);
        var ghostObject = new StaticObject(ghost);
        mapManager.StaticsManager.AddGhost(o, ghostObject);
    }

    private Direction GetWaterDirection(LandTile land)
    {
        var around = DirectionHelper.All.ToDictionary
            (dir => dir, dir => mapManager.GetLandTile(land.X + dir.Offset().Item1, land.Y + dir.Offset().Item2));
        if (around.Values.Any(t => t == null))
            return Direction.None;

        return around.Where
                     (kvp => _terrainWaterTiles.Contains(kvp.Value.Id) || IsBottomTile(kvp.Value.Id))
                     .Select(kvp => kvp.Key).Aggregate(Direction.None, (a, b) => a | b);
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o == null)
            return;

        mapManager.StaticsManager.ClearGhost(o);
        var landTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if (landTile != null)
        {
            landTile.Reset();
            MapManager.GhostLandTiles.Remove(landTile);
            MapManager.OnLandTileElevated(landTile.LandTile, landTile.LandTile.Z);
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o == null)
            return;
        
        if (MapManager.StaticsManager.TryGetGhost(o, out var ghostTile))
        {
            if (_overwriteExistingObjects)
            {
                foreach (var existingObject in mapManager.StaticsManager.Get(o.Tile.X, o.Tile.Y))
                {
                    Client.Remove(existingObject.StaticTile); //Do we need to create a ghost for this?
                }
            }
            Client.Add(ghostTile.StaticTile);
        }
        var landTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if (landTile != null)
        {
            if (MapManager.GhostLandTiles.TryGetValue(landTile, out var ghostLandTile))
            {
                if (landTile.Tile.Id != ghostLandTile.Tile.Id)
                {
                    landTile.LandTile.ReplaceLand(ghostLandTile.Tile.Id, ghostLandTile.Tile.Z);
                }
                else
                {
                    landTile.LandTile.Z = ghostLandTile.Tile.Z;
                }
            }
        }
    }

    public override void GrabZ(sbyte z)
    {
        _waterZ = z;
    }
}