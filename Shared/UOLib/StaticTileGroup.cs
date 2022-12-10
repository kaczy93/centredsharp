using System.Text;
using static Shared.TileData;

namespace Shared; 

public class StaticTileGroup : MulBlock { //Todo: Land/Static TileGroup can share all the code

    public StaticTileGroup(Stream? data = null, TileDataVersion version = TileDataVersion.Legacy) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        Unknown = reader.ReadInt32();
        for (int i = 0; i < GroupSize; i++) {
            StaticTiles[i] = new StaticTileData(data, version);
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