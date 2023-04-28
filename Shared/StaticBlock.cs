using System.Collections.ObjectModel;

namespace CentrED;

public delegate void TileAdded(StaticBlock block, StaticTile tile);
public delegate void TileRemoved(StaticBlock block, StaticTile tile);

public class StaticBlock : WorldBlock {
    public TileAdded? OnTileAdded;
    public TileRemoved? OnTileRemoved;
    public static ushort TileId(ushort x, ushort y) {
        return (ushort)(y % 8 * 8 + x % 8);
    }
    
    public StaticBlock(BinaryReader? reader = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        _tiles = new List<StaticTile>();
        
        if (reader != null && index?.Lookup >= 0 && index.Length > 0) {
            reader.BaseStream.Position = index.Lookup;
            for (var i = 0; i < index.Length / 7; i++) 
                _tiles.Add(new StaticTile(reader, this, x, y));
        }
        
        Changed = false;
    }

    private List<StaticTile> _tiles;

    public ReadOnlyCollection<StaticTile> Tiles => _tiles.AsReadOnly();

    public int TotalSize => _tiles.Count * StaticTile.Size;
    
    public ReadOnlyCollection<StaticTile> CellItems(int cellId) =>
        _tiles.FindAll(s => TileId(s.X, s.Y) == cellId).AsReadOnly();

    public void AddTile(StaticTile tile) {
        _tiles.Add(tile);
        Changed = true;
        OnTileAdded?.Invoke(this, tile);
    }
    
    public void RemoveTile(StaticTile tile) {
        _tiles.Remove(tile);
        Changed = true;
        OnTileRemoved?.Invoke(this, tile);
    }

    public override void Write(BinaryWriter writer) {
        foreach (var staticItem in _tiles)
            staticItem.Write(writer);
    }

    public void SortTiles(TileDataProvider tdp) {
        for (var i = 0; i < _tiles.Count; i++) {
            var staticTile = _tiles[i];
            staticTile.UpdatePriorities(tdp.StaticTiles[staticTile.Id], i + 1);
        }

        _tiles.Sort();
    }
}