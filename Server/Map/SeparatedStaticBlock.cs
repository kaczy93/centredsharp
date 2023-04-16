using Shared;
using Shared.MulProvider;

namespace Server; 

public class SeparatedStaticBlock : StaticBlock {
    //Original implementation calls base constructors and does everything again, a little bit different
    //This uses base implementation and only adds what's changed
    public SeparatedStaticBlock(Stream data, GenericIndex index, ushort x = 0, ushort y = 0) : base(data, index, x, y) {
        for (int i = 0; i < 64; i++) {
            Cells[i] = new List<StaticItem>();
        }

        foreach (var item in Items) {
            Cells[item.Y % 8 * 8 + item.X % 8].Add(item);
        }
    }
    
    public List<StaticItem>[] Cells = new List<StaticItem>[64];
    
    public TileDataProvider TileDataProvider { get; set; }

    public override int GetSize {
        get {
            RebuildList();
            return base.GetSize;
        }
    }

    public void RebuildList() {
        Items.Clear();
        int solver = 0;
        for (int i = 0; i < 64; i++) {
            if (Cells[i] != null) {
                for (int j = 0; j < Cells[i].Count; j++) {
                    Items.Add(Cells[i][j]);
                    if (Cells[i][j].TileId < TileDataProvider.StaticCount) {
                        Cells[i][j].UpdatePriorities(TileDataProvider.StaticTiles[Cells[i][j].TileId], solver);
                    }
                    else {
                        CEDServer.LogError($"Cannot find Tiledata for the Static Item with ID {Cells[i][j].TileId}");
                    }
                    solver++;
                }
            }
        }
        Sort();
    }
}