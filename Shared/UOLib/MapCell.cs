using System.Text;

namespace Shared;

public class MapCell : WorldItem {
    public MapCell(WorldBlock? owner = null, Stream? data = null, ushort x = 0, ushort y = 0) : base(owner) {
        _x = x;
        _y = y;
        if (data != null) {
            using var reader = new BinaryReader(data, Encoding.UTF8, true);
            _tileId = reader.ReadUInt16();
            _z = reader.ReadSByte();
        }

    }

    public override ushort TileId => _tileId;

    public override sbyte Z => _z;

    public sbyte Altitude {
        get => Z;
        set => Z = value;
    }

    public override int GetSize => Map.CellSize;

    //Originally MapCell is a returnType, maybe make MulBlock generic?
    //Maybe Copy constructor?
    public override MulBlock Clone() {
        return new MapCell {
            _x = _x,
            _y = _y,
            _z = _z,
            _tileId = _tileId
        };
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(_tileId);
        writer.Write(_z);
    }
}