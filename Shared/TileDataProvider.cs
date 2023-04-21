namespace CentrED; 

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

            LandTiles[i] = new LandTileData(Version, Reader);
        }

        StaticCount = (uint)((Stream.Length - Stream.Position) / StaticTileDataBlock.Size(Version) * 32);
        StaticTiles = new StaticTileData[StaticCount];
        for (var i = 0; i < StaticCount; i++) {
            if (i % 32 == 0) {
                Stream.Seek(4, SeekOrigin.Current); // skip header
            }
            StaticTiles[i] = new StaticTileData(Version, Reader){Id = i};
        }
    }
    
    public TileDataVersion Version { get; }
    
    public LandTileData[] LandTiles { get; } = new LandTileData[0x4000];

    public StaticTileData[] StaticTiles { get; }

    public uint StaticCount { get; }

    protected override int CalculateOffset(int block) {
        if (block > 0x3FFF) {
            block -= 0x4000;
            var group = block / 32;
            var tile = block % 32;
            return 512 * LandTileDataBlock.Size(Version) + group * StaticTileDataBlock.Size(Version) + 4 + tile * StaticTileData.Size(Version);
        }
        else {
            var group = block / 32;
            var tile = block % 32;
            return group * LandTileDataBlock.Size(Version) + 4 + tile * LandTileData.Size(Version);
        }
    }

    protected override TileData GetData(int id, int offset) {
        if (id < 0x4000) {
            return LandTiles[id];
        }

        var result = StaticTiles[id - 0x4000];
        result.Id = id;
        return result;
    }

    protected override void SetData(int id, int offset, TileData block) {
        if (id >= 0x4000 + StaticCount) return;

        if (id < 0x4000) {
            LandTiles[id] = (LandTileData)block;
        }
        else {
            StaticTiles[id] = (StaticTileData)block;
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
}