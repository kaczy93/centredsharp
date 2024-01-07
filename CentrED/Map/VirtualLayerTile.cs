using CentrED.Renderer;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class VirtualLayerTile : TileObject
{
    private readonly int _hash;
    public VirtualLayerTile(ushort x = 0, ushort y = 0, sbyte z = 0)
    {
        _hash = HashCode.Combine(x, y, z);
        Tile = new LandTile(0, x, y, z);
        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(Vector3.Zero,Vector3.Zero, Vector3.Zero);
        }
    }

    protected bool Equals(VirtualLayerTile other)
    {
        return _hash == other._hash;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != this.GetType())
        {
            return false;
        }
        return Equals((VirtualLayerTile)obj);
    }

    public override int GetHashCode()
    {
        return _hash;
    }
}