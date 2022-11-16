//UOLib/UGenericIndex.pas

namespace Shared;

//TGenericIndex
public class GenericIndex : MulBlock {

    public int Lookup { get; set; }
    public int Size { get; set; }
    public int Various { get; set; }
    
    public GenericIndex(Stream? stream) {
        if (stream != null) {
            using var reader = new BinaryReader(stream);
            Lookup = reader.ReadInt32();
            Size = reader.ReadInt32();
            Various = reader.ReadInt32();
        }
    }

    public override int GetSize => 12;

    public override MulBlock Clone() {
        return new GenericIndex(null) {
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