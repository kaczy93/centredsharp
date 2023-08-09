using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace CentrED.Map; 

public class StaticObject : MapObject<StaticTile> {
    private const float INVERSE_SQRT2 = 0.70711f;
    private const float TILE_SIZE = 31.11f;
    private const float TILE_Z_SCALE = 4.0f;
    public StaticObject(StaticTile tile, int depthOffsetIndex) {
        root = tile;

        var posX = (tile.X + 1) * TILE_SIZE;
        var posY = (tile.Y + 1) * TILE_SIZE;
        var posZ = tile.Z * TILE_Z_SCALE;
        
        Texture = ArtLoader.Instance.GetStaticTexture(tile.Id, out var bounds);
        var projectedWidth = (bounds.Width / 2f) * INVERSE_SQRT2;
        Vertices[0] = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ + bounds.Height);
        Vertices[1] = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ + bounds.Height);
        Vertices[2] = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ);
        Vertices[3] = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ);

        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        
        var texX = bounds.X / (float)Texture.Width + (onePixel / 2f);
        var texY = bounds.Y / (float)Texture.Height + (onePixel / 2f);
        var texWidth = (bounds.Width / (float)Texture.Width) - onePixel;
        var texHeight = (bounds.Height / (float)Texture.Height) - onePixel;

        var depthOffset = depthOffsetIndex * 0.0001f;
        
        TexCoords[0] = new Vector3(texX, texY, depthOffset);
        TexCoords[1] = new Vector3(texX + texWidth, texY, depthOffset);
        TexCoords[2] = new Vector3(texX, texY + texHeight, depthOffset);
        TexCoords[3] = new Vector3(texX + texWidth, texY + texHeight, depthOffset);

        Normals[0] = Vector3.UnitZ;
        Normals[1] = Vector3.UnitZ;
        Normals[2] = Vector3.UnitZ;
        Normals[3] = Vector3.UnitZ;
    }
}