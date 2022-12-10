using System.Text;

namespace Shared;

public class StaticItem : WorldItem {
    protected ushort _hue;
    private byte _localX;
    private byte _localY;

    public StaticItem(WorldBlock? owner = null, Stream? data = null, ushort blockx = 0, ushort blocky = 0) : base(owner) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        _tileId = reader.ReadUInt16();
        _localX = reader.ReadByte();
        _localY = reader.ReadByte();
        _z = reader.ReadSByte();
        _hue = reader.ReadUInt16();

        _x = (ushort)(blockx * 8 + _localX);
        _y = (ushort)(blocky * 8 + _localY);
    }

    public ushort Hue {
        get => _hue;
        set {
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
            _hue = _hue,
            _localX = _localX,
            _localY = _localY
        };
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(_tileId);
        writer.Write(_localX);
        writer.Write(_localY);
        writer.Write(_z);
        writer.Write(_hue);
    }
}