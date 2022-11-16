//UOLib/UTiledata

namespace Shared;

//TTiledata
public abstract class Tiledata : MulBlock { //Todo
    public const int GroupSize = 32;
    public const int LandTileDataSize = 26;
    public const int LandTileGroupSize = 4 + 32 * LandTileDataSize;
    public const int StaticTileDataSize = 37;
    public const int StaticTileGroupSize = 4 + 32 * StaticTileDataSize;

    public static int GetTileDataOffset(int block) {
        if (block > 0x3FFF) {
            block = block - 0x4000;
            var group = block / 32;
            var tile = block % 32;
            return 512 * LandTileGroupSize + group * StaticTileGroupSize + 4 + tile * StaticTileDataSize;
        }
        else {
            var group = block / 32;
            var tile = block % 32;
            return group * LandTileGroupSize + 4 + tile * LandTileDataSize;
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

    protected void PopulateClone(Tiledata clone) {
        clone.version = version;
        clone.Flags = Flags;
        clone.TileName = TileName;
    }
}