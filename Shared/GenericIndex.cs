namespace CentrED;

public class GenericIndex
{
    public const int Size = 12;

    public static GenericIndex Empty => new()
    {
        Lookup = -1,
        Length = 0,
        Various = 0
    };

    public GenericIndex(int lookup, int length, int various)
    {
        Lookup = lookup;
        Length = length;
        Various = various;
    }
    
    public GenericIndex(BinaryReader? reader = null)
    {
        if (reader == null)
            return;

        Lookup = reader.ReadInt32();
        Length = reader.ReadInt32();
        Various = reader.ReadInt32();
    }

    public int Lookup { get; set; }
    public int Length { get; set; }
    public int Various { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Lookup);
        writer.Write(Length);
        writer.Write(Various);
    }
}