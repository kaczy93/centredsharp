namespace CentrED.IO.Models;

public class LandBrush
{
    public string Name = "";
    public List<ushort> Tiles = new();
    public Dictionary<string, List<LandBrushTransition>> Transitions = new();
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
}

[Flags]
public enum Direction : byte
{
    North = 1 << 0,
    Right = 1 << 1,
    East = 1 << 2,
    Down = 1 << 3,
    South = 1 << 4,
    Left = 1 << 5,
    West = 1 << 6,
    Up = 1 << 7,
}