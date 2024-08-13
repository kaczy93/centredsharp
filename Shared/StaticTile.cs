using CentrED.Network;
using ClassicUO.Assets;

namespace CentrED;

public class StaticTile : BaseTile, IEquatable<StaticTile>, IEquatable<BaseTile>
{
    public const int Size = 7;

    private StaticBlock? _block;

    internal ushort _hue;

    public StaticTile(StaticInfo si) : this(si.Id, si.X, si.Y, si.Z, si.Hue)
    {
    }

    public StaticTile(ushort id, ushort x, ushort y, sbyte z, ushort hue, StaticBlock? block = null)
    {
        _block = block;
        _id = id;
        _x = x;
        _y = y;
        _z = z;
        _hue = hue;

        LocalX = (byte)(x & 0x7);
        LocalY = (byte)(y & 0x7);

        PriorityZ = _z;
    }

    public StaticTile(BinaryReader reader, StaticBlock? block = null, ushort blockX = 0, ushort blockY = 0)
    {
        _block = block;
        _id = reader.ReadUInt16();
        LocalX = reader.ReadByte();
        LocalY = reader.ReadByte();
        _z = reader.ReadSByte();
        _hue = reader.ReadUInt16();

        _x = (ushort)(blockX * 8 + LocalX);
        _y = (ushort)(blockY * 8 + LocalY);
        
        PriorityZ = _z;
    }

    public StaticBlock? Block
    {
        get => _block;
        internal set => _block = value;
    }

    public override ushort Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                OnTileIdChanged(value);
                _block?.OnChanged();
            }
        }
    }

    public override ushort X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                OnTilePosChanged(value, _y);
                _block?.OnChanged();
            }
        }
    }

    public override ushort Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                OnTilePosChanged(_x, value);
                _block?.OnChanged();
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
                OnTileZChanged(value);
                _block?.OnChanged();
            }
        }
    }

    public ushort Hue
    {
        get => _hue;
        set
        {
            if (_hue != value)
            {
                OnTileHueChanged(value);
                _block?.OnChanged();
            }
        }
    }

    public byte LocalX { get; internal set; }

    public byte LocalY { get; internal set; }

    public int PriorityZ { get; private set; }
    public int CellIndex { get; internal set; }

    public void UpdatePos(ushort newX, ushort newY, sbyte newZ)
    {
        if (_x != newX || _y != newY)
        {
            OnTilePosChanged(newX, newY);
        }
        if (_z != newZ)
        {
            OnTileZChanged(newZ);
        }
        _block?.OnChanged();
    }

    public void UpdatePriority(StaticTiles tileData)
    {
        PriorityZ = _z;
        if (tileData.IsBackground)
            PriorityZ--;
        if (tileData.Height > 0)
            PriorityZ++;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(_id);
        writer.Write(LocalX);
        writer.Write(LocalY);
        writer.Write(_z);
        writer.Write(_hue);
    }

    public bool Equals(BaseTile? other)
    {
        if (other is StaticTile staticTile)
            return Equals(staticTile);
        return false;
    }

    public bool Equals(StaticTile? other)
    {
        return other != null && _id == other._id && _x == other._x && _y == other._y && _z == other._z &&
               _hue == other._hue;
    }

    public bool Match(StaticInfo si) => si.Z == Z && si.Id == Id && si.Hue == Hue;

    private void OnTileIdChanged(ushort newId)
    {
        _block?.Landscape.OnStaticTileReplaced(this, newId);
    }

    private void OnTilePosChanged(ushort newX, ushort newY)
    {
        _block?.Landscape.OnStaticTileMoved(this, newX, newY);
    }

    private void OnTileZChanged(sbyte newZ)
    {
        _block?.Landscape.OnStaticTileElevated(this, newZ);
    }

    private void OnTileHueChanged(ushort newHue)
    {
        _block?.Landscape.OnStaticTileHued(this, newHue);
    }

    public override string ToString()
    {
        return $"Static {Id}:{X},{Y},{Z} {Hue}";
    }
    
    public override string ShortString()
    {
        return $"Static 0x{Id:x}";
    }
}