//UOLib/UTiledata.pas
using static Shared.Tiledata;

namespace Shared; 

//TStaticTileGroup
public class StaticTileGroup : MulBlock { //Todo: Land/Static TileGroup can share all the code

    public StaticTileGroup(BinaryReader? reader = null, TileDataVersion version = TileDataVersion.Legacy) {
        if (reader != null) {
            Unknown = reader.ReadInt32();
            for (int i = 0; i < GroupSize; i++) {
                StaticTiles[i] = new StaticTileData(reader, version);
            }
        }
    }

    public int Unknown { get; set; }

    public StaticTileData[] StaticTiles = new StaticTileData[32];

    public override int GetSize => StaticTileGroupSize;
    
    public override MulBlock Clone() {
        StaticTileGroup result = new StaticTileGroup();
        result.Unknown = Unknown;
        for (int i = 0; i < GroupSize; i++) {
            result.StaticTiles[i] = StaticTiles[i];
        }

        return result;
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Unknown);
        for (int i = 0; i < GroupSize; i++) {
            StaticTiles[i].Write(writer);
        }
    }
}