using CentrED.Network;

namespace CentrED;

public delegate void StaticTileIdChanged(StaticTile tile, ushort newId);
public delegate void StaticTilePosChanged(StaticTile tile, ushort newX, ushort newY);
public delegate void StaticTileZChanged(StaticTile tile, sbyte newZ);
public delegate void StaticTileHueChanged(StaticTile tile, ushort newHue);
public class StaticTile: IEquatable<StaticTile> {
    public const int Size = 7;
    
    public StaticTileIdChanged? OnIdChanged;
    public StaticTilePosChanged? OnPosChanged;
    public StaticTileZChanged? OnZChanged;
    public StaticTileHueChanged? OnHueChanged;
    
    private StaticBlock? _block;
    private ushort _id;
    private ushort _x;
    private ushort _y;
    private sbyte _z;
    private ushort _hue;

    public StaticTile(StaticInfo si) : this(si.Id, si.X, si.Y, si.Z, si.Hue) { }
    
    public StaticTile(ushort id, ushort x, ushort y, sbyte z, ushort hue, StaticBlock? block = null) {
        _block = block;
        _id = id;
        _x = x;
        _y = y;
        _z = z;
        _hue = hue;
        
        LocalX = (byte)(x & 0x7);
        LocalY = (byte)(y & 0x7);
    }

    public StaticTile(BinaryReader reader, StaticBlock? block = null, ushort blockX = 0, ushort blockY = 0) {
        _block = block;
        _id = reader.ReadUInt16();
        LocalX = reader.ReadByte();
        LocalY = reader.ReadByte();
        _z = reader.ReadSByte();
        _hue = reader.ReadUInt16();

        _x = (ushort)(blockX * 8 + LocalX);
        _y = (ushort)(blockY * 8 + LocalY);
    }
    
    public StaticBlock? Block {
        get => _block;
        internal set {
            if (_block == value) return;
            
            OnChanged(); //Old block changed
            _block = value;
            OnChanged(); //New block changed
        }
    }
    
    public ushort Id {
        get => _id;
        set {
            if (_id != value) {
                OnIdChanged?.Invoke(this, value);
                _id = value;
                OnChanged();
            }
        }
    }
    
    public sbyte Z {
        get => _z;
        set {
            if (_z != value) {
                OnTileZChanged(value);
                _z = value;
                OnChanged();
            }
        }
    }
    
    public ushort Hue {
        get => _hue;
        set {
            if (_hue != value) {
                OnHueChanged?.Invoke(this, value);
                _hue = value;
                OnChanged();
            }
        }
    }
    
    public ushort X { 
        get => _x;
        set {
            if (_x != value) {
                OnTilePosChanged(value, _y);
                _x = value;
                OnChanged();
            }
        } 
    }
    public ushort Y { 
        get => _y;
        set {
            if (_y != value) {
                OnTilePosChanged(_x, value);
                _y = value;
                OnChanged();
            }
        } 
    }
    
    public byte LocalX { get; private set; }

    public byte LocalY { get; private set; }
    
    public int PriorityZ { get; private set; }

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
        OnChanged();
    }

    public void UpdatePriority(StaticTileData tileData) {
        PriorityZ = _z;
        if (tileData.Flags.HasFlag(TiledataFlag.Background)) PriorityZ--;

        if (tileData.Height > 0) PriorityZ++;
    }

    private void OnChanged() {
        if (_block != null) {
            _block.Changed = true;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(_id);
        writer.Write(LocalX);
        writer.Write(LocalY);
        writer.Write(_z);
        writer.Write(_hue);
    }

    public bool Equals(StaticTile? other) {
        return other != null && 
               _id == other._id && 
               _x == other._x && 
               _y == other._y && 
               _z == other._z && 
               _hue == other._hue;
    }
    

    private void OnTilePosChanged(ushort newX, ushort newY) {
        OnPosChanged?.Invoke(this, newX, newY);
        LocalX = (byte)(newX & 0x7);
        LocalY = (byte)(newY & 0x7);
    }

    private void OnTileZChanged(sbyte newZ) {
        OnZChanged?.Invoke(this, newZ);
    }
    
    public override string ToString() {
        return $"{Id}:{X},{Y},{Z} {Hue}";
    }
}