namespace CentrED;

public struct TileDataLand
{
    public TileDataLand(ulong flags, ushort textId, string name)
    {
        Flags = (TileDataFlag) flags;
        TexID = textId;
        Name = name;
    }

    public TileDataFlag Flags;
    public ushort TexID;
    public string Name;

    public bool IsWet => (Flags & TileDataFlag.Wet) != 0;
    public bool IsImpassable => (Flags & TileDataFlag.Impassable) != 0;
    public bool IsNoDiagonal => (Flags & TileDataFlag.NoDiagonal) != 0;
}