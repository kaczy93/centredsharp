//UOLib/UStatics.pas

namespace Shared;

//TStaticItem
public class StaticItem : WorldItem {
    protected ushort _hue;

    public StaticItem(WorldBlock? owner = null, BinaryReader? reader = null, ushort blockx = 0, ushort blocky = 0) : base(owner) {
        if (reader != null) {
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

    public override int GetSize => 7; // What is this?

    public void UpdatePriorities(StaticTileData tileData, int solver) {
        PriorityBonus = 0;
        if (!tileData.Flags.HasFlag(TiledataFlag.Background)) PriorityBonus++;

        if (tileData.Height > 0) PriorityBonus = 0;

        Priority = _z + PriorityBonus;
        PrioritySolver = solver;
    }

    public override MulBlock Clone() {
        return new StaticItem {
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