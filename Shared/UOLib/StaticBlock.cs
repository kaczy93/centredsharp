namespace Shared;

public class StaticBlock : WorldBlock {
    public StaticBlock(Stream? data = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        Items = new List<StaticItem>();
        
        if (data != null && index?.Lookup >= 0 && index.Size > 0) {
            data.Position = index.Lookup;
            for (var i = 0; i < index.Size / 7; i++) Items.Add(new StaticItem(this, data, x, y));
        }

        Changed = false;
    }

    public List<StaticItem> Items { get; }

    public override int GetSize => Items.Count * 7;

    public void Sort() {
        Items.Sort();
    }

    public override MulBlock Clone() {
        var result = new StaticBlock {
            X = X,
            Y = Y
        };
        foreach (var staticItem in Items) Items.Add((StaticItem)staticItem.Clone());
        return result;
    }

    public override void Write(BinaryWriter writer) {
        foreach (var staticItem in Items) staticItem.Write(writer);
    }
}