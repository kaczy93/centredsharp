using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class LandBrushTool : BaseTool
{
    public override string Name => App.Lang.Get(LAND_BRUSH_WINDOW);
    public override Keys Shortcut => Keys.F7;

    private bool _fixedZ = false;
    private int _fixedHeightZ = 0, _randomZ = 0;
    private LandBrushManagerWindow _managerWindow => UIManager.GetWindow<LandBrushManagerWindow>();

    internal override void Draw()
    {
        base.Draw();
        if (!App.Client.Running)
        {
            ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            return;
        }
        
        _managerWindow.LandBrushCombo(ref App.LandBrush.ActiveName);
        ImGui.Checkbox(App.Lang.Get(FIXED_Z), ref _fixedZ);
        if (_fixedZ)
        {
            ImGui.SameLine();
            ImGuiEx.DragInt("##FixedHeightZ", ref _fixedHeightZ, 1, ZRange.Min, ZRange.Max);
        }
        ImGuiEx.DragInt(App.Lang.Get(ADD_RANDOM_Z), ref _randomZ, 1, 0, 127);
    }

    private short CalculateNewZ(short height)
    {
        int result = height;
        if (_fixedZ)
        {
            result = _fixedHeightZ;
        }

        if (_randomZ > 0)
        {
            result += (short)Random.Shared.Next(0, _randomZ);
        }

        return ZRange.Clamp(result);
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
        if(App.LandBrush.Active == null)
            return result;

        var currentTileId = lo.Tile.Id;
        if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghost))
        {
            currentTileId = ghost.Tile.Id;
        }
        var newTileId = currentTileId;
        LandBrushTransition? targetTransition = null;

        if (App.LandBrush.TileToLandBrushNames.TryGetValue(currentTileId, out var tileLandBrushNames))
        {
            //Current tile is defined in at least one brush
            if (!TryGetBestMatchingLandBrush(tileLandBrushNames, currentTileId, direction, out var fromBrushName, out var toBrushName))
            {
                if (App.MapManager.DebugLogging)
                {
                    Console.WriteLine($"More than one matching brush for {currentTileId},{direction}: {string.Join(',', tileLandBrushNames)}");
                }
            }
            
            var currentTileBrushName = fromBrushName;
            var currentTileBrush =  App.LandBrush.Brushes[currentTileBrushName];
            var targetDirection = direction;
            
            if (fromBrushName == toBrushName)
            {
                //Current tile is full tile
                if (currentTileBrush.TryGetMinimalTransition(App.LandBrush.ActiveName, direction, out targetTransition))
                {
                    result = targetTransition.Direction;
                }
            }
            else 
            {
                //Current tile is transition tile
                LandBrushTransition currentTransition = currentTileBrush.To[toBrushName].First(lbt => lbt.TileID == currentTileId);
                if (App.LandBrush.ActiveName == toBrushName)
                {
                    //Our active brush is to-brush, we will try adding direction to current transition
                    if (currentTransition.Contains(direction))
                    {
                        //Current transition has direction that we want, nothing to do
                        targetDirection = currentTransition.Direction;
                    }
                    else
                    {
                        //We have to look for transition tile that will merge current transition and our direciton
                        targetDirection = currentTransition.Direction | direction;
                    }
                }
                else if (App.LandBrush.ActiveName == fromBrushName)
                {
                    //Our active brush is from-brush, we will try subtracting direction from current transition
                    currentTileBrushName = toBrushName;
                    currentTileBrush =  App.LandBrush.Brushes[currentTileBrushName];
                    if ((~currentTransition.Direction).Contains(direction))
                    {
                        //Current transition has direction that we want, nothing to do
                        targetDirection = ~currentTransition.Direction;
                    }
                    else
                    {
                        //We have to look for transition tile in the opposite direction with inversions
                        targetDirection = ~(currentTransition.Direction & ~direction);
                    }
                }
                //Lets try to find transition from current tile brush to our active brush
                if (currentTileBrush.TryGetMinimalTransition(App.LandBrush.ActiveName, targetDirection, out targetTransition))
                {
                    result = targetTransition.Direction;
                }
                if (targetTransition == null)
                {
                    //We didn't find a proper transition, we will look for reversed transition
                    //Let's look only for either corners or sides to simplify the search
                    var mask = (direction & DirectionHelper.CornersMask) > Direction.None ?
                        DirectionHelper.CornersMask :
                        DirectionHelper.SideMask;
                    //We have to reverse the direction, because we are looking other way around
                    var reversedTransition = targetDirection.Reverse() & mask;

                    if (reversedTransition != Direction.None)
                    {
                        if (App.LandBrush.Active.TryGetMinimalTransition(currentTileBrushName, reversedTransition, out targetTransition))
                        {
                            // We have inverse the transition direction to get real result
                            result = ~targetTransition.Direction;
                        }
                    }
                }
            }
        }
        if (targetTransition != null)
        {
            //Found a proper transition tile
            newTileId = targetTransition.TileID;
        }
        else
        {
            if (App.LandBrush.Active.Tiles.Count > 0)
            {
                //fallback to full tile of active brush
                newTileId = App.LandBrush.Active.Tiles[Random.Shared.Next(App.LandBrush.Active.Tiles.Count)];
                result = Direction.All;
            }
            else
            {
                //No tiles in active brush, do nothing
                newTileId = currentTileId;
                result = Direction.None;
            }
        }
        if (newTileId != currentTileId)
        {
            //We have new id so apply the change
            lo.Visible = false;
            if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                ghostTile.LandTile.Id = newTileId; // Very dirty way to update in area mode
                ghostTile.UpdateId(newTileId);
            }
            else
            {
                var newTile = new LandTile(newTileId, lo.Tile.X, lo.Tile.Y, CalculateNewZ(lo.Tile.Z));
                MapManager.GhostLandTiles[lo] = new LandObject(newTile);
            }
        }
        if (!result.Contains(direction))
        {
            //If we got here, `result` should always be at least initial direction
            Console.WriteLine($"[Error][LandBrush] result doesn't contain direction {lo}, {direction}: {result}");;
            result |= direction;
        }
        return result;
    }

    private bool 
        TryGetBestMatchingLandBrush
    (
        List<(string, string)> tileLandBrushNames,
        ushort tileId,
        Direction targetDirection,
        out string fromBrushName,
        out string toBrushName
    )
    {
        //This whole function is a total mess, can we simplify it?
        bool result = true;
        (string, string) resultNames;

        //Lets count directions in each brush
        var minDirectionCount = int.MaxValue;
        var brushesWithDirection = new List<(string, string, Direction, int)>();
        foreach (var (from, to) in tileLandBrushNames)
        {
            var brush =  App.LandBrush.Brushes[from];
            Direction direction;
            if (from == to)
            {
                //Full tile
                direction = Direction.All;
            }
            else
            {
                direction = brush.To[to].First(lbt => lbt.TileID == tileId).Direction;
            }
            var directionCount = direction.Count();
            if(directionCount < minDirectionCount)
            {
                minDirectionCount = directionCount;
            }
            brushesWithDirection.Add((from, to, direction, directionCount));
        }

        var minimalBrushes = brushesWithDirection
                     .Where(x => x.Item4 == minDirectionCount)
                     .ToList();
        
        if (minimalBrushes.Count > 1)
        {
            // Get the brush that has transition in the target direction
            if (minimalBrushes.Exists(x => x.Item3.Contains(targetDirection)))
            {
                var bestMatch = minimalBrushes.First(x => x.Item3.Contains(targetDirection));
                resultNames = (bestMatch.Item1, bestMatch.Item2);
            }
            else
            {
                var bestMatch = minimalBrushes.First();
                resultNames = (bestMatch.Item1, bestMatch.Item2);
                result = false;
            }
        }
        else
        {
            resultNames = (minimalBrushes[0].Item1, minimalBrushes[0].Item2);
        }
        fromBrushName = resultNames.Item1;
        toBrushName = resultNames.Item2;

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

    protected override void InternalApply(TileObject? o)
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
                            tile.LandTile.ReplaceLand(ghostTile.Tile.Id, ghostTile.Tile.Z);
                        }
                    }
                }
            }
        }
    }

    public override void GrabZ(short z)
    {
        _fixedZ = true;
        _fixedHeightZ = z;
    }
}