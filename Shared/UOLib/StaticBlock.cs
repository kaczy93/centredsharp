namespace Shared;

public class StaticBlock : WorldBlock {
    public StaticBlock(Stream? data = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        Items = new List<StaticItem>();
        
        if (data != null && index?.Lookup >= 0 && index.Length > 0) {
            data.Position = index.Lookup;
            for (var i = 0; i < index.Length / 7; i++) Items.Add(new StaticItem(this, data, x, y));
        }
        
        for (int i = 0; i < 64; i++) {
            Cells[i] = new List<StaticItem>();
        }

        foreach (var item in Items) {
            Cells[item.Y % 8 * 8 + item.X % 8].Add(item);
        }

        Changed = false;
    }

    public List<StaticItem> Items { get; }
    
    public List<StaticItem>[] Cells = new List<StaticItem>[64];

    public int TotalSize => Items.Count * StaticItem.Size;

    public StaticBlock Clone() {
        var result = new StaticBlock {
            X = X,
            Y = Y
        };
        foreach (var staticItem in Items) Items.Add(staticItem.Clone());
        return result;
    }

    public override void Write(BinaryWriter writer) {
        foreach (var staticItem in Items) staticItem.Write(writer);
    }
}