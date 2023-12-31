using CentrED.Renderer;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class VirtualLayerTile : TileObject
{
    public VirtualLayerTile(ushort x = 0, ushort y = 0, sbyte z = 0)
    {
        Tile = new LandTile(0, x, y, z);
        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(Vector3.Zero,Vector3.Zero, Vector3.Zero);
        }
    }
}