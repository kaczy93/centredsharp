using CentrED.IO.Models;
using CentrED.Map;
using CentrED.UI;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

record struct CoastlineTransition(Direction fullMatch, Direction partialMatch, ushort tileId);


public class CoastlineTool : BaseTool
{
    private static MapManager mapManager => Application.CEDGame.MapManager;
    public override string Name => "Coastline";

    public override Keys Shortcut => Keys.F10;

    private sbyte _z = -5;
    private bool _overwriteExistingObjects = true;
    private bool _tweakTerrain = true;
    
    private Direction _dir = Direction.None;
    private List<CoastlineTransition> _transitionTiles = new();
    private List<ushort> _terrainBottomTiles = [];
    private List<ushort> _terrainWaterTiles = [];
    private List<ushort> _objectWaterTiles = [];
    

    public CoastlineTool()
    {
        _transitionTiles.Add(new(Direction.West, Direction.Left | Direction.Up, 0x179D));
        _transitionTiles.Add(new(Direction.South, Direction.Left | Direction.Down, 0x179F));
        _transitionTiles.Add(new(Direction.North, Direction.Up | Direction.Right, 0x17A1));      //0x17A2
        _transitionTiles.Add(new(Direction.East, Direction.Right | Direction.Down, 0x17A3)); //0x17A4
        _transitionTiles.Add(new(Direction.Left, Direction.None, 0x17A5));
        _transitionTiles.Add(new(Direction.Down, Direction.None, 0x17A6));
        _transitionTiles.Add(new(Direction.Up, Direction.None, 0x17A7));
        _transitionTiles.Add(new(Direction.Right, Direction.None, 0x17A8));
        _transitionTiles.Add(new(Direction.South | Direction.Left | Direction.West, Direction.Down | Direction.Up, 0x17A9));
        _transitionTiles.Add(new(Direction.West | Direction.Up | Direction.North, Direction.Left | Direction.Right, 0x17AA));
        _transitionTiles.Add(new(Direction.North | Direction.Right | Direction.East, Direction.Down | Direction.Up, 0x17AB));
        _transitionTiles.Add(new(Direction.East | Direction.Down | Direction.South, Direction.Left | Direction.Right, 0x17AC));
        
        _terrainWaterTiles.AddRange([0x00A8, 0x00A9, 0x00AA, 0x0AB]);
        _objectWaterTiles.AddRange([0x1559, 0x1797,0x1798,0x1799,0x179A,0x179B,0x179C]);
        for(ushort i = 0x004C; i <= 0x006F; i++)
        {
            _terrainBottomTiles.Add(i);
        }
    }
    
    internal override void Draw()
    {
        int waterLevel = _z;
        if (ImGuiEx.DragInt("Water level", ref waterLevel, 1, sbyte.MinValue, sbyte.MaxValue))
        {
            _z = (sbyte)Math.Clamp(waterLevel, sbyte.MinValue, sbyte.MaxValue);
            mapManager.VirtualLayerZ = _z;
        }
        ImGui.Checkbox("Overwrite Existing Objects", ref _overwriteExistingObjects);
        ImGui.Checkbox("Tweak Terrain", ref _tweakTerrain);
        ImGui.Separator();
        if(_dir != Direction.None)
            ImGui.Text($"Direction: {_dir}");
        base.Draw();
    }

    public override void OnActivated(TileObject? o)
    {
        mapManager.UseVirtualLayer = true;
        mapManager.VirtualLayerZ = _z;
    }

    public override void OnDeactivated(TileObject? o)
    {
        mapManager.UseVirtualLayer = false;
    }

    protected override void GhostApply(TileObject? o)
    {
        var maxZ = sbyte.MaxValue;
        if (o == null)
            return;
        
        var selectedTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if (selectedTile == null)
            return;
        
        if (_terrainWaterTiles.Contains(selectedTile.Tile.Id))
            return; 

        var selectedDirection = GetWaterDirection(selectedTile);
        if (selectedDirection is Direction.None)
            return;
        
        Direction[] corners =
        [
            Direction.Left | Direction.South,
            Direction.Left | Direction.West,
            Direction.Right | Direction.North,
            Direction.Right | Direction.East,
        ];
        
        if (corners.Any(c => selectedDirection.Contains(c)))
        {
            maxZ = (sbyte)(_z - 10); //To look good
        }
        else if (selectedDirection.Contains(Direction.Left) || selectedDirection.Contains(Direction.Right))
        {
            maxZ = (sbyte)(_z + 3); //To not hide statics because of terrain
        }
        
        var contextDir = Direction.Up; //Due to the height difference, we check what's around tile that's above the target
        var contextTile = mapManager.LandTiles[o.Tile.X + contextDir.Offset().Item1, o.Tile.Y + contextDir.Offset().Item2];
        if (contextTile == null)
            return;

        ushort newId = 0;
        if (_terrainWaterTiles.Contains(contextTile.Tile.Id) || _terrainBottomTiles.Contains(contextTile.Tile.Id))
        {
            newId = _objectWaterTiles[0];
        }
        else
        {
            var contextDirection = GetWaterDirection(contextTile);
            _dir = contextDirection; //For debugging purposes

            newId = _transitionTiles
                    //Full match
                    .Where(m => contextDirection.Contains(m.fullMatch)) 
                    //Doesn't have anything else than full match and partial match
                    .Where(m => (contextDirection & ~(m.fullMatch | m.partialMatch)) == Direction.None)
                    .Select(m => m.tileId)
                    .FirstOrDefault((ushort)0);
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
        
        var tile = selectedTile.Tile;
        if (_tweakTerrain && tile.Z > maxZ)
        {
            selectedTile.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, maxZ);
            MapManager.GhostLandTiles[selectedTile] = new LandObject(newTile);
            MapManager.OnLandTileElevated(newTile, newTile.Z);
        }
    }

    private Direction GetWaterDirection(TileObject o)
    {
        var around = DirectionHelper.All
                                    .ToDictionary(
                                        dir => dir, 
                                        dir => mapManager.LandTiles[o.Tile.X + dir.Offset().Item1, o.Tile.Y + dir.Offset().Item2]
                                        );
        if (around.Values.Any(t => t == null))
            return Direction.None;
        
        return around.Where
                     (kvp => 
                          _terrainWaterTiles.Contains(kvp.Value.Tile.Id) || 
                          _terrainBottomTiles.Contains(kvp.Value.Tile.Id)
                     )
                     .Select(kvp => kvp.Key)
                     .Aggregate(Direction.None, (a, b) => a | b);
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o == null)
            return;
        
        mapManager.StaticsManager.ClearGhost(o);
        var landTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if(landTile != null)
        {
            landTile.Reset();
            MapManager.GhostLandTiles.Remove(landTile);
            MapManager.OnLandTileElevated(landTile.LandTile, landTile.LandTile.Z);
        }
    }

    protected override void InternalApply(TileObject? o)
    {
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
        if(landTile != null)
        {
            if (MapManager.GhostLandTiles.TryGetValue(landTile, out var ghostLandTile))
            {
                landTile.LandTile.Z = ghostLandTile.Tile.Z;
            }
        }
    }
}