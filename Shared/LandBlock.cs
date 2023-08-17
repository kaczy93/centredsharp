namespace CentrED;

public class LandBlock {
    public const int Size = 4 + 64 * LandTile.Size;
    public static LandBlock Empty => new() { Header = 0, Tiles = Enumerable.Repeat(LandTile.Empty, 64).ToArray() };
    
    public bool Changed { get; set; }
    public ushort X { get; }
    public ushort Y { get; }

    public LandTile[] Tiles { get; init; } = new LandTile[64];

    public LandBlock(ushort x = 0, ushort y = 0, BinaryReader? reader = null) {
        X = x;
        Y = y;
        if(reader != null) {
            Header = reader.ReadInt32();
            for (ushort iy = 0; iy < 8; iy++)
                for (ushort ix = 0; ix < 8; ix++)
                    Tiles[iy * 8 + ix] =
                        new LandTile(reader, this, (ushort)(x * 8 + ix), (ushort)(y * 8 + iy));
        }
        Changed = false;
    }

    public int Header { get; init; }

    public void Write(BinaryWriter writer) {
        writer.Write(Header);
        foreach (var tile in Tiles)
            tile.Write(writer);
    }
}