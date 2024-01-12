using CentrED.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map;

public abstract class MapObject
{
    private static int NextObjectId = 1;

    public MapObject()
    {
        ObjectId = GetNextId();
    }

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

    public readonly int ObjectId;
    public bool Visible = true;
    public Texture2D Texture;
    public Rectangle TextureBounds;
    public MapVertex[] Vertices = new MapVertex[4];
}