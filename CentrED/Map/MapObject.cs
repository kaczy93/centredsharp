using System.Drawing;
using System.Numerics;
using CentrED.Renderer;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map;

public abstract class MapObject
{
    private static int NextObjectId = 1;

    public MapObject()
    {
        ObjectId = GetNextId();
        ObjectIdColor = new Vector4((ObjectId & 0xFF) / 255f, ((ObjectId >> 8) & 0xFF) / 255f, ((ObjectId >> 16) & 0xFF) / 255f, 1.0f);
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
    public readonly Vector4 ObjectIdColor;

    
    public bool Valid = true;
    public bool Visible = true;
    public bool CanDraw => Valid && Visible;
    public Texture2D Texture;
    public Rectangle TextureBounds;
    public MapVertex[] Vertices = new MapVertex[4];
}