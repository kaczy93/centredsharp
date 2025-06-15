using System.Buffers;
using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StaticInfo(ushort X, ushort Y, sbyte Z, ushort Id, ushort Hue)
{
    public const int SIZE = 9;
    public void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(Id);
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
                              reader.ReadSByte(), 
                              reader.ReadUInt16(), 
                              reader.ReadUInt16());
    }
}