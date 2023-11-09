namespace CentrED;

public class LandTile : BaseTile
{
    public const int Size = 3;

    public static LandTile Empty => new(0, 0, 0, 0);

    public LandTile(ushort id, ushort x, ushort y, sbyte z)
    {
        _id = id;
        _x = x;
        _y = y;
        _z = z;
    }

    public LandTile(BinaryReader reader, LandBlock? block = null, ushort x = 0, ushort y = 0)
    {
        Block = block;
        _id = reader.ReadUInt16();
        _x = x;
        _y = y;
        _z = reader.ReadSByte();
    }

    public LandBlock? Block { get; }

    public override ushort Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                Block?.Landscape.OnLandReplaced(this, value);
                Block?.OnChanged();
            }
        }
    }

    public override sbyte Z
    {
        get => _z;
        set
        {
            if (_z != value)
            {
                Block?.Landscape.OnLandElevated(this, value);
                Block?.OnChanged();
            }
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(_id);
        writer.Write(_z);
    }
}