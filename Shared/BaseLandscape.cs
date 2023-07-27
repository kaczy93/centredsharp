using System.Collections.ObjectModel;

namespace CentrED;

public delegate void MapChanged();
public delegate void BlockChanged(Block block);
public delegate void LandChanged(LandTile landTile);
public delegate void StaticChanged(StaticTile staticTile);


public abstract class BaseLandscape {
    public uint GetBlockId(ushort x, ushort y) {
        return (uint)(x / 8 * Height + y / 8);
    }
    public static byte GetTileId(ushort x, ushort y) {
        return (byte)((y & 0x7) * 8 + (x & 0x7));
    }
    
    public event MapChanged? MapChanged;
    public event BlockChanged? BlockUnloaded;
    public event BlockChanged? BlockLoaded;
    public event LandChanged? LandTileChanged;
    public event StaticChanged? StaticTileAdded;
    public event StaticChanged? StaticTileRemoved;
    public event StaticChanged? StaticTileElevated;
    public event StaticChanged? StaticTileHued;

    protected BaseLandscape(ushort width, ushort height) {
        Width = width;
        Height = height;
        CellWidth = (ushort)(width * 8);
        CellHeight = (ushort)(height * 8);
        BlockCache = new BlockCache {
            OnRemovedCachedObject = OnBlockReleased
        };
    }
    
    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }
    public readonly BlockCache BlockCache;
    
    protected void AssertBlockCoords(ushort x, ushort y) {
        if (x >= Width || y >= Height) 
            throw new ArgumentException($"Coords out of range. Size: {Width}x{Height}, Requested: {x},{y}");
    }
    
    public LandTile GetLandTile(ushort x, ushort y) {
        var block = GetLandBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.Tiles[GetTileId(x, y)];
    }
    
    public IEnumerable<StaticTile> GetStaticTiles(ushort x, ushort y) {
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.GetTiles(x, y);
    }
    
    public LandBlock GetLandBlock(ushort x, ushort y) {
        return GetBlock(x, y).LandBlock;
    }

    public StaticBlock GetStaticBlock(ushort x, ushort y) {
        return GetBlock(x, y).StaticBlock;
    }
    
    public Block GetBlock(ushort x, ushort y) {
        AssertBlockCoords(x, y);
        var result = BlockCache.Get(x, y);
        if (result == null) {
            result = LoadBlock(x, y);
            OnBlockLoaded(result);
        }
        return result;
    }

    public void OnMapChanged() {
        MapChanged?.Invoke();
    }

    public void OnBlockReleased(Block block) {
        BlockUnloaded?.Invoke(block);
        OnMapChanged();
    }

    public void OnBlockLoaded(Block block) {
        BlockLoaded?.Invoke(block);
        OnMapChanged();
    }

    public void OnLandChanged(LandTile landTile) {
        LandTileChanged?.Invoke(landTile);
        OnMapChanged();
    }

    public void OnStaticTileAdded(StaticTile staticTile) {
        StaticTileAdded?.Invoke(staticTile);
        OnMapChanged();
    }

    public void OnStaticTileRemoved(StaticTile staticTile) {
        StaticTileRemoved?.Invoke(staticTile);
        OnMapChanged();
    }

    public void OnStaticTileElevated(StaticTile staticTile) {
        StaticTileElevated?.Invoke(staticTile);
        OnMapChanged();
    }

    public void OnStaticTileHued(StaticTile staticTile) {
        StaticTileHued?.Invoke(staticTile);
        OnMapChanged();
    }

    protected abstract Block LoadBlock(ushort x, ushort y);
}