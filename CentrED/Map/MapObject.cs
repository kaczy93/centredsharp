using CentrED.Renderer;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map;

public abstract class MapObject
{
    public const float INVERSE_SQRT2 = 0.70711f;
    public const float TILE_SIZE = 31.11f;
    public const float TILE_Z_SCALE = 4.0f;

    public BaseTile Tile;

    public bool Visible = true;
    public Texture2D Texture;
    public MapVertex[] Vertices = new MapVertex[4];

    public ushort Hue
    {
        set
        {
            for (var index = 0; index < Vertices.Length; index++)
            {
                Vertices[index].HueVec = HuesManager.Instance.GetHueVector(Tile.Id, value, Vertices[index].HueVec.Z);
            }
        }
    }

    public float Alpha
    {
        set
        {
            for (var index = 0; index < Vertices.Length; index++)
            {
                Vertices[index].HueVec.Z = value;
            }
        }
    }
}