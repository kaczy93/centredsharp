//UOLib/UTiledata.pas

using System.Text;
using static Shared.TileData;

namespace Shared; 

//TLandTileGroup
public class LandTileGroup : MulBlock {
    
    
    public LandTileGroup(Stream? data = null, TileDataVersion version = TileDataVersion.Legacy) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        Unknown = reader.ReadInt32();
        for (int i = 0; i < GroupSize; i++) {
            LandTiles[i] = new LandTileData(data, version);
        }
    }
    
    public int Unknown { get; set; }

    public LandTileData[] LandTiles = new LandTileData[GroupSize];

    public override int GetSize => LandTileGroupSize;
    
    public override MulBlock Clone() {
        LandTileGroup result = new LandTileGroup();
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