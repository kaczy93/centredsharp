using CentrED.Renderer;
using ClassicUO.Assets;
using ClassicUO.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace CentrED.Map;

public class LandObject : TileObject
{
    public LandTile LandTile;

    public LandObject(LandTile tile)
    {
        ObjectId = GetNextId();
        Tile = tile;
        LandTile = tile;
        ref var tileData = ref TileDataLoader.Instance.LandData[tile.Id];

        Vector4 cornerZ;

        if ((tileData.Flags & TileFlag.Wet) != 0)
        {
            // Water tiles are always flat
            cornerZ = new Vector4(tile.Z * TILE_Z_SCALE);
        }
        else
        {
            cornerZ = GetCornerZ(tile);
        }

        var posX = (tile.X - 1) * TILE_SIZE;
        var posY = (tile.Y - 1) * TILE_SIZE;

        var coordinates = new Vector3[4];
        coordinates[0] = new Vector3(posX, posY, cornerZ.X);
        coordinates[1] = new Vector3(posX + TILE_SIZE, posY, cornerZ.Y);
        coordinates[2] = new Vector3(posX, posY + TILE_SIZE, cornerZ.Z);
        coordinates[3] = new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W);

        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(coordinates[i], Vector3.Zero, Vector3.Zero);
        }
        UpdateId(Tile.Id);
    }

    private bool IsFlat(float x, float y, float z, float w)
    {
        return x == y && x == z && x == w;
    }

    private Vector4 GetCornerZ(LandTile tile)
    {
        var client = Application.CEDClient;
        var x = tile.X;
        var y = tile.Y;
        var top = tile;
        var right = client.TryGetLandTile(Math.Min(client.Width * 8 - 1, x + 1), y, out var rightTile) ? rightTile : tile;
        
        var left = client.TryGetLandTile(x, Math.Min(client.Height * 8 - 1, y + 1), out var leftTile) ? leftTile : tile;
        var bottom = client.TryGetLandTile(Math.Min(client.Width * 8 - 1, x + 1), Math.Min(client.Height * 8 - 1, y + 1), out var bottomTile) ? bottomTile : tile;

        return new Vector4(top.Z, right.Z, left.Z, bottom.Z) * TILE_Z_SCALE;
    }

    public void UpdateId(ushort newId)
    {
        Texture2D? tileTex = null;
        Rectangle bounds;
        var isFlat = IsFlat
            (Vertices[0].Position.Z, Vertices[1].Position.Z, Vertices[2].Position.Z, Vertices[3].Position.Z);
        var isTexMapValid = !TexmapsLoader.Instance.GetValidRefEntry(newId).Equals(UOFileIndex.Invalid);
        var useTexMap = isTexMapValid && (Config.Instance.PreferTexMaps || !isFlat);
        if (useTexMap)
        {
            Texture = TexmapsLoader.Instance.GetLandTexture(newId, out bounds);
        }
        else
        {
            Texture = ArtLoader.Instance.GetLandTexture(newId, out bounds);
        }

        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);

        var texX = bounds.X / (float)Texture.Width + (onePixel / 2f);
        var texY = bounds.Y / (float)Texture.Height + (onePixel / 2f);
        var texWidth = (bounds.Width / (float)Texture.Width) - onePixel;
        var texHeight = (bounds.Height / (float)Texture.Height) - onePixel;

        var texCoords = new Vector3[4];
        if (useTexMap)
        {
            texCoords[0] = new Vector3(texX, texY, 0f);
            texCoords[1] = new Vector3(texX + texWidth, texY, 0f);
            texCoords[2] = new Vector3(texX, texY + texHeight, 0f);
            texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, 0f);
        }
        else
        {
            texCoords[0] = new Vector3(texX + texWidth / 2f, texY, 0f);
            texCoords[1] = new Vector3(texX + texWidth, texY + texHeight / 2f, 0f);
            texCoords[2] = new Vector3(texX, texY + texHeight / 2f, 0f);
            texCoords[3] = new Vector3(texX + texWidth / 2f, texY + texHeight, 0f);
        }

        for (int i = 0; i < 4; i++)
        {
            Vertices[i].TextureCoordinate = texCoords[i];
        }
    }

    public void UpdateRightCorner(float z)
    {
        Vertices[1].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id); //Reassign same Id, to reconsider art vs tex
    }
    public void UpdateLeftCorner(float z)
    {
        Vertices[2].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id);
    }
    
    public void UpdateBottomCorner(float z)
    {
        Vertices[3].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id);
    }
}