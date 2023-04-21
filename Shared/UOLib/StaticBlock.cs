using System.Collections.ObjectModel;

namespace Shared;

public class StaticBlock : WorldBlock {
    public StaticBlock(BinaryReader? reader = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        Tiles = new List<StaticTile>();
        
        if (reader != null && index?.Lookup >= 0 && index.Length > 0) {
            reader.BaseStream.Position = index.Lookup;
            for (var i = 0; i < index.Length / 7; i++) 
                Tiles.Add(new StaticTile(this, reader, x, y));
        }
        
        Changed = false;
    }

    public List<StaticTile> Tiles { get; }

    public int TotalSize => Tiles.Count * StaticTile.Size;

    public static ushort TileId(ushort x, ushort y) {
        return (ushort)(y % 8 * 8 + x % 8);
    }
    
    public ReadOnlyCollection<StaticTile> CellItems(int cellId) =>
        Tiles.FindAll(s => TileId(s.X, s.Y) == cellId).AsReadOnly();

    public override void Write(BinaryWriter writer) {
        lock (Tiles) {
            foreach (var staticItem in Tiles)
                staticItem.Write(writer);
        }
    }
}