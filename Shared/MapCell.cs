namespace Shared;

public class MapCell : WorldItem {
    private ulong _ghostId;

    public MapCell(WorldBlock owner, Stream stream, ulong x, ulong y) : base(owner) {
        _x = x;
        _y = y;
        using (var reader = new BinaryReader(stream)) {
            _tileId = reader.ReadUInt64();
            _z = reader.ReadInt16();
        }

        IsGhost = false;
    }

    public MapCell(WorldBlock owner, Stream stream) : this(owner, stream, 0, 0) { }

    public override ulong TileId {
        get {
            if (IsGhost) return _ghostId;
            return _tileId;
        }
    }

    public override short Z {
        get {
            if (IsGhost) return GhostZ;
            return _z;
        }
    }

    public short Altitude {
        get => Z;
        set => Z = value;
    }

    public bool IsGhost { get; set; }

    public short GhostZ { get; set; }

    public ulong GhostId {
        set => _ghostId = value;
    }

    public override int Size => Map.CellSize;

    //Originally MapCell is a returnType, maybe make MulBlock generic?
    //Maybe Copy constructor?
    public override MulBlock Clone() {
        return new MapCell(null, null) {
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