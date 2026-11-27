using System.Buffers;

namespace CentrED;

// Land and static ids are kept as uint in memory, map/statics files and network still use ushort
public static class TileId
{
    public const int MAX_LAND_INDEX = 0x4000;
    
    public const uint MAX = ushort.MaxValue;

    //Anything above int.MaxValue will not fit into UOFile.GetValidRefEntry()
    public static int ToInt32(uint id) => id < int.MaxValue ? (int)id : throw new ArgumentOutOfRangeException();

    public static ushort ToUInt16(uint id) =>
        id <= MAX ? (ushort)id : throw new ArgumentOutOfRangeException(nameof(id), $"Tile id 0x{id:X} doesn't fit in 16 bit");

    public static uint ReadTileId(this BinaryReader reader) => reader.ReadUInt16();

    public static uint ReadTileId(this ref SpanReader reader) => reader.ReadUInt16();

    public static void WriteTileId(this BinaryWriter writer, uint id) => writer.Write(ToUInt16(id));

    public static uint ReadLsoStaticId(this ref SpanReader reader) => (ushort)(reader.ReadUInt16() - MAX_LAND_INDEX);

    public static void WriteLsoStaticId(this BinaryWriter writer, uint shiftedId)
    {
        if (shiftedId > MAX + MAX_LAND_INDEX)
            throw new ArgumentOutOfRangeException(nameof(shiftedId), $"Static id 0x{shiftedId - MAX_LAND_INDEX:X} doesn't fit in 16 bit");
        writer.Write((ushort)shiftedId);
    }
}
