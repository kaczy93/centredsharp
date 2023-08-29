using CentrED.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map; 

public abstract class MapObject {
    public const float INVERSE_SQRT2 = 0.70711f;
    public const float TILE_SIZE = 31.11f;
    public const float TILE_Z_SCALE = 4.0f;
    
    public Texture2D Texture;
    public Vector3[] Coordinates = new Vector3[4];
    public Vector3[] Normals = new Vector3[4];
    public Vector3[] TexCoords = new Vector3[4];
    public Vector3 Hue;
    
    public MapVertex[] Vertices = new MapVertex[4];
}

public abstract class MapObject<T> : MapObject {
    public T root;
}