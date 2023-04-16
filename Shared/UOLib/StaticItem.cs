using System.Text;

namespace Shared;

public class StaticItem : WorldItem {
    private ushort _hue;
    private byte _localX;
    private byte _localY;

    public StaticItem(WorldBlock? owner = null, Stream? data = null, ushort blockX = 0, ushort blockY = 0) : base(owner) {
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        _tileId = reader.ReadUInt16();
        _localX = reader.ReadByte();
        _localY = reader.ReadByte();
        _z = reader.ReadSByte();
        _hue = reader.ReadUInt16();

        _x = (ushort)(blockX * 8 + _localX);
        _y = (ushort)(blockY * 8 + _localY);
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

    public override ushort X { 
        get => _x;
        set {
            if (_x != value) {
                _x = value;
                _localX = (byte)(_x % 8);
                DoChanged();
            }
        } 
    }

    public override ushort Y { 
        get => _y;
        set {
            if (_y != value) {
                _y = value;
                _localY = (byte)(_y % 8);
                DoChanged();
            }
        }
    }

    public byte LocalX => _localX;
    public byte LocalY => _localY;

    public const int Size = 7;

    public void UpdatePriorities(StaticTileData tileData, int solver) {
        PriorityBonus = 0;
        if (!tileData.Flags.HasFlag(TiledataFlag.Background)) PriorityBonus++;

        if (tileData.Height > 0) PriorityBonus = 0;

        Priority = _z + PriorityBonus;
        PrioritySolver = solver;
    }

    public StaticItem Clone() {
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