namespace CentrED;

public delegate void StaticTileIdChanged(StaticTile tile, ushort newId);
public delegate void StaticTilePosChanged(StaticTile tile, ushort newX, ushort newY);
public delegate void StaticTileZChanged(StaticTile tile, sbyte newZ);
public delegate void StaticTileHueChanged(StaticTile tile, ushort newHue);
public class StaticTile : Tile<StaticBlock> {
    public StaticTileIdChanged? OnIdChanged;
    public StaticTilePosChanged? OnPosChanged;
    public StaticTileZChanged? OnZChanged;
    public StaticTileHueChanged? OnHueChanged;
    
    public const int Size = 7;
    private ushort _hue;

    public StaticTile(ushort id, ushort x, ushort y, sbyte z, ushort hue, StaticBlock? owner = null) : base(owner) {
        _id = id;
        _x = x;
        _y = y;
        _z = z;
        _hue = hue;
        
        LocalX = (byte)(x & 0x7);
        LocalY = (byte)(y & 0x7);
    }

    public StaticTile(BinaryReader reader, StaticBlock? owner = null, ushort blockX = 0, ushort blockY = 0) : base(owner) {
        _id = reader.ReadUInt16();
        LocalX = reader.ReadByte();
        LocalY = reader.ReadByte();
        _z = reader.ReadSByte();
        _hue = reader.ReadUInt16();

        _x = (ushort)(blockX * 8 + LocalX);
        _y = (ushort)(blockY * 8 + LocalY);
    }
    
    public ushort Hue {
        get => _hue;
        set {
            if (_hue != value) {
                OnHueChanged?.Invoke(this, value);
                _hue = value;
                DoChanged();
            }
        }
    }
    
    public byte LocalX { get; private set; }

    public byte LocalY { get; private set; }

    public void UpdatePos(ushort newX, ushort newY, sbyte newZ) {
        if (_x != newX || _y != newY) {
            OnTilePosChanged(newX, newY);
            _x = newX;
            _y = newY;
        }
        if (_z != newZ) {
            OnTileZChanged(newZ);
            _z = newZ;
        }
        DoChanged();
    }

    public void UpdatePriorities(StaticTileData tileData, int solver) {
        PriorityBonus = 0;
        if (!tileData.Flags.HasFlag(TiledataFlag.Background)) PriorityBonus++;

        if (tileData.Height > 0) PriorityBonus = 0;

        Priority = _z + PriorityBonus;
        PrioritySolver = solver;
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(_id);
        writer.Write(LocalX);
        writer.Write(LocalY);
        writer.Write(_z);
        writer.Write(_hue);
    }

    public override void OnTileIdChanged(ushort newId) {
        OnIdChanged?.Invoke(this, newId);
    }

    public override void OnTilePosChanged(ushort newX, ushort newY) {
        OnPosChanged?.Invoke(this, newX, newY);
        LocalX = (byte)(newX & 0x7);
        LocalY = (byte)(newY & 0x7);
    }

    public override void OnTileZChanged(sbyte newZ) {
        OnZChanged?.Invoke(this, newZ);
    }
}