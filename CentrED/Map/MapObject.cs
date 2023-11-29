using CentrED.Renderer;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map;

public abstract class MapObject
{
    private static int NextObjectId = 1;

    public static int GetNextId()
    {
        var objectId = NextObjectId++;
        //This is crap, but should work for now
        if (NextObjectId < 0)
        {
            NextObjectId = 1;
            Application.CEDGame.MapManager.Reset();
        }
        return objectId;
    }
    
    public const float INVERSE_SQRT2 = 0.70711f;
    public const float TILE_SIZE = 31.11f;
    public const float TILE_Z_SCALE = 4.0f;
    
    public int ObjectId { get; protected set; }
    public bool Visible = true;
    public Texture2D Texture;
    public MapVertex[] Vertices = new MapVertex[4];
    
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