//UOLib/UStatics.pas

namespace Shared;

//TStaticBlock
public class StaticBlock : WorldBlock {
    public StaticBlock(Stream stream, GenericIndex index, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        Items = new List<StaticItem>();
        if (stream != null && index.Lookup > 0 && index.Size > 0) {
            stream.Position = index.Lookup;
            var block = new MemoryStream();
            using (var reader = new BinaryReader(stream)) {
                block.Write(reader.ReadBytes(index.Size));
            }

            block.Position = 0;
            for (var i = 1; i <= index.Size / 7; i++) Items.Add(new StaticItem(this, block, x, y));
        }
    }

    public List<StaticItem> Items { get; set; }

    public override int Size => 7; //???

    public void ReverseWrite(BinaryWriter writer) {
        for (var i = Items.Count - 1; i >= 0; i--) Items[i].Write(writer);
    }

    public void Sort() {
        Items.Sort();
    }

    public override MulBlock Clone() {
        var result = new StaticBlock(null, null, X, Y);
        foreach (var staticItem in Items) Items.Add((StaticItem)staticItem.Clone());
        return result;
    }

    public override void Write(BinaryWriter writer) {
        foreach (var staticItem in Items) staticItem.Write(writer);
    }
}