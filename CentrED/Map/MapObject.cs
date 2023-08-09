using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map; 

public class MapObject<T> where T : Tile {
    public T root;
    public Texture2D Texture;
    public Vector3[] Vertices = new Vector3[4];
    public Vector3[] Normals = new Vector3[4];
    public Vector3[] TexCoords = new Vector3[4];
}