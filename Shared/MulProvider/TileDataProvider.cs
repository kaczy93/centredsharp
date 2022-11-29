//MulProvider/UTileDataProvider.pas
namespace Shared.MulProvider; 

//TTiledataProvider
public class TileDataProvider : MulProvider<TileData> {
    
    public TileDataProvider(Stream data, bool readOnly = false) : base(data, readOnly) {
        InitArray();
    }
    
    public TileDataProvider(string dataPath, bool readOnly = false) : base(dataPath, readOnly) {
        InitArray();
    }

    protected LandTileData[] _landTiles = new LandTileData[0x4000];
    protected StaticTileData[] _staticTiles;
    protected uint _staticCount;

    protected void InitArray() {
        var version = Data.Length >= 3188736 ? TileDataVersion.HighSeas : TileDataVersion.Legacy;
        Data.Position = 0;
        //log.info("Loading 0x4000 LandTileData Entires");
        for (var i = 0; i < 0x4000; i++) {
            //In High Seas, the first header comes AFTER the unknown tile (for whatever reason).
            //Therefore special handling is required.
            if ((version == TileDataVersion.Legacy && i % 32 == 0) ||
                version >= TileDataVersion.HighSeas && (i == 1 || (i > 1 && i % 32 == 0))) {
                Data.Seek(4, SeekOrigin.Current);
            }

            LandTiles[i] = new LandTileData(Data, version);
        }

        _staticCount = (uint)(Data.Length - Data.Position) / TileData.StaticTileGroupSize * 32;
        //log.info($"Loading {StaticCount} StaticTiledata Entries");
        _staticTiles = new StaticTileData[StaticCount];
        for (var i = 0; i < StaticCount; i++) {
            if (i % 32 == 0) {
                Data.Seek(4, SeekOrigin.Current);
            }
            StaticTiles[i] = new StaticTileData(Data, version);
        }
    }

    protected override int CalculateOffset(int id) {
        return TileData.GetTileDataOffset(id);
    }

    protected override MulBlock GetData(int id, int offset) {
        if (id < 0x4000) {
            return LandTiles[id].Clone();
        }

        var result = StaticTiles[id - 0x4000].Clone();
        result.Id = id;
        result.OnChanged = OnChanged;
        result.OnFinished = OnFinished;
        return result;
    }

    protected override void SetData(int id, int offset, MulBlock block) {
        if (id >= 0x4000 + StaticCount) return;

        if (id < 0x4000) {
            LandTiles[id] = (LandTileData)block.Clone();
        }
        else {
            StaticTiles[id] = (StaticTileData)block.Clone();
        }

        if (!ReadOnly) {
            Data.Position = offset;
            block.Write(new BinaryWriter(Data));
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

    public override MulBlock GetBlock(int id) {
        return GetData(id, 0);
    }
    
    public LandTileData[] LandTiles => _landTiles;
    
    public StaticTileData[] StaticTiles => _staticTiles;
    
    public new TileData this[int index] {
        get => (TileData)GetBlock(index);
        set => SetBlock(index, value);
    }

    public uint StaticCount => _staticCount;
}