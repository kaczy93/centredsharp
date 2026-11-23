using System.Buffers;

namespace CentrED;

// Static id is kept as uint in memory, statics files and network still use ushort
public static class StaticId
{
    public const uint MAX = ushort.MaxValue;

    // Large scale operations send static ids shifted by this, wrapping around in 16 bit
    public const uint LSO_OFFSET = 0x4000;

    public static ushort ToUInt16(uint id) =>
        id <= MAX ? (ushort)id : throw new ArgumentOutOfRangeException(nameof(id), $"Static id 0x{id:X} doesn't fit in 16 bit");

    public static uint ReadStaticId(this BinaryReader reader) => reader.ReadUInt16();

    public static uint ReadStaticId(this ref SpanReader reader) => reader.ReadUInt16();

    public static void WriteStaticId(this BinaryWriter writer, uint id) => writer.Write(ToUInt16(id));

    public static uint ReadLsoStaticId(this ref SpanReader reader) => (ushort)(reader.ReadUInt16() - LSO_OFFSET);

    public static void WriteLsoStaticId(this BinaryWriter writer, uint shiftedId)
    {
        if (shiftedId > MAX + LSO_OFFSET)
            throw new ArgumentOutOfRangeException(nameof(shiftedId), $"Static id 0x{shiftedId - LSO_OFFSET:X} doesn't fit in 16 bit");
        writer.Write((ushort)shiftedId);
    }
}
