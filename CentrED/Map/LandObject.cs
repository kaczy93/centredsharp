using CentrED.Client;
using CentrED.Renderer;
using ClassicUO.Assets;
using ClassicUO.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map; 

public class LandObject : MapObject {
    public LandTile LandTile;

    public LandObject(CentrEDClient client, LandTile tile) {
        Tile = tile;
        LandTile = tile;
        ref var tileData = ref TileDataLoader.Instance.LandData[tile.Id];

        Vector4 cornerZ;
        Vector3 normalTop;
        Vector3 normalRight;
        Vector3 normalLeft;
        Vector3 normalBottom;
        
        if ((tileData.Flags & TileFlag.Wet) != 0) { // Water tiles are always flat
            cornerZ = new Vector4(tile.Z * TILE_Z_SCALE);
            normalTop = normalRight = normalLeft = normalBottom = Vector3.UnitZ;
        }
        else {
            cornerZ = GetCornerZ(client, tile);
            normalTop = ComputeNormal(client, tile.X, tile.Y);
            normalRight = ComputeNormal(client, tile.X + 1, tile.Y);
            normalLeft = ComputeNormal(client, tile.X, tile.Y + 1);
            normalBottom = ComputeNormal(client, tile.X + 1, tile.Y + 1);
        }
        
        var normals = new Vector3[4];
        normals[0] = normalTop;
        normals[1] = normalRight;
        normals[2] = normalLeft;
        normals[3] = normalBottom;
        
        var posX = (tile.X - 1) * TILE_SIZE;
        var posY = (tile.Y - 1) * TILE_SIZE;

        var coordinates = new Vector3[4];
        coordinates[0] = new Vector3(posX, posY, cornerZ.X);
        coordinates[1] = new Vector3(posX + TILE_SIZE, posY, cornerZ.Y);
        coordinates[2] = new Vector3(posX, posY + TILE_SIZE, cornerZ.Z);
        coordinates[3] = new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W);


        Texture2D? tileTex = null;
        Rectangle bounds;
        var diamondTexture = IsFlat(cornerZ.X, cornerZ.Y, cornerZ.Z, cornerZ.W) || TexmapsLoader.Instance.GetValidRefEntry(tile.Id).Equals(UOFileIndex.Invalid);
        if (diamondTexture) {
            Texture = ArtLoader.Instance.GetLandTexture(tile.Id, out bounds);
        }
        else {
            Texture = TexmapsLoader.Instance.GetLandTexture(tile.Id, out bounds);
        }
        
        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        
        var texX = bounds.X / (float)Texture.Width + (onePixel / 2f);
        var texY = bounds.Y / (float)Texture.Height + (onePixel / 2f);
        var texWidth = (bounds.Width / (float)Texture.Width) - onePixel;
        var texHeight = (bounds.Height / (float)Texture.Height) - onePixel;

