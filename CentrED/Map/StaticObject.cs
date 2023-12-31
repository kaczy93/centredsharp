using CentrED.Renderer;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class StaticObject : TileObject
{
    public const float INVERSE_SQRT2 = 0.70711f;
    public StaticTile StaticTile;

    public StaticObject(StaticTile tile)
    {
        ObjectId = GetNextId();
        Tile = tile;
        StaticTile = tile;

        var posX = tile.X * TILE_SIZE;
        var posY = tile.Y * TILE_SIZE;
        var posZ = tile.Z * TILE_Z_SCALE;

        Texture = ArtLoader.Instance.GetStaticTexture(tile.Id, out var bounds);
        var projectedWidth = (bounds.Width / 2f) * INVERSE_SQRT2;
        var depthOffset = tile.CellIndex * 0.0001f;

        var coordinates = new Vector3[4];
        coordinates[0] = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ + bounds.Height);
        coordinates[1] = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ + bounds.Height);
        coordinates[2] = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ);
        coordinates[3] = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ);

        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        var texX = bounds.X / (float)Texture.Width + onePixel / 2f;
        var texY = bounds.Y / (float)Texture.Height + onePixel / 2f;
        var texWidth = bounds.Width / (float)Texture.Width - onePixel;
        var texHeight = bounds.Height / (float)Texture.Height - onePixel;

        var texCoords = new Vector3[4];
        texCoords[0] = new Vector3(texX, texY, depthOffset);
        texCoords[1] = new Vector3(texX + texWidth, texY, depthOffset);
        texCoords[2] = new Vector3(texX, texY + texHeight, depthOffset);
        texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, depthOffset);

        var hue = HuesManager.Instance.GetHueVector(tile);
        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(coordinates[i], texCoords[i], hue);
        }
    }

    public void UpdatePos(ushort newX, ushort newY, sbyte newZ)
    {
        var posX = newX * TILE_SIZE;
        var posY = newY * TILE_SIZE;
        var posZ = newZ * TILE_Z_SCALE;
        
        Texture = ArtLoader.Instance.GetStaticTexture(Tile.Id, out var bounds);
        var projectedWidth = (bounds.Width / 2f) * INVERSE_SQRT2;
        
        Vertices[0].Position = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ + bounds.Height);
        Vertices[1].Position = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ + bounds.Height);
        Vertices[2].Position = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ);
        Vertices[3].Position = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ);
    }

    public void UpdateHue(ushort newHue)
    {
        var hueVec = HuesManager.Instance.GetHueVector(Tile.Id, newHue);
        for (int i = 0; i < 4; i++)
        {
            Vertices[i].HueVec = hueVec;
        }
    }
    
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
}