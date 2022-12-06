//UOLib/UGenericIndex.pas

using System.Text;

namespace Shared;

//TGenericIndex
public class GenericIndex : MulBlock {

    public int Lookup { get; set; }
    public int Size { get; set; }
    public int Various { get; set; }
    
    public GenericIndex(Stream? data = null) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        Lookup = reader.ReadInt32();
        Size = reader.ReadInt32();
        Various = reader.ReadInt32();
    }

    public override int GetSize => 12;

    public override MulBlock Clone() {
        return new GenericIndex() {
            Lookup = Lookup,
            Size = Size,
            Various = Various
        };
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Lookup);
        writer.Write(Size);
        writer.Write(Various);
    }
}