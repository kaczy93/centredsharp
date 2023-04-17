namespace Shared;

public class MapCell : WorldItem {
    public MapCell(WorldBlock? owner = null, BinaryReader? reader = null, ushort x = 0, ushort y = 0) : base(owner) {
        _x = x;
        _y = y;
        if (reader != null) {
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

    public const int Size = 3;

    public MapCell Clone() {
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