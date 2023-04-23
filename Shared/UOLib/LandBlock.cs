namespace Shared;

public class LandBlock : WorldBlock {
    public const int Size = 4 + 64 * LandTile.Size;

    public readonly LandTile[] Tiles = new LandTile[64];

    public LandBlock(BinaryReader? reader = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        if(reader != null) {
            Header = reader.ReadInt32();
            for (ushort iy = 0; iy < 8; iy++)
                for (ushort ix = 0; ix < 8; ix++)
                    Tiles[iy * 8 + ix] =
                        new LandTile(this, reader, (ushort)(x * 8 + ix), (ushort)(y * 8 + iy));
        }
        Changed = false;
    }

    public int Header { get; init; }

    public override void Write(BinaryWriter writer) {
        writer.Write(Header);
        lock (Tiles) {
            foreach (var tile in Tiles)
                tile.Write(writer);
        }
    }
}