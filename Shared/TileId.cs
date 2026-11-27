using System.Buffers;

namespace CentrED;

// Land and static ids are kept as uint in memory, map/statics files and network still use ushort
public static class TileId
{
    public const int MAX_LAND_INDEX = 0x4000;
    
    public const uint MAX = ushort.MaxValue;

    public static uint Ensure(uint id) => id <= MAX ? id :
        throw new ArgumentOutOfRangeException(nameof(id), $"Tile id 0x{id:X} exceeds 0x{MAX:X}");

    public static uint EnsureLand(uint id) => id < MAX_LAND_INDEX ? id :
        throw new ArgumentOutOfRangeException(nameof(id), $"Land id 0x{id:X} exceeds 0x{MAX_LAND_INDEX - 1:X}");

    //Anything above int.MaxValue will not fit into UOFile.GetValidRefEntry(), -1 makes it return Invalid
    public static int ToInt32OrInvalid(uint id) => id <= int.MaxValue ? (int)id : -1;

    public static ushort ToUInt16(uint id) =>
        id <= MAX ? (ushort)id : throw new ArgumentOutOfRangeException(nameof(id), $"Tile id 0x{id:X} exceeds ushort");

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
