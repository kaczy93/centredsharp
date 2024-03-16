using System.Diagnostics.CodeAnalysis;

namespace CentrED.IO.Models;

public class LandBrush
{
    public string Name = "";
    public List<ushort> Tiles = new();
    public Dictionary<string, List<LandBrushTransition>> Transitions = new();

    public bool TryGetTransition(string name, Direction dir, [MaybeNullWhen(false)] out LandBrushTransition result)
    {
        if (Transitions.TryGetValue(name, out var transitions))
        {
            var matched = transitions
                                      .Where(lbt => lbt.Contains(dir))
                                      .GroupBy(lbt => lbt.Direction.Count())
                                      .MinBy(x => x.Key);
            if(matched != null)
            {
                var found = matched.ToArray();
                result = found[Random.Shared.Next(found.Length)];
                return true;
            }
            
        }
        result = null;
        return false;
    }
}

public class LandBrushTransition
{
    public ushort TileID;
    /*
     * This byte encodes fields of 3x3 grid of a transition from one brush to another
     * |7|0|1|
     * |6| |2|
     * |5|4|3|
     */
    public Direction Direction;

    public bool Contains(Direction dir) => Direction.Contains(dir);
}

[Flags]
public enum Direction : byte
{
    None = 0,
    North = 1 << 0,
    Right = 1 << 1,
    East = 1 << 2,
    Down = 1 << 3,
    South = 1 << 4,
    Left = 1 << 5,
    West = 1 << 6,
    Up = 1 << 7,
    All = 0xFF,
}

public static class DirectionHelper
{
    public static readonly Direction CornersMask = Direction.Up | Direction.Down | Direction.Left | Direction.Right;
    public static readonly Direction SideMask = Direction.North | Direction.South | Direction.East | Direction.West;

    public static bool Contains(this Direction dir, Direction other) => (dir & other) >= other;

    private static Direction Opposite(this Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.South,
            Direction.Right => Direction.Left,
            Direction.East => Direction.West,
            Direction.Down => Direction.Up,
            Direction.South => Direction.North,
            Direction.Left => Direction.Right,
            Direction.West => Direction.East,
            Direction.Up => Direction.Down,
            _ => dir
        };
    }

    public static Direction Reverse(this Direction dir)
    {
        var toAdd = Direction.None;
        var toRemove = Direction.None;
        foreach (var direction in Enum.GetValues<Direction>())
        {
            if (direction == Direction.None || direction == Direction.All)
                continue;
            if ((dir & direction) == 0)
                continue;
            toAdd |= direction.Opposite();
            toRemove |= direction;
        }

        dir |= toAdd;
        dir &= ~toRemove;

        return dir;
    }

    public static byte Count(this Direction dir)
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
}