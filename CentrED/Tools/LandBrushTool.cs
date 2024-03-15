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
        if (o is LandObject lo)
        {
            AddTransistion(lo, defaultTransitionDirection);
            foreach (var valueTuple in offsets)
            {
                var newX = lo.Tile.X + valueTuple.Item1;
                var newY = lo.Tile.Y + valueTuple.Item2;
                if (Client.IsValidX(newX) && Client.IsValidY(newY))
                {
                    var tile = MapManager.LandTiles[newX, newY];
                    if(tile != null)
                        AddTransistion(tile, valueTuple.Item3);
                }
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
            var targetTransition = direction;
            var found = false;
            if(fromBrushName != toBrushName )
            {
                var currentTransition = tileLandBrush.Transitions[toBrushName].First(lbt => lbt.TileID == lo.Tile.Id);
                if (currentBrush.Name == toBrushName && currentTransition.Contains(direction))
                {
                    found = true;
                }
                else if (currentBrush.Name == fromBrushName && (~currentTransition.Direction).Contains(direction))
                {
                    found = true;
                }
                    
            }
            if (!found && tileLandBrush.Transitions.TryGetValue(currentBrush.Name, out var transitions))
            {
                //TODO: Support multiple transitions
                if (!fullTile && currentBrush.Name == toBrushName)
                {
                    //Merge current transisiton with target direction
                    var foundTransition = transitions.First(lbt => lbt.TileID == lo.Tile.Id);
                    targetTransition = foundTransition.Direction | direction;
                }
                var foundTransition1 = transitions.Where(lbt => lbt.Contains(targetTransition)).MinBy
                    (lbt => getSetBitCount(lbt.Direction));
                if (foundTransition1 != null)
                {
                    found = true;
                    newTileId = foundTransition1.TileID;
                }
            }
            if (!found)
            {
                targetTransition = targetTransition.Reverse() & DirectionHelper.CornersMask;
                if (targetTransition != Direction.None && currentBrush.Transitions.TryGetValue(tileLandBrush.Name, out var transitions2))
                {
                    var foundTransition2 = transitions2.Where(lbt => lbt.Contains(targetTransition)).MinBy
                        (lbt => getSetBitCount(lbt.Direction));
                    if (foundTransition2 != null)
                        newTileId = foundTransition2.TileID;
                }
                else
                {
                    //fallback to full tile of selected brush
                    newTileId = currentBrush.Tiles[Random.Next(currentBrush.Tiles.Count)]; 
                }
                found = true;
            }
            if (found && newTileId != lo.Tile.Id)
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
                var newX = lo.Tile.X + valueTuple.Item1;
                var newY = lo.Tile.Y + valueTuple.Item2;
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
                var newX = lo.Tile.X + valueTuple.Item1;
                var newY = lo.Tile.Y + valueTuple.Item2;
                if (Client.IsValidX(newX) && Client.IsValidY(newY))
                {
                    var tile = MapManager.LandTiles[newX, newY];
                    if (MapManager.GhostLandTiles.TryGetValue(tile, out var ghostTile2))
                    {
                        tile.Tile.Id = ghostTile2.Tile.Id;
                    }
                }
            }
        }
    }
}