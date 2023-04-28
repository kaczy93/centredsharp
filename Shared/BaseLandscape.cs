﻿using System.Collections.ObjectModel;

namespace CentrED; 

public abstract class BaseLandscape {
    public static ushort GetTileId(ushort x, ushort y) {
        return (ushort)(y % 8 * 8 + x % 8);
    }
    
    protected BaseLandscape(ushort width, ushort height) {
        Width = width;
        Height = height;
        CellWidth = (ushort)(width * 8);
        CellHeight = (ushort)(height * 8);
        BlockCache = new BlockCache();
    }

    public RemovedCachedObjectArgs OnFreeBlock {
        set => BlockCache.OnRemovedCachedObject = value;
    }
    
    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }
    protected readonly BlockCache BlockCache;
    
    protected void AssertBlockCoords(ushort x, ushort y) {
        if (x >= Width || y >= Height) 
            throw new ArgumentException($"Coords out of range. Size: {Width}x{Height}, Requested: {x},{y}");
    }
    
    public LandTile GetLandTile(ushort x, ushort y) {
        var block = GetLandBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.Tiles[GetTileId(x, y)];
    }
    
    public ReadOnlyCollection<StaticTile> GetStaticTiles(ushort x, ushort y) {
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        return block.CellItems(GetTileId(x, y));
    }
    
    public LandBlock GetLandBlock(ushort x, ushort y) {
        return GetBlock(x, y).LandBlock;
    }

    public StaticBlock GetStaticBlock(ushort x, ushort y) {
        return GetBlock(x, y).StaticBlock;
    }
    
    private Block GetBlock(ushort x, ushort y) {
        AssertBlockCoords(x, y);
        return BlockCache.Get(x, y) ?? LoadBlock(x, y);
    }

    protected abstract Block LoadBlock(ushort x, ushort y);
}