using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CentrED;

public delegate void TileAdded(StaticBlock block, StaticTile tile);
public delegate void TileRemoved(StaticBlock block, StaticTile tile);

public class StaticBlock : WorldBlock {
    public TileAdded? OnTileAdded;
    public TileRemoved? OnTileRemoved;
    
    public StaticBlock(BinaryReader? reader = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        _tiles = new List<StaticTile>[8,8];
        
        if (reader != null && index?.Lookup >= 0 && index.Length > 0) {
            reader.BaseStream.Position = index.Lookup;
            for (var i = 0; i < index.Length / 7; i++) {
                AddTile(new StaticTile(reader, this, x, y));
            }
        }
        
        Changed = false;
    }

    private List<StaticTile>?[,] _tiles;

    public int TotalTilesCount { get; private set; }

    public int TotalSize => TotalTilesCount * StaticTile.Size;

    public IEnumerable<StaticTile> AllTiles() {
        foreach (var staticTiles in _tiles) {
            if (staticTiles == null) continue;
            
            foreach (var staticTile in staticTiles) {
                yield return staticTile;
            }
        }
    }

    public IEnumerable<StaticTile> GetTiles(ushort x, ushort y) =>
        EnsureTiles(x, y).AsReadOnly();

    private List<StaticTile> EnsureTiles(ushort x, ushort y) {
        ref var result = ref _tiles[x & 0x7, y & 0x7];
        if (result == null) {
            result = new List<StaticTile>();
        }
        return result;
    }

    public void AddTile(StaticTile tile) {
        EnsureTiles(tile.LocalX, tile.LocalY).Add(tile);
        TotalTilesCount++;
        tile.Owner = this;
        Changed = true;
        OnTileAdded?.Invoke(this, tile);
    }
    
    public void RemoveTile(StaticTile tile) {
        if ( EnsureTiles(tile.LocalX, tile.LocalY).Remove(tile))
            tile.Owner = null;
        TotalTilesCount--;
        Changed = true;
        OnTileRemoved?.Invoke(this, tile);
    }

    public override void Write(BinaryWriter writer) {
        foreach (var staticTiles in _tiles) {
            if(staticTiles == null) continue;
            foreach (var staticTile in staticTiles) {
                staticTile.Write(writer);
            }
        }
    }

    public void SortTiles(TileDataProvider tdp) {
        var i = 0;
        foreach (var staticTiles in _tiles) {
            if(staticTiles == null) continue;
            foreach (var tile in staticTiles) {
                tile.UpdatePriorities(tdp.StaticTiles[tile.Id], i + 1);
            }
            staticTiles.Sort();
        }
    }
}