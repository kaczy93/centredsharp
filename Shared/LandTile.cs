using System.Buffers;

namespace CentrED;

public class LandTile : BaseTile
{
    public const int Size = 3;

    public static LandTile Empty => new(0, 0, 0, 0);

    public LandTile(uint id, ushort x, ushort y, short z)
    {
        _id = id;
        _x = x;
        _y = y;
        _z = ZRange.Clamp(z);
    }

    public LandTile(BinaryReader reader, ushort x, ushort y, LandBlock? block = null)
    {
        Block = block;
        _id = reader.ReadTileId();
        _x = x;
        _y = y;
        _z = reader.ReadZ();
    }

    public LandTile(LandBlock block, uint id, ushort x, ushort y, short z)
    {
        Block = block;
        _id = id;
        _x = x;
        _y = y;
        _z = ZRange.Clamp(z);
    }


    public LandBlock? Block { get; }

    //GhostId is needed only for client for landbrush calculations, until we receive proper id update from the server
    public uint? GhostId;
    public uint RealId => _id;
    public override uint Id
    {
        get => GhostId ?? _id;
        set
        {
            TileId.Ensure(value);
            if (_id != value)
            {
                GhostId = value;
                Block?.Landscape.OnLandReplaced(this, value, _z);
                Block?.OnChanged();
            }
        }
    }

    public override short Z
    {
        get => _z;
        set
        {
            value = ZRange.Clamp(value);
            if (_z != value)
            {
                Block?.Landscape.OnLandElevated(this, value);
                Block?.OnChanged();
            }
        }
    }

    public void ReplaceLand(uint newID, short newZ)
    {
        TileId.Ensure(newID);
        newZ = ZRange.Clamp(newZ);
        if (newID != Id || newZ != Z)
        {
            GhostId = newID;
            Block?.Landscape.OnLandReplaced(this, newID, newZ);
            Block?.OnChanged();
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteTileId(_id);
        writer.WriteZ(_z);
    }

    public override string ToString()
    {
        return $"Land 0x{Id:X} <{X},{Y},{Z}>";
    }
    
    public override string ShortString()
    {
        return $"Land 0x{Id:x}";
    }
}