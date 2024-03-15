using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CentrED.Network;

namespace CentrED;

public delegate void MapChanged();
public delegate void BlockChanged(Block block);
public delegate void LandReplaced(LandTile landTile, ushort newId);
public delegate void LandElevated(LandTile landTile, sbyte newZ);
public delegate void StaticChanged(StaticTile staticTile);
public delegate void StaticReplaced(StaticTile staticTile, ushort newId);
public delegate void StaticMoved(StaticTile staticTile, ushort newX, ushort newY);
public delegate void StaticElevated(StaticTile staticTile, sbyte newZ);
public delegate void StaticHued(StaticTile staticTile, ushort newHue);

public abstract class BaseLandscape
{
    public uint GetBlockId(ushort x, ushort y)
    {
        return (uint)(x / 8 * Height + y / 8);
    }

    protected BaseLandscape(ushort width, ushort height)
    {
        Width = width;
        Height = height;
        CellWidth = (ushort)(width * 8);
        CellHeight = (ushort)(height * 8);
        BlockCache = new BlockCache();
        BlockCache.OnRemovedItem = OnBlockReleased;
    }

    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }
    public readonly BlockCache BlockCache;

    protected void AssertBlockCoords(ushort x, ushort y)
    {
        if (x >= Width || y >= Height)
            throw new ArgumentException($"Coords out of range. Size: {Width}x{Height}, Requested: {x},{y}");
    }

    public LandTile GetLandTile(ushort x, ushort y)
    {
        var block = GetLandBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.Tiles[LandBlock.GetTileId(x, y)];
    }

    public IEnumerable<StaticTile> GetStaticTiles(ushort x, ushort y)
    {
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.GetTiles(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LandBlock GetLandBlock(ushort x, ushort y)
    {
        return GetBlock(x, y).LandBlock;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StaticBlock GetStaticBlock(ushort x, ushort y)
    {
        return GetBlock(x, y).StaticBlock;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StaticBlock GetStaticBlock(StaticInfo staticInfo)
    {
        return GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
    }

    public Block GetBlock(ushort x, ushort y)
    {
        AssertBlockCoords(x, y);
        var result = BlockCache.Get(Block.Id(x, y));
        if (result == null)
        {
            result = LoadBlock(x, y);
        }
        return result;
    }

    public bool TryGetLandTile(ushort x, ushort y, [MaybeNullWhen(false)] out LandTile landTile)
    {
        if(TryGetLandBlock((ushort)(x / 8), (ushort)(y / 8), out var landBlock))
        {
            landTile = landBlock.Tiles[LandBlock.GetTileId(x, y)];
            return true;
        }
        landTile = default;
        return false;
    }
    
    public bool TryGetStaticTiles(ushort x, ushort y, [MaybeNullWhen(false)] out IEnumerable<StaticTile> staticTiles)
    {
        if(TryGetStaticBlock((ushort)(x / 8), (ushort)(y / 8), out var staticBlock))
        {
            staticTiles = staticBlock.GetTiles(x, y);
            return true;
        }
        staticTiles = default;
        return false;
    }
    
    public bool TryGetLandBlock(ushort x, ushort y, [MaybeNullWhen(false)] out LandBlock landBlock)
    {
        AssertBlockCoords(x, y);
        if (TryGetBlock(x, y, out var block))
        {
            landBlock = block.LandBlock;
            return true;
        }
        landBlock = default;
        return false;
    }
    
    public bool TryGetStaticBlock(ushort x, ushort y, [MaybeNullWhen(false)] out StaticBlock staticBlock)
    {
        AssertBlockCoords(x, y);
        if (TryGetBlock(x, y, out var block))
        {
            staticBlock = block.StaticBlock;
            return true;
        }
        staticBlock = default;
        return false;
    }
    
    public bool TryGetBlock(ushort x, ushort y, [MaybeNullWhen(false)] out Block block)
    {
        AssertBlockCoords(x, y);
        block = BlockCache.Get(Block.Id(x, y));
        return block != null;
    }

    public void AddTile(StaticTile tile)
    {
        OnStaticTileAdded(tile);
    }

    public void RemoveTile(StaticTile tile)
    {
        OnStaticTileRemoved(tile);
    }

    protected void InternalSetLandId(LandTile tile, ushort newId)
    {
        tile._id = newId;
        tile.GhostId = null;
        tile.Block?.OnChanged();
    }

    protected void InternalSetLandZ(LandTile tile, sbyte newZ)
    {
        tile._z = newZ;
        tile.Block?.OnChanged();
    }

    protected void InternalAddStatic(StaticBlock block, StaticTile tile)
    {
        block.AddTileInternal(tile);
        block.OnChanged();
    }

    protected bool InternalRemoveStatic(StaticBlock block, StaticTile tile)
    {
        var result = block.RemoveTileInternal(tile);
        block.OnChanged();
        return result;
    }

    protected void InternalSetStaticId(StaticTile tile, ushort newId)
    {
        tile._id = newId;
        tile.Block?.OnChanged();
    }

    protected void InternalSetStaticPos(StaticTile tile, ushort newX, ushort newY)
    {
        tile._x = newX;
        tile._y = newY;
        tile.LocalX = (byte)(newX & 0x7);
        tile.LocalY = (byte)(newY & 0x7);
        tile.Block?.OnChanged();
    }

    protected void InternalSetStaticZ(StaticTile tile, sbyte newZ)
    {
        tile._z = newZ;
        tile.Block?.OnChanged();
    }

    protected void InternalSetStaticHue(StaticTile tile, ushort newHue)
    {
        tile._hue = newHue;
        tile.Block?.OnChanged();
    }

    public event MapChanged? MapChanged;
    public event BlockChanged? BlockUnloaded;
    public event BlockChanged? BlockLoaded;
    public event LandReplaced? LandTileReplaced;
    public event LandElevated? LandTileElevated;
    public event StaticChanged? StaticTileAdded;
    public event StaticChanged? StaticTileRemoved;
    public event StaticReplaced? StaticTileReplaced;
    public event StaticMoved? StaticTileMoved;
    public event StaticElevated? StaticTileElevated;
    public event StaticHued? StaticTileHued;

    public void OnMapChanged()
    {
        MapChanged?.Invoke();
    }

    public void OnBlockReleased(Block block)
    {
        BlockUnloaded?.Invoke(block);
        OnMapChanged();
    }

    public void OnBlockLoaded(Block block)
    {
        BlockLoaded?.Invoke(block);
        OnMapChanged();
    }

    public void OnLandReplaced(LandTile landTile, ushort newId)
    {
        LandTileReplaced?.Invoke(landTile, newId);
        OnMapChanged();
    }

    public void OnLandElevated(LandTile landTile, sbyte newZ)
    {
        LandTileElevated?.Invoke(landTile, newZ);
        OnMapChanged();
    }

    public void OnStaticTileAdded(StaticTile staticTile)
    {
        StaticTileAdded?.Invoke(staticTile);
        OnMapChanged();
    }

    public void OnStaticTileRemoved(StaticTile staticTile)
    {
        StaticTileRemoved?.Invoke(staticTile);
        OnMapChanged();
    }

    public void OnStaticTileReplaced(StaticTile staticTile, ushort newId)
    {
        StaticTileReplaced?.Invoke(staticTile, newId);
        OnMapChanged();
    }

    public void OnStaticTileMoved(StaticTile staticTile, ushort newX, ushort newY)
    {
        StaticTileMoved?.Invoke(staticTile, newX, newY);
        OnMapChanged();
    }

    public void OnStaticTileElevated(StaticTile staticTile, sbyte newZ)
    {
        StaticTileElevated?.Invoke(staticTile, newZ);
        OnMapChanged();
    }

    public void OnStaticTileHued(StaticTile staticTile, ushort newHue)
    {
        StaticTileHued?.Invoke(staticTile, newHue);
        OnMapChanged();
    }

    protected abstract Block LoadBlock(ushort x, ushort y);
}