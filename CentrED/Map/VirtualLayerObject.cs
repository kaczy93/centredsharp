using CentrED.Renderer;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class VirtualLayerObject : MapObject
{
    private static VirtualLayerObject _instance = new();
    public static VirtualLayerObject Instance => _instance;

    private ushort _width;
    private ushort _height;
    private sbyte _z;
    
    public ushort Width
    {
        get => _width;
        set
        {
            _width = value;
            Vertices[1].Position.X = _width * TILE_SIZE;
            Vertices[3].Position.X = _width * TILE_SIZE;
        }
    }

    public ushort Height
    {
        get => _height;
        set
        {
            _height = value; 
            Vertices[2].Position.Y = _height * TILE_SIZE;
            Vertices[3].Position.Y = _height * TILE_SIZE;
        }
    }

    public sbyte Z
    {
        get => _z;
        set
        {
            _z = value;
            for (var i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Position.Z = _z * TILE_Z_SCALE;
            }
        }
    }

    public Vector3 Color
    {
        set
        {
            for (var i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].HueVec = value;
            }
        }
    }

    public float Alpha
    {
        set
        {
            for (var i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].TextureCoordinate.X = value;
            }
        }
    }

    private VirtualLayerObject()
    {
        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
        }
    }
}