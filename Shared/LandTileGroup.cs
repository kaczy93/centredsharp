//UOLib/UTiledata.pas
using static Shared.Tiledata;

namespace Shared; 

//TLandTileGroup
public class LandTileGroup : MulBlock {
    
    
    public LandTileGroup(Stream? stream, TileDataVersion version = TileDataVersion.Legacy) {
        if (stream != null) {
            using var reader = new BinaryReader(stream);
            Unknown = reader.ReadInt32();
            for (int i = 0; i < GroupSize; i++) {
                LandTiles[i] = new LandTileData(stream, version);
            }
        }
    }
    
    public int Unknown { get; set; }

    public LandTileData[] LandTiles = new LandTileData[GroupSize];

    public override int GetSize => LandTileGroupSize;
    
    public override MulBlock Clone() {
        LandTileGroup result = new LandTileGroup(null);
        result.Unknown = Unknown;
        for (int i = 0; i < GroupSize; i++) {
            result.LandTiles[i] = LandTiles[i];
        }

        return result;
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Unknown);
        for (int i = 0; i < GroupSize; i++) {
            LandTiles[i].Write(writer);
        }
    }
}