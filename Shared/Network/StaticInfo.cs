using System.Buffers;

namespace CentrED.Network;

public record struct StaticInfo(ushort X, ushort Y, short Z, uint Id, ushort Hue)
{
    public const int SIZE = 9;
    public void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.WriteZ(Z);
        writer.WriteStaticId(Id);
        writer.Write(Hue);
    }

    public override string ToString()
    {
        return $"{Id}:{X},{Y},{Z} {Hue}";
    }
}

public static class SpanReaderStaticInfo
{
    public static StaticInfo ReadStaticInfo(this ref SpanReader reader)
    {
        return new StaticInfo(reader.ReadUInt16(), 
                              reader.ReadUInt16(), 
                              reader.ReadZ(), 
                              reader.ReadStaticId(), 
                              reader.ReadUInt16());
    }
}