//UOLib/UStatics.pas

using System.Text;

namespace Shared;

//TStaticBlock
public class StaticBlock : WorldBlock {
    public StaticBlock(Stream? data = null, GenericIndex? index = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        Items = new List<StaticItem>();
        
        if (data != null && index?.Lookup > 0 && index.GetSize > 0) {
            using var reader = new BinaryReader(data, Encoding.UTF8, true);
            reader.BaseStream.Position = index.Lookup;
            var block = new MemoryStream();
            block.Write(reader.ReadBytes(index.GetSize));
            block.Position = 0;
            for (var i = 1; i <= index.GetSize / 7; i++) Items.Add(new StaticItem(this, block, x, y));
        }
    }

    public List<StaticItem> Items { get; set; }

    public override int GetSize => 7; //???

    public void ReverseWrite(BinaryWriter writer) {
        for (var i = Items.Count - 1; i >= 0; i--) Items[i].Write(writer);
    }

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