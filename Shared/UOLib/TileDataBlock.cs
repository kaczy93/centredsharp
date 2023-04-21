namespace Shared;

public sealed class LandTileDataBlock : TileDataBlock<LandTileData> {
    public static int Size(TileDataVersion version) => 4 + BlockSize * LandTileData.Size(version);
    
    public LandTileDataBlock(TileDataVersion version, BinaryReader? reader = null) : base(version, reader) {
        for (var i = 0; i < BlockSize; i++) {
            Tiles[i] = new LandTileData(version, reader);
        }
    }
}

public sealed class StaticTileDataBlock : TileDataBlock<StaticTileData> {
    public static int Size(TileDataVersion version) => 4 + BlockSize * StaticTileData.Size(version);
    
    public StaticTileDataBlock(TileDataVersion version, BinaryReader? reader = null) : base(version, reader) {
        for (var i = 0; i < BlockSize; i++) {
            Tiles[i] = new StaticTileData(version, reader);
        }
    }
}

public abstract class TileDataBlock<T> : MulEntry where T : TileData {
    public const int BlockSize = 32;
    private readonly TileDataVersion _version;
    
    public TileDataBlock(TileDataVersion version, BinaryReader? reader = null) {
        _version = version;
        if (reader == null) return;
        
        Header = reader.ReadInt32();
    }

    public int Header { get; }

    public T[] Tiles = new T[BlockSize];

    public override void Write(BinaryWriter writer) {
        writer.Write(Header);
        for (int i = 0; i < BlockSize; i++) {
            Tiles[i].Write(writer);
        }
    }
}