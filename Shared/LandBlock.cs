namespace CentrED;

public class LandBlock {
    public const int SIZE = 4 + 64 * LandTile.Size;
    public static LandBlock Empty(BaseLandscape landscape) => new(landscape) { _header = 0, Tiles = Enumerable.Repeat(LandTile.Empty, 64).ToArray() };
    
    public BaseLandscape Landscape { get; }
    public bool Changed { get; set; }
    public ushort X { get; }
    public ushort Y { get; }

    public LandTile[] Tiles { get; private init; } = new LandTile[64];

    public LandBlock(BaseLandscape landscape, ushort x = 0, ushort y = 0, BinaryReader? reader = null) {
        Landscape = landscape;
        X = x;
        Y = y;
        if(reader != null) {
            _header = reader.ReadInt32();
            for (ushort iy = 0; iy < 8; iy++)
                for (ushort ix = 0; ix < 8; ix++)
                    Tiles[iy * 8 + ix] =
                        new LandTile(reader, this, (ushort)(x * 8 + ix), (ushort)(y * 8 + iy));
        }
        Changed = false;
    }

    private int _header;

    public void OnChanged() {
        Changed = true;
    }

    public void Write(BinaryWriter writer) {
        writer.Write(_header);
        foreach (var tile in Tiles)
            tile.Write(writer);
    }
}