using System.Buffers;

namespace CentrED;

// Z is kept as short in memory, map files and network still use sbyte
public static class ZRange
{
    public const short MIN = sbyte.MinValue;
    public const short MAX = sbyte.MaxValue;

    public static short Clamp(int z) => (short)Math.Clamp(z, MIN, MAX);

    public static short ReadZ(this BinaryReader reader) => reader.ReadSByte();

    public static short ReadZ(this ref SpanReader reader) => reader.ReadSByte();

    public static void WriteZ(this BinaryWriter writer, short z) => writer.Write((sbyte)Clamp(z));
}
