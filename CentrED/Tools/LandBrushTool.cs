using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class LandBrushTool : BaseTool
{
    public override string Name => "LandBrush";
    public override Keys Shortcut => Keys.F7;

    public override void OnActivated(TileObject? o)
    {
        UIManager.LandBrushWindow.Show = true;
    }

    protected override void GhostApply(TileObject? o)
    {
        var defaultTransitionDirection = Direction.Up;
        if (o is LandObject lo)
        {
            var direction = AddTransistion(lo, defaultTransitionDirection);
            var offsets = CalculateOffsets(direction);
            foreach (var valueTuple in offsets)
            {
                var newX = lo.Tile.X + valueTuple.Key.Item1;
                var newY = lo.Tile.Y + valueTuple.Key.Item2;
                if (Client.IsValidX(newX) && Client.IsValidY(newY))
                {
                    var tile = MapManager.LandTiles[newX, newY];
                    if (tile != null)
                    {
                        AddTransistion(tile, valueTuple.Value);
                    }
                }
            }
        }
    }

    private Dictionary<(int, int), Direction> CalculateOffsets(Direction dir)
    {
        var result = new Dictionary<(int, int), Direction>();
        if (dir == Direction.None)
        {
            return result;
        }
        foreach (var value in Enum.GetValues<Direction>())
        {
            if (value == Direction.None || value == Direction.All)
                continue;
            if (!dir.HasFlag(value))
                continue;

            var opposite = value.Opposite();

            if ((value & DirectionHelper.SideMask) > Direction.None)
            {
                //TODO: Sides are not working well yet
                // var offset = value.Offset();
                // result.TryGetValue(offset, out var d);
                // result[offset] = d | opposite;
            }
            else
            {
                var offset = value.Offset();
                result.TryGetValue(offset, out var d);
                result[offset] = d | value.Opposite();

                var offset2 = value.Prev().Offset();
                result.TryGetValue(offset2, out var d2);
                result[offset2] = d2 | value.Next().Next();

                var offset3 = value.Next().Offset();
                result.TryGetValue(offset3, out var d3);
                result[offset3] = d3 | value.Prev().Prev();
            }
        }
        return result;
    }

    private Direction AddTransistion(LandObject lo, Direction direction)
    {
        Direction result = Direction.None;
        var currentBrush = UIManager.LandBrushWindow.Selected;
        if (currentBrush == null)
            return result;

        var currentId = lo.Tile.Id;
        if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghost))
        {
            currentId = ghost.Tile.Id;
        }

        if (MapManager.tileLandBrushesNames.TryGetValue(currentId, out var tileLandBrushNames))
        {
            if (tileLandBrushNames.Count > 1)
            {
                Console.WriteLine
                    ($"More than one brush defined for {currentId}: {string.Join(',', tileLandBrushNames)}");
            }
            var (fromBrushName, toBrushName) = tileLandBrushNames[0];
            var tileLandBrush = ProfileManager.ActiveProfile.LandBrush[fromBrushName];

            var newTileId = currentId;
            var targetTransition = direction;
            LandBrushTransition? t = null;
            LandBrushTransition? currentTransition = null;

            if (fromBrushName != toBrushName)
            {
                currentTransition = tileLandBrush.Transitions[toBrushName].First(lbt => lbt.TileID == currentId);
                if (currentBrush.Name == toBrushName)
                {
                    if (currentTransition.Contains(direction))
                    {
                        t = currentTransition;
                    }
                    else
                    {
                        targetTransition = currentTransition.Direction | direction;
                    }
                }
                else if (currentBrush.Name == fromBrushName)
                {
                    tileLandBrush = ProfileManager.ActiveProfile.LandBrush[toBrushName];
                    if ((~currentTransition.Direction).Contains(direction))
                    {
                        t = currentTransition;
                    }
                    else
                    {
                        targetTransition = ~(currentTransition.Direction & ~direction);
                    }
                }
            }
            if (t == null)
            {
                if (tileLandBrush.TryGetTransition(currentBrush.Name, targetTransition, out t))
                {
                    result = ~(currentTransition?.Direction ?? Direction.None) & t.Direction;
                }
            }
            if (t == null)
            {
                var mask = (direction & DirectionHelper.CornersMask) > Direction.None ?
                    DirectionHelper.CornersMask :
                    DirectionHelper.SideMask;
                var revresedTransition = targetTransition.Reverse() & mask;

                if (revresedTransition != Direction.None)
                {
                    if (currentBrush.TryGetTransition(tileLandBrush.Name, revresedTransition, out t))
                    {
                        result = ~(currentTransition?.Direction ?? Direction.None) & ~t.Direction;
                    }
                }
                else //fallback to full tile of selected brush
                {
                    newTileId = currentBrush.Tiles[Random.Next(currentBrush.Tiles.Count)];
                    result = Direction.All;
                }
            }
            if (t != null)
            {
                newTileId = t.TileID;
                result &= ~(currentTransition?.Direction ?? Direction.None);
            }
            if (newTileId != currentId)
            {
                lo.Visible = false;
                if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
                {
                    ghostTile.LandTile.Id = newTileId; // Very dirty way to update in area mode
                    ghostTile.UpdateId(newTileId);
                }
                else
                {
                    var newTile = new LandTile(newTileId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
                    MapManager.GhostLandTiles[lo] = new LandObject(newTile);
                }
            }
        }
        if (result == Direction.None)
        {
            //Result should always be at least initial direction to enable fixing exisiting tiles
            result = direction;
        }
        return result;
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is LandObject lo)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var newX = lo.Tile.X + x;
                    var newY = lo.Tile.Y + y;
                    if (Client.IsValidX(newX) && Client.IsValidY(newY))
                    {
                        var tile = MapManager.LandTiles[newX, newY];
                        if (tile != null)
                        {
                            tile.Reset();
                            MapManager.GhostLandTiles.Remove(tile);
                        }
                    }
                }
            }
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is LandObject lo)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var newX = lo.Tile.X + x;
                    var newY = lo.Tile.Y + y;
                    if (Client.IsValidX(newX) && Client.IsValidY(newY))
                    {
                        var tile = MapManager.LandTiles[newX, newY];
                        if (MapManager.GhostLandTiles.TryGetValue(tile, out var ghostTile))
                        {
                            tile.Tile.Id = ghostTile.Tile.Id;
                        }
                    }
                }
            }
        }
    }
}