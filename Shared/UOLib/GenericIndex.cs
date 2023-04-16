using System.Text;

namespace Shared;

public class GenericIndex : MulBlock {
    
    public const int Size = 12;
    public int Lookup { get; set; }
    public int Length { get; set; }
    public int Various { get; init; }
    
    public GenericIndex(Stream? data = null) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        Lookup = reader.ReadInt32();
        Length = reader.ReadInt32();
        Various = reader.ReadInt32();
    }

    public GenericIndex Clone() {
        return new GenericIndex {
            Lookup = Lookup,
            Length = Length,
            Various = Various
        };
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Lookup);
        writer.Write(Length);
        writer.Write(Various);
    }
}