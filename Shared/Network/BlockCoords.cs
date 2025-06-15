using System.Buffers;

namespace CentrED.Network;

public record struct BlockCoords(ushort X, ushort Y)
{
    public const int SIZE = 4;

    public void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }
};

public static class SpanReaderBlockCoords
{
    public static BlockCoords ReadBlockCoords(this ref SpanReader reader)
    {
        return new BlockCoords(reader.ReadUInt16(), reader.ReadUInt16());
    }
}