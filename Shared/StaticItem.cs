//UOLib/UStatics.pas

namespace Shared;

//TStaticItem
public class StaticItem : WorldItem {
    protected ushort _hue;

    public StaticItem(WorldBlock owner, Stream stream, ushort blockx = 0, ushort blocky = 0) : base(owner) {
        using (var reader = new BinaryReader(stream)) {
            _tileId = reader.ReadUInt16();
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            _z = reader.ReadSByte();
            _hue = reader.ReadUInt16();

            _x = (ushort)(blockx * 8 + x);
            _y = (ushort)(blocky * 8 + y);
        }
    }

    public ushort Hue {
        get => _hue;
        protected set {
            if (_hue != value) {
                _hue = value;
                DoChanged();
            }
        }
    }

    public override int Size => 7; // What is this?

    public void UpdatePriorities(StaticTiledata tiledata, int solver) {
        PriorityBonus = 0;
        if (!tiledata.Flags.Contains(TiledataFlag.Background)) PriorityBonus++;

        if (tiledata.Height > 0) PriorityBonus = 0;

        Priority = _z + PriorityBonus;
        PrioritySolver = solver;
    }

    public override MulBlock Clone() {
        return new StaticItem(null, null) {
            _tileId = _tileId,
            _x = _x,
            _y = _y,
            _z = _z,
            _hue = _hue
        };
    }

    public override void Write(BinaryWriter writer) {
        var x = _x / 8;
        var y = _y / 8;
        writer.Write(_tileId);
        writer.Write(x);
        writer.Write(y);
        writer.Write(_z);
        writer.Write(_hue);
    }
}