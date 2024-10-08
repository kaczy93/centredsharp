using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CentrED.IO.Models;

public class LandBrush
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    public string Name = "";
    public List<ushort> Tiles = new();
    public Dictionary<string, List<LandBrushTransition>> Transitions = new();

    public bool TryGetMinimalTransition(string name, Direction dir, [MaybeNullWhen(false)] out LandBrushTransition result)
    {
        if (Transitions.TryGetValue(name, out var transitions))
        {
            var matched = transitions.Where(lbt => lbt.Contains(dir)).GroupBy(lbt => lbt.Direction.Count()).MinBy
                (x => x.Key);
            if (matched != null)
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
    public LandBrushTransition(){}

    public LandBrushTransition(ushort tileId)
    {
        TileID = tileId;
        Direction = Direction.None;
    }
    
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

    public static Direction Prev(this Direction dir)
    {
        var newVal = (byte)((byte)dir >> 1);
        if (newVal == 0)
        {
            newVal = 1 << 7;
        }
        return (Direction)newVal;
    }

    public static Direction Next(this Direction dir)
    {
        var newVal = (byte)((byte)dir << 1);
        if (newVal == 0)
        {
            newVal = 1;
        }
        return (Direction)newVal;
    }

    public static Direction Opposite(this Direction dir)
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

    public static (sbyte, sbyte) Offset(this Direction dir)
    {
        return dir switch
        {
            Direction.North => (0, -1),
            Direction.Right => (1, -1),
            Direction.East => (1, 0),
            Direction.Down => (1, 1),
            Direction.South => (0, 1),
            Direction.Left => (-1, 1),
            Direction.West => (-1, 0),
            Direction.Up => (-1, -1),
        };
    }

    // Reverse the direction 
    // This means transform North to South, or East|Down to West|Up
    public static Direction Reverse(this Direction dir)
    {
        var toAdd = Direction.None;
        var toRemove = Direction.None;
        foreach (var direction in Enum.GetValues<Direction>())
        {
            if (direction == Direction.None || direction == Direction.All)
                continue;
            if (!dir.HasFlag(direction))
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