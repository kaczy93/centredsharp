using CentrED.IO.Models;
using CentrED.Map;
using CentrED.UI;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

record struct CoastlineMatch(Direction fullMatch, Direction partialMatch, ushort tileId);


public class CoastlineTool : BaseTool
{
    public MapManager mapManager => Application.CEDGame.MapManager;
    public override string Name => "Coastline";

    public override Keys Shortcut => Keys.F10;

    private sbyte _z = -5;
    private Direction _dir = Direction.None;
    private List<CoastlineMatch> _matchList = new();
    private List<ushort> _waterBottonTerrain = [];
    private List<ushort> _fullWaterTerrain = [];
    private List<ushort> _fullWaterObject = [];
    

    public CoastlineTool()
    {
        _matchList.Add(new(Direction.West, Direction.Left | Direction.Up, 0x179D));
        _matchList.Add(new(Direction.South, Direction.Left | Direction.Down, 0x179F));
        _matchList.Add(new(Direction.North, Direction.Up | Direction.Right, 0x17A1));      //0x17A2
        _matchList.Add(new(Direction.East, Direction.Right | Direction.Down, 0x17A3)); //0x17A4
        _matchList.Add(new(Direction.Left, Direction.None, 0x17A5));
        _matchList.Add(new(Direction.Down, Direction.None, 0x17A6));
        _matchList.Add(new(Direction.Up, Direction.None, 0x17A7));
        _matchList.Add(new(Direction.Right, Direction.None, 0x17A8));
        _matchList.Add(new(Direction.South | Direction.Left | Direction.West, Direction.Down | Direction.Up, 0x17A9));
        _matchList.Add(new(Direction.West | Direction.Up | Direction.North, Direction.Left | Direction.Right, 0x17AA));
        _matchList.Add(new(Direction.North | Direction.Right | Direction.East, Direction.Down | Direction.Up, 0x17AB));
        _matchList.Add(new(Direction.East | Direction.Down | Direction.South, Direction.Left | Direction.Right, 0x17AC));
        
        _fullWaterTerrain.AddRange([0x00A8, 0x00A9, 0x00AA, 0x0AB]);
        _fullWaterObject.AddRange([0x1559, 0x1797,0x1798,0x1799,0x179A,0x179B,0x179C]);
        
        //Water bottom
        for(ushort i = 0x004C; i <= 0x006F; i++)
        {
            _waterBottonTerrain.Add(i);
        }
    }

    protected override void GhostApply(TileObject? o)
    {
        var maxZ = sbyte.MaxValue;
        if (o == null)
            return;
        
        var selectedTile = mapManager.LandTiles[o.Tile.X, o.Tile.Y];
        if (selectedTile == null)
            return;
        
        var aroundSelected = DirectionHelper.All.ToDictionary
        (
            dir => dir,
            dir => mapManager.LandTiles[selectedTile.Tile.X + dir.Offset().Item1,
                                        selectedTile.Tile.Y + dir.Offset().Item2]
        );
        var selectedDirection = aroundSelected
                                .Where(kvp => 
                                           _fullWaterTerrain.Contains(kvp.Value.Tile.Id) ||
                                           _waterBottonTerrain.Contains(kvp.Value.Tile.Id))
                                .Select(kvp => kvp.Key)
                                .Aggregate(Direction.None, (a, b) => a | b);
        if (selectedDirection != Direction.None && selectedDirection != Direction.Up)
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
        
        var contextDir = Direction.Up; //Due to the height difference, to have it look right, we see what's around tile above targeted tile
        var contextTile = mapManager.LandTiles[o.Tile.X + contextDir.Offset().Item1, o.Tile.Y + contextDir.Offset().Item2];
        if (contextTile == null)
            return;
        var contextDirection = Direction.None;

        ushort newId = 0;
        if (_fullWaterTerrain.Contains(contextTile.Tile.Id) || _waterBottonTerrain.Contains(contextTile.Tile.Id))
        {
            newId = _fullWaterObject[0];
        }
        else
        {
            var aroundContext = DirectionHelper.All.ToDictionary
            (
                dir => dir,
                dir => mapManager.LandTiles[contextTile.Tile.X + dir.Offset().Item1,
                                            contextTile.Tile.Y + dir.Offset().Item2]
            );

            if (aroundContext.Values.Any(t => t == null))
                return;

            contextDirection = aroundContext.Where
            (kvp => _fullWaterTerrain.Contains(kvp.Value.Tile.Id) || _waterBottonTerrain.Contains(kvp.Value.Tile.Id)
            ).Select(kvp => kvp.Key).Aggregate(Direction.None, (a, b) => a | b);


            newId = _matchList.Where
            (m => contextDirection.Contains(m.fullMatch) && //We have full match
                  (contextDirection & ~(m.fullMatch | m.partialMatch)) ==
                  Direction.None //Doesn't have anything else than full match and partial match
            ).Select(m => m.tileId).FirstOrDefault((ushort)0);
        }

        if (newId != 0)
        {
             
            var ghost = new StaticTile(newId, o.Tile.X, o.Tile.Y, o.Tile.Z, 0);
            var ghostObject = new StaticObject(ghost);
            mapManager.StaticsManager.AddGhost(o, ghostObject);
        }
       
        _dir = contextDirection;
        
        var tile = selectedTile.Tile;
        if (tile.Z > maxZ)
        {
            selectedTile.Visible = false;
            var newTile = new LandTile(tile.Id, tile.X, tile.Y, maxZ);
            MapManager.GhostLandTiles[selectedTile] = new LandObject(newTile);
            MapManager.OnLandTileElevated(newTile, newTile.Z);
        }
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

    internal override void Draw()
    {
        int waterLevel = _z;
        if (ImGuiEx.DragInt("Water level", ref waterLevel, 1, sbyte.MinValue, sbyte.MaxValue))
        {
            _z = (sbyte)Math.Clamp(waterLevel, sbyte.MinValue, sbyte.MaxValue);
            mapManager.VirtualLayerZ = _z;
        }
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
}