        var texCoords = new Vector3[4];
        if (diamondTexture)
        {
            texCoords[0] = new Vector3(texX + texWidth / 2f, texY, 0);
            texCoords[1] = new Vector3(texX + texWidth, texY + texHeight / 2f, 0);
            texCoords[2] = new Vector3(texX, texY + texHeight / 2f, 0);
            texCoords[3] = new Vector3(texX + texWidth / 2f, texY + texHeight, 0);
        }
        else
        {
            texCoords[0] = new Vector3(texX, texY, 0);
            texCoords[1] = new Vector3(texX + texWidth, texY, 0);
            texCoords[2] = new Vector3(texX, texY + texHeight, 0);
            texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, 0);
        }

        for (int i = 0; i < 4; i++) {
            Vertices[i] = new MapVertex(coordinates[i], normals[i], texCoords[i], Vector3.Zero);
        }
    }
    
    private bool IsFlat(float x, float y, float z, float w) {
        return x == y && x == z && x == w;
    }
    
    private Vector4 
        GetCornerZ(CentrEDClient client, LandTile tile) {
        var x = tile.X;
        var y = tile.Y;
        var top = tile;
        var right = client.GetLandTile(Math.Min(client.Width * 8 - 1, x + 1), y);
        var left = client.GetLandTile(x, Math.Min(client.Height * 8 - 1, y + 1));
        var bottom = client.GetLandTile(Math.Min(client.Width * 8 - 1, x + 1), Math.Min(client.Height * 8 - 1, y + 1));

        return new Vector4(
            top.Z * TILE_Z_SCALE,
            right.Z * TILE_Z_SCALE,
            left.Z * TILE_Z_SCALE,
            bottom.Z * TILE_Z_SCALE
        );
    }
    
    private static (Vector2, Vector2)[] _normalOffsets = 
    {
        (new Vector2(1, 0), new Vector2(0, 1)),
        (new Vector2(0, 1), new Vector2(-1, 0)),
        (new Vector2(-1, 0), new Vector2(0, -1)),
        (new Vector2(0, -1), new Vector2(1, 0))
    };
    
    
    private Vector3 ComputeNormal(CentrEDClient client, int tileX, int tileY)
    {
        var t = client.GetLandTile(Math.Clamp(tileX, 0, client.Width * 8 - 1), Math.Clamp(tileY, 0, client.Height * 8 - 1));

        Vector3 normal = Vector3.Zero;

        for (int i = 0; i < _normalOffsets.Length; i++)
        {
            (var tu, var tv) = _normalOffsets[i];

            var tx = client.GetLandTile(Math.Clamp((int)(tileX + tu.X), 0, client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, client.Height * 8 - 1));
            var ty = client.GetLandTile(Math.Clamp((int)(tileX + tv.X), 0, client.Width * 8 - 1), Math.Clamp((int)(tileY + tu.Y), 0, client.Height * 8 - 1));

            if (tx.Id == 0 || ty.Id == 0)
                continue;

            Vector3 u = new Vector3(tu.X * TILE_SIZE, tu.Y * TILE_SIZE, tx.Z - t.Z);
            Vector3 v = new Vector3(tv.X * TILE_SIZE, tv.Y * TILE_SIZE, ty.Z - t.Z);

            var tmp = Vector3.Cross(u, v);
            normal = Vector3.Add(normal, tmp);
        }

        return Vector3.Normalize(normal);
    }

    public void UpdateId(ushort newId) {
        Texture2D? tileTex = null;
        Rectangle bounds;
        var isFlat = IsFlat(Vertices[0].Position.Z, Vertices[1].Position.Z, Vertices[2].Position.Z, Vertices[3].Position.Z);
        var diamondTexture = isFlat || TexmapsLoader.Instance.GetValidRefEntry(newId).Equals(UOFileIndex.Invalid);
        if (diamondTexture) {
            Texture = ArtLoader.Instance.GetLandTexture(newId, out bounds);
        }
        else {
            Texture = TexmapsLoader.Instance.GetLandTexture(newId, out bounds);
        }
        
        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        
        var texX = bounds.X / (float)Texture.Width + (onePixel / 2f);
        var texY = bounds.Y / (float)Texture.Height + (onePixel / 2f);
        var texWidth = (bounds.Width / (float)Texture.Width) - onePixel;
        var texHeight = (bounds.Height / (float)Texture.Height) - onePixel;

        var texCoords = new Vector3[4];
        if (diamondTexture)
        {
            texCoords[0] = new Vector3(texX + texWidth / 2f, texY, 0);
            texCoords[1] = new Vector3(texX + texWidth, texY + texHeight / 2f, 0);
            texCoords[2] = new Vector3(texX, texY + texHeight / 2f, 0);
            texCoords[3] = new Vector3(texX + texWidth / 2f, texY + texHeight, 0);
        }
        else
        {
            texCoords[0] = new Vector3(texX, texY, 0);
            texCoords[1] = new Vector3(texX + texWidth, texY, 0);
            texCoords[2] = new Vector3(texX, texY + texHeight, 0);
            texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, 0);
        }
        
        for (int i = 0; i < 4; i++) {
            Vertices[i].TextureCoordinate = texCoords[i];
        }
    }
}