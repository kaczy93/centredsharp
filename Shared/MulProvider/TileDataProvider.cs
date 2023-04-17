namespace Shared.MulProvider; 

public class TileDataProvider : MulProvider<TileData> {
    
    public TileDataProvider(FileStream stream, bool readOnly = false) : base(stream, readOnly) {
        Version = Stream.Length >= 3188736 ? TileDataVersion.HighSeas : TileDataVersion.Legacy;
        Stream.Position = 0;
        for (var i = 0; i < 0x4000; i++) {
            //In High Seas, the first header comes AFTER the unknown tile (for whatever reason).
            //Therefore special handling is required.
            if ((Version == TileDataVersion.Legacy && i % 32 == 0) ||
                Version >= TileDataVersion.HighSeas && (i == 1 || (i > 1 && i % 32 == 0))) {
                Stream.Seek(4, SeekOrigin.Current);
            }

            LandTiles[i] = new LandTileData(Reader, Version);
        }

        _staticCount = (uint)((Stream.Length - Stream.Position) / TileData.StaticTileGroupSize(Version) * 32);
        _staticTiles = new StaticTileData[StaticCount];
        for (var i = 0; i < StaticCount; i++) {
            if (i % 32 == 0) {
                Stream.Seek(4, SeekOrigin.Current);
            }
            StaticTiles[i] = new StaticTileData(Reader, Version);
        }
    }
    
    public TileDataVersion Version { get; }
    protected LandTileData[] _landTiles = new LandTileData[0x4000];
    protected StaticTileData[] _staticTiles;
    protected uint _staticCount;

    protected override int CalculateOffset(int id) {
        return TileData.GetTileDataOffset(Version, id);
    }

    protected override TileData GetData(int id, int offset) {
        if (id < 0x4000) {
            return LandTiles[id].Clone();
        }

        var result = StaticTiles[id - 0x4000].Clone();
        result.Id = id;
        return result;
    }

    protected override void SetData(int id, int offset, TileData block) {
        if (id >= 0x4000 + StaticCount) return;

        if (id < 0x4000) {
            LandTiles[id] = ((LandTileData)block).Clone();
        }
        else {
            StaticTiles[id] = ((StaticTileData)block).Clone();
        }

        if (!ReadOnly) {
            Stream.Position = offset;
            block.Write(Writer);
        }
    }

    protected TileData GetTileData(int id) {
        if (id < 0x4000) {
            return LandTiles[id];
        }
        else {
            return StaticTiles[id - 0x4000];
        }
    }

    public override TileData GetBlock(int id) {
        return GetData(id, 0);
    }
    
    public LandTileData[] LandTiles => _landTiles;
    
    public StaticTileData[] StaticTiles => _staticTiles;
    
    public uint StaticCount => _staticCount;
}