namespace Shared;

public class MapCell : WorldItem {
    private ushort _ghostId;

    public MapCell(WorldBlock? owner = null, BinaryReader? reader = null, ushort x = 0, ushort y = 0) : base(owner) {
        _x = x;
        _y = y;
        if (reader != null) {
            _tileId = reader.ReadUInt16();
            _z = reader.ReadSByte();
        }

        IsGhost = false;
    }

    public override ushort TileId {
        get {
            if (IsGhost) return _ghostId;
            return _tileId;
        }
    }

    public override sbyte Z {
        get {
            if (IsGhost) return GhostZ;
            return _z;
        }
    }

    public sbyte Altitude {
        get => Z;
        set => Z = value;
    }

    public bool IsGhost { get; set; }

    public sbyte GhostZ { get; set; }

    public ushort GhostId {
        set => _ghostId = value;
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