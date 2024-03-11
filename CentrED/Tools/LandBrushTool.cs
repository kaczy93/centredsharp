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
        UIManager.HuesWindow.Show = true;
    }


    private (int, int, Direction)[] offsets =
    {
        (-1, 0, Direction.Right), (0, -1, Direction.Left), (-1, -1, Direction.Down)
    };

    protected override void GhostApply(TileObject? o)
    {
        var defaultTransitionDirection = Direction.Up;

        //1. get current tile transistion
        //2. find new transition that will keep current transistion with addition of target direction (default is West)
        //  new transition should aim to add as little direction as possible, if ambiguous, use first.
        //3. Update adjacent tiles, that will get impacted
        // i.e. if adding West:
        //  x-1 should add North
        //  y-1 should add South
        //  x-1,y-1 should add East


        if (o is LandObject lo)
        {
            AddTransistion(lo, defaultTransitionDirection);
            foreach (var valueTuple in offsets)
            {
                //Should we pass ghost tile here for area operations?
                AddTransistion
                (
                    MapManager.LandTiles[lo.Tile.X + valueTuple.Item1, lo.Tile.Y + valueTuple.Item2],
                    valueTuple.Item3
                );
            }
        }
    }

    private void AddTransistion(LandObject lo, Direction direction)
    {
        var currentBrush = UIManager.LandBrushWindow.Selected;
        if (currentBrush == null)
            return;

        if (MapManager.tileLandBrushesNames.TryGetValue(lo.Tile.Id, out var tileLandBrushNames))
        {
            if (tileLandBrushNames.Count > 1)
            {
                Console.WriteLine
                    ($"More than one brush defined for {lo.Tile.Id}: {string.Join(',', tileLandBrushNames)}");
            }
            var (fromBrushName, toBrushName) = tileLandBrushNames[0];
            var fullTile = fromBrushName == toBrushName;
            var tileLandBrush = ProfileManager.ActiveProfile.LandBrush[fromBrushName];
            var newTileId = lo.Tile.Id;
            if (tileLandBrush.Transitions.TryGetValue(currentBrush.Name, out var transitions))
            {
                Direction targetTransition;
                if (tileLandBrush.Tiles.Contains(lo.Tile.Id))
                {
                    targetTransition = direction;
                }
                else
                {
                    var foundTransition = transitions.First(lbt => lbt.TileID == lo.Tile.Id);
                    targetTransition = foundTransition.Direction | direction;
                }
                var foundTransition1 = transitions.Where(lbt => lbt.Contains(targetTransition)).MinBy
                    (lbt => getSetBitCount(lbt.Direction));
                if (foundTransition1 != null)
                {
                    newTileId = foundTransition1.TileID;
                }
                else
                {
                    if (currentBrush.Transitions.TryGetValue(tileLandBrush.Name, out var transitions2))
                    {
                        Direction targetTransition2 = targetTransition.Reverse();
                        var foundTransition2 = transitions2.Where(lbt => lbt.Contains(targetTransition2)).MinBy
                            (lbt => getSetBitCount(lbt.Direction));
                        newTileId = foundTransition2 != null ?
                            foundTransition2.TileID :
                            currentBrush.Tiles[Random.Next
                                               (
                                                   currentBrush.Tiles.Count
                                               )]; //fallback to full tile of selected tilebrush
                    }
                }
            }
            else if (currentBrush.Transitions.TryGetValue(tileLandBrush.Name, out var transitions2))
            {
                Direction targetTransition;
                if (tileLandBrush.Tiles.Contains(lo.Tile.Id))
                {
                    targetTransition = direction;
                }
                else
                {
                    var foundTransition = transitions.First(lbt => lbt.TileID == lo.Tile.Id);
                    targetTransition = foundTransition.Direction | direction;
                }
                Direction targetTransition2 = direction.Reverse();
                var foundTransition2 = transitions2.Where(lbt => lbt.Contains(targetTransition2)).MinBy
                    (lbt => getSetBitCount(lbt.Direction));
                newTileId = foundTransition2 != null ?
                    foundTransition2.TileID :
                    currentBrush.Tiles[Random.Next
                                           (currentBrush.Tiles.Count)]; //fallback to full tile of selected tilebrush
            }
            {
            }
            if (newTileId != lo.Tile.Id)
            {
                lo.Visible = false;
                if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
                {
                    ghostTile.UpdateId(newTileId);
                }
                else
                {
                    var newTile = new LandTile(newTileId, lo.Tile.X, lo.Tile.Y, lo.Tile.Z);
                    MapManager.GhostLandTiles[lo] = new LandObject(newTile);
                }
            }
        }
    }

    private byte getSetBitCount(Direction dir)
    {
        byte count = 0;
        var value = (byte)dir;
        while (value != 0)
        {
            value = (byte)(value & (value - 1));
            count++;
        }
        return count;
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is LandObject lo)
        {
            //Clear this tile and all around
            lo.Reset();
            MapManager.GhostLandTiles.Remove(lo);
            foreach (var valueTuple in offsets)
            {
                var tile = MapManager.LandTiles[lo.Tile.X + valueTuple.Item1, lo.Tile.Y + valueTuple.Item2];
                tile.Reset();
                MapManager.GhostLandTiles.Remove(tile);
            }
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is LandObject lo)
        {
            //Apply to this tile and all around
            if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                o.Tile.Id = ghostTile.Tile.Id;
            }
            foreach (var valueTuple in offsets)
            {
                var tile = MapManager.LandTiles[lo.Tile.X + valueTuple.Item1, lo.Tile.Y + valueTuple.Item2];
                if (MapManager.GhostLandTiles.TryGetValue(tile, out var ghostTile2))
                {
                    tile.Tile.Id = ghostTile2.Tile.Id;
                }
            }
        }
    }
}