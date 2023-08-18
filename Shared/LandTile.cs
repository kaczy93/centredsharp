namespace CentrED;

public class LandTile {
    public const int Size = 3;

    public static LandTile Empty => new(0, 0, 0, 0);

    internal ushort _id;
    internal sbyte _z;

    private LandTile(ushort id, ushort x, ushort y, sbyte z) {
        _id = id;
        X = x;
        Y = y;
        _z = z;
    }

    public LandTile(BinaryReader reader, LandBlock? block = null, ushort x = 0, ushort y = 0) {
        Block = block;
        _id = reader.ReadUInt16();
        X = x;
        Y = y;
        _z = reader.ReadSByte();
    }
    
    public LandBlock? Block { get; }

    public ushort Id {
        get => _id;
        set {
            if (_id != value) {
                Block?.Landscape.OnLandReplaced(this, value);
                Block?.OnChanged(); 
            }
        }
    }

    public ushort X { get; }
    public ushort Y { get; }

    public sbyte Z {
        get => _z;
        set {
            if (_z != value) {
                Block?.Landscape.OnLandElevated(this, value);
                Block?.OnChanged(); 
            }
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(_id);
        writer.Write(_z);
    }
}