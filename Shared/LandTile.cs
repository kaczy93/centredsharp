namespace CentrED;

public delegate void LandTileIdChanged(LandTile tile, ushort newId);
public delegate void LandTileZChanged(LandTile tile, sbyte newZ);
public class LandTile {
    public const int Size = 3;
    
    public LandTileIdChanged? OnIdChanged;
    public LandTileZChanged? OnZChanged;

    public static LandTile Empty => new(0, 0, 0, 0);
    
    private LandBlock? _block;
    private ushort _id;
    private sbyte _z;

    private LandTile(ushort id, ushort x, ushort y, sbyte z) {
        _id = id;
        X = x;
        Y = y;
        _z = z;
    }

    public LandTile(BinaryReader reader, LandBlock? block = null, ushort x = 0, ushort y = 0) {
        _block = block;
        _id = reader.ReadUInt16();
        X = x;
        Y = y;
        _z = reader.ReadSByte();
    }
    
    public LandBlock? Block {
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

    public ushort X { get; }
    public ushort Y { get; }

    public sbyte Z {
        get => _z;
        set {
            if (_z != value) {
                OnZChanged?.Invoke(this, value);
                _z = value;
                OnChanged();
            }
        }
    }

    private void OnChanged() {
        if (_block != null) {
            _block.Changed = true;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(_id);
        writer.Write(_z);
    }
}