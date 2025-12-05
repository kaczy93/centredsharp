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

    /// <summary>
    /// Result from ApplyCoastlineAt containing static tile, terrain modifications, and push info.
    /// </summary>
    public record CoastlineResult(
        StaticTile? StaticTile,
        sbyte? NewTerrainZ,
        ushort? NewLandTileId = null,        // Replace current tile with this ID (e.g., shore tile 0x0042)
        (ushort x, ushort y, ushort id)? PushTile = null  // Push original tile to this location
    );

    /// <summary>
    /// Apply coastline to a tile. Can be called from Large Scale Operations.
    /// Returns the static tile to add and optional terrain Z modification.
    /// </summary>
    public static CoastlineResult ApplyCoastlineAt(CentrED.Client.CentrEDClient client, ushort x, ushort y, sbyte waterZ = -5, bool tweakTerrain = true)
    {
        var landTile = client.GetLandTile(x, y);

        // Water terrain tiles
        HashSet<ushort> terrainWaterTiles = [0x00A8, 0x00A9, 0x00AA, 0x00AB, 0x0136, 0x0137];

        // Shore/bottom tiles
        HashSet<ushort> terrainBottomTiles = Enumerable.Range(0x004C, 0x006F - 0x004C + 1)
            .Select(i => (ushort)i).ToHashSet();

        // Border tiles - land tiles placed on water pixels (all biomes)
        HashSet<ushort> borderTiles =
        [
            // Grass border tiles
            0x0093, 0x0096, 0x0098, 0x0099, 0x008E, 0x0095, 0x00A0,
            // Forest border tiles
            0x02EE, 0x02EF, 0x02F0, 0x02F1, 0x02F2, 0x02F3, 0x00C5
        ];

        // Skip if this tile IS water - coastline statics go on LAND tiles adjacent to water
        bool isWaterTile = terrainWaterTiles.Contains(landTile.Id) || terrainBottomTiles.Contains(landTile.Id);
        if (isWaterTile)
            return new CoastlineResult(null, null);

        // Transition tiles - directions indicate where WATER is relative to land tile
        List<CoastlineTransition> transitionTiles =
        [
            new(Direction.West, Direction.Left | Direction.Up, [0x179D, 0x179E]),
            new(Direction.South, Direction.Left | Direction.Down, [0x179F, 0x17A0]),
            new(Direction.North, Direction.Up | Direction.Right, [0x17A1, 0x17A2]),
            new(Direction.East, Direction.Right | Direction.Down, [0x17A3, 0x17A4]),
            new(Direction.Left, Direction.None, [0x17A5]),
            new(Direction.Down, Direction.None, [0x17A6]),
            new(Direction.Up, Direction.None, [0x17A7]),
            new(Direction.Right, Direction.None, [0x17A8]),
            new(Direction.South | Direction.Left | Direction.West, Direction.Down | Direction.Up, [0x17A9]),
            new(Direction.West | Direction.Up | Direction.North, Direction.Left | Direction.Right, [0x17AA]),
            new(Direction.North | Direction.Right | Direction.East, Direction.Down | Direction.Up, [0x17AB]),
            new(Direction.East | Direction.Down | Direction.South, Direction.Left | Direction.Right, [0x17AC])
        ];

        ushort[] objectWaterTiles = [0x1797, 0x1798, 0x1799, 0x179A, 0x179B, 0x179C];

        Direction[] sideUpEdge =
        [
            Direction.Left | Direction.West,
            Direction.Right | Direction.North
        ];

        // Get water direction - find which directions have PURE water adjacent to this land tile
        // Only check for actual water tiles, not shore/bottom tiles (to avoid cascade effect)
        Direction GetWaterDir(ushort tx, ushort ty)
        {
            var result = Direction.None;
            foreach (var dir in DirectionHelper.All)
            {
                var offset = dir.Offset();
                var nx = tx + offset.Item1;
                var ny = ty + offset.Item2;

                // Skip if out of bounds
                if (nx < 0 || ny < 0 || nx > ushort.MaxValue || ny > ushort.MaxValue)
                    continue;

                try
                {
                    var neighbor = client.GetLandTile((ushort)nx, (ushort)ny);
                    // Only check for PURE water tiles, not shore/bottom tiles
                    bool isWater = terrainWaterTiles.Contains(neighbor.Id);
                    if (isWater)
                    {
                        result |= dir;
                    }
                }
                catch
                {
                    // Skip if coordinates are out of map bounds
                }
            }
            return result;
        }

        var selectedDirection = GetWaterDir(x, y);
        if (selectedDirection == Direction.None)
            return new CoastlineResult(null, null);

        // Calculate terrain Z modification (same logic as GhostApply lines 194-228)
        sbyte? newLandZ = null;
        if (tweakTerrain)
        {
            var currentZ = landTile.Z;
            sbyte calculatedZ = currentZ;

            if (terrainBottomTiles.Contains(landTile.Id) ||
                selectedDirection.HasFlag(Direction.West | Direction.Up | Direction.North) ||
                (!selectedDirection.HasFlag(Direction.Up) && sideUpEdge.Any(e => selectedDirection.HasFlag(e))))
            {
                calculatedZ = (sbyte)(waterZ - 10);
            }
            else if (selectedDirection == Direction.Up)
            {
                calculatedZ = waterZ; // Otherwise water sticks through terrain on up facing land edge
            }
            else if (selectedDirection.HasFlag(Direction.Up) || sideUpEdge.Any(e => e == selectedDirection))
            {
                calculatedZ = (sbyte)(waterZ - 4);
            }
            else if (!selectedDirection.HasFlag(Direction.Down) && sideUpEdge.Any(c => selectedDirection.HasFlag(c)))
            {
                calculatedZ = (sbyte)(waterZ - 10);
            }
            else if (selectedDirection is Direction.Left or Direction.Right)
            {
                calculatedZ = (sbyte)(waterZ + 2); // To not hide statics because of terrain
            }

            if (calculatedZ != currentZ)
            {
                newLandZ = calculatedZ;
            }
        }

        // Get context tile (tile "above" in UO terms)
        var contextOffset = Direction.Up.Offset();
        var ctxX = x + contextOffset.Item1;
        var ctxY = y + contextOffset.Item2;

        // Check bounds for context tile
        if (ctxX < 0 || ctxY < 0 || ctxX > ushort.MaxValue || ctxY > ushort.MaxValue)
            return new CoastlineResult(null, null);

        var contextX = (ushort)ctxX;
        var contextY = (ushort)ctxY;
        LandTile contextTile;
        try
        {
            contextTile = client.GetLandTile(contextX, contextY);
        }
        catch
        {
            return new CoastlineResult(null, null);
        }

        var contextDirection = GetWaterDir(contextX, contextY);

        // Context adjustment logic from CoastlineTool
        if (contextDirection.HasFlag(Direction.Up) || sideUpEdge.Any(e => e == contextDirection || e == selectedDirection))
        {
            contextDirection = selectedDirection;
            contextTile = landTile;
        }

        // Determine biome tile for pushing inland
        // Grass biome: 0x0003-0x0006, 0x037B-0x037E -> push 0x0004
        // Forest biome: 0x00C4-0x00C7, 0x02EE-0x02F3 -> push 0x00C5
        // Shore tile at water edge is ALWAYS 0x0095
        HashSet<ushort> grassTiles =
        [
            0x0003, 0x0004, 0x0005, 0x0006,  // Grass base tiles
            0x037B, 0x037C, 0x037D, 0x037E,  // Grass variants
            0x0093, 0x0096, 0x0098, 0x0099, 0x008E, 0x0095, 0x00A0  // Grass border tiles
        ];
        HashSet<ushort> forestTiles =
        [
            0x00C4, 0x00C5, 0x00C6, 0x00C7,  // Forest base tiles
            0x02EE, 0x02EF, 0x02F0, 0x02F1, 0x02F2, 0x02F3  // Forest border tiles
        ];

        // Shore tile at water edge is always 0x0095
        const ushort shoreTile = 0x0095;

        // Biome tile to push inland
        ushort biomeTile;
        if (forestTiles.Contains(landTile.Id))
        {
            biomeTile = 0x00C5;  // Forest biome tile
        }
        else if (grassTiles.Contains(landTile.Id))
        {
            biomeTile = 0x0004;  // Grass biome tile
        }
        else
        {
            // Default to grass for unknown biomes
            biomeTile = 0x0004;
        }

        // Find primary water direction for push calculation
        Direction? primaryDir = null;
        foreach (var dir in new[] { Direction.West, Direction.South, Direction.North, Direction.East,
                                    Direction.Left, Direction.Down, Direction.Up, Direction.Right })
        {
            if (selectedDirection.HasFlag(dir))
            {
                primaryDir = dir;
                break;
            }
        }

        if (primaryDir == null)
            return new CoastlineResult(null, null);

        // Calculate push direction (opposite of water direction)
        var oppositeDir = primaryDir.Value switch
        {
            Direction.West => Direction.East,
            Direction.East => Direction.West,
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };

        // Get push target coordinates - push BIOME tile inland (not original tile)
        (ushort x, ushort y, ushort id)? pushTile = null;
        if (oppositeDir != Direction.None)
        {
            var pushOffset = oppositeDir.Offset();
            var pushX = x + pushOffset.Item1;
            var pushY = y + pushOffset.Item2;

            if (pushX >= 0 && pushY >= 0 && pushX <= ushort.MaxValue && pushY <= ushort.MaxValue)
            {
                // Push the biome-appropriate tile (0x0004 grass, 0x00C5 forest) inland
                pushTile = ((ushort)pushX, (ushort)pushY, biomeTile);
            }
        }

        // Get wave static based on water direction
        ushort waveStaticId = transitionTiles
            .Where(m => selectedDirection.HasFlag(m.fullMatch))
            .Where(m => (selectedDirection & ~(m.fullMatch | m.partialMatch)) == Direction.None)
            .Select(m => m.tileIds[Random.Shared.Next(m.tileIds.Length)])
            .FirstOrDefault((ushort)0);

        // If no transition tile matches, use plain water objects as fallback
        if (waveStaticId == 0 && selectedDirection != Direction.None)
        {
            waveStaticId = objectWaterTiles[Random.Shared.Next(objectWaterTiles.Length)];
        }

        // Return result: replace current tile with shore tile, add wave static on top of shore
        // Shore tile goes at Z=-15 (below water level), wave static on shore tile at Z=-5
        return new CoastlineResult(
            waveStaticId != 0 ? new StaticTile(waveStaticId, x, y, waterZ, 0) : null,  // Wave static on shore tile
            (sbyte)(waterZ - 10),    // Set Z to -15 (waterZ is -5, so -5 - 10 = -15)
            shoreTile,               // Replace with shore tile 0x0066
            pushTile                 // Push original tile inland
        );
    }
}