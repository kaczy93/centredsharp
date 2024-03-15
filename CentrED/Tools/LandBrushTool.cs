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
                    if (tile != null)
                    {
                        AddTransistion(tile, valueTuple.Item3);
                    }
                }
            }
        }
    }

    private void AddTransistion(LandObject lo, Direction direction)
    {
        var currentBrush = UIManager.LandBrushWindow.Selected;
        if (currentBrush == null)
            return;
        
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

            if (fromBrushName != toBrushName)
            {
                var currentTransition = tileLandBrush.Transitions[toBrushName].First(lbt => lbt.TileID == currentId);
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
                    if((~currentTransition.Direction).Contains(direction))
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
                tileLandBrush.TryGetTransition(currentBrush.Name, targetTransition, out t);
            }
            if (t == null)
            {
                var mask = (direction & DirectionHelper.CornersMask) > Direction.None ?
                    DirectionHelper.CornersMask :
                    DirectionHelper.SideMask;
                targetTransition = targetTransition.Reverse() & mask;
                if (targetTransition != Direction.None)
                {
                    currentBrush.TryGetTransition(tileLandBrush.Name, targetTransition, out t);
                }
                if (t == null)
                {
                    //fallback to full tile of selected brush
                    newTileId = currentBrush.Tiles[Random.Next(currentBrush.Tiles.Count)];
                }
            }
            if (t != null)
            {
                newTileId = t.TileID;
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