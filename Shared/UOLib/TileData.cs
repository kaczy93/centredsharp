namespace Shared;

public abstract class TileData : MulBlock { //Todo
    public const int GroupSize = 32;

    public static int LandTileDataSize(TileDataVersion version) =>
        version switch {
            TileDataVersion.HighSeas => 30,
            _ => 26
        };

    public static int LandTileGroupSize(TileDataVersion version) => 4 + 32 * LandTileDataSize(version);
    public static int StaticTileDataSize(TileDataVersion version) => version switch {
        TileDataVersion.HighSeas => 41,
        _ => 37
    };
    public static int StaticTileGroupSize(TileDataVersion version) => 4 + 32 * StaticTileDataSize(version);

    public static int GetTileDataOffset(TileDataVersion version, int block) {
        if (block > 0x3FFF) {
            block -= 0x4000;
            var group = block / 32;
            var tile = block % 32;
            return 512 * LandTileGroupSize(version) + group * StaticTileGroupSize(version) + 4 + tile * StaticTileDataSize(version);
        }
        else {
            var group = block / 32;
            var tile = block % 32;
            return group * LandTileGroupSize(version) + 4 + tile * LandTileDataSize(version);
        }
    }

    protected TileDataVersion version;
    
    public TiledataFlag Flags { get; set; }

    public string TileName { get; set; }

    protected void ReadFlags(BinaryReader? reader = null) {
        if (reader != null) {
            if (version == TileDataVersion.Legacy) {
                Flags = (TiledataFlag)reader.ReadUInt32();
            }
            else {
                Flags = (TiledataFlag)reader.ReadUInt64();
            }
        }
    }

    protected void WriteFlags(BinaryWriter writer) {
        writer.Write((ulong)Flags);
    }
}