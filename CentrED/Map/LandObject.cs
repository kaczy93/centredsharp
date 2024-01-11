using CentrED.Renderer;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class LandObject : TileObject
{
    public LandTile LandTile;

    public LandObject(LandTile tile)
    {
        Tile = LandTile = tile;
        
        Vector4 cornerZ = AlwaysFlat() ? new Vector4(tile.Z * TILE_Z_SCALE) : GetCornerZ(tile);

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

    private bool AlwaysFlat()
    {
        ref var tileData = ref TileDataLoader.Instance.LandData[Tile.Id];
        // Water tiles are always flat
        return tileData.TexID == 0 && tileData.IsWet;
    }

    private bool IsFlat(float x, float y, float z, float w)
    {
        return x == y && x == z && x == w;
    }
    
    public void UpdateId(ushort newId)
    {
        Rectangle bounds;
        var isStretched = !IsFlat(Vertices[0].Position.Z, Vertices[1].Position.Z, Vertices[2].Position.Z, Vertices[3].Position.Z);
        var isTexMapValid = TexmapsLoader.Instance.GetValidRefEntry(newId).Length > 0;
        if (isTexMapValid && !AlwaysFlat())
        {
            isStretched |= CalculateNormals(out var normals);
            for (int i = 0; i < 4; i++)
            {
                Vertices[i].HueVec = normals[i];
            }
        }
        var useTexMap = isTexMapValid && (Config.Instance.PreferTexMaps || isStretched);
        if (useTexMap)
        {
            Texture = TexmapsLoader.Instance.GetLandTexture(newId, out bounds);
        }
        else
        {
            Texture = ArtLoader.Instance.GetLandTexture(newId, out bounds);
        }

        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);

        var texX = bounds.X / (float)Texture.Width + onePixel / 2f;
        var texY = bounds.Y / (float)Texture.Height + onePixel / 2f;
        var texWidth = bounds.Width / (float)Texture.Width - onePixel;
        var texHeight = bounds.Height / (float)Texture.Height - onePixel;
        
        var texCoords = new Vector3[4];
        var strechedFlag = isStretched ? 0.00001f : 0f;
        if (useTexMap)
        {
            texCoords[0] = new Vector3(texX, texY, strechedFlag);
            texCoords[1] = new Vector3(texX + texWidth, texY, strechedFlag);
            texCoords[2] = new Vector3(texX, texY + texHeight, strechedFlag);
            texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, strechedFlag);
        }
        else
        {
            texCoords[0] = new Vector3(texX + texWidth / 2f, texY, strechedFlag);
            texCoords[1] = new Vector3(texX + texWidth, texY + texHeight / 2f, strechedFlag);
            texCoords[2] = new Vector3(texX, texY + texHeight / 2f, strechedFlag);
            texCoords[3] = new Vector3(texX + texWidth / 2f, texY + texHeight, strechedFlag);
        }
        for (int i = 0; i < 4; i++)
        {
            Vertices[i].TextureCoordinate = texCoords[i];
        }
        
    }
    
    public void UpdateRightCorner(float z)
    {
        if (AlwaysFlat())
            return;
        
        Vertices[1].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id); //Reassign same Id, to reconsider art vs tex
    }
    public void UpdateLeftCorner(float z)
    {
        if (AlwaysFlat())
            return;
        
        Vertices[2].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id);
    }
    
    public void UpdateBottomCorner(float z)
    {
        if (AlwaysFlat())
            return;
        
        Vertices[3].Position.Z = z * TILE_Z_SCALE;
        UpdateId(LandTile.Id);
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
    
    private bool CalculateNormals(out Vector3[] normals)
    {
        normals = new Vector3[4];
        var client = Application.CEDClient;
        var x = Tile.X;
        var y = Tile.Y;
        /*  _____ _____ _____ _____
         * |     | t10 | t20 |     |
         * |_____|_____|_____|_____|
         * | t01 |  z  | t21 | t31 |
         * |_____|_____|_____|_____|
         * | t02 | t12 | t22 | t32 |
         * |_____|_____|_____|_____|
         * |     | t13 | t23 |     |
         * |_____|_____|_____|_____|
         */
        client.TryGetLandTile(x, y - 1, out var t10);
        client.TryGetLandTile(x + 1, y - 1, out var t20);
        client.TryGetLandTile(x - 1, y, out var t01);
        client.TryGetLandTile(x + 1, y, out var t21);
                                                client.TryGetLandTile(x + 2, y, out var t31);
                                                client.TryGetLandTile(x - 1, y + 1, out var t02);
                                                client.TryGetLandTile(x, y + 1, out var t12);
                                                client.TryGetLandTile(x + 1, y + 1, out var t22);
        client.TryGetLandTile(x + 2, y + 1, out var t32);
        client.TryGetLandTile(x, y + 2, out var t13);
        client.TryGetLandTile(x + 1, y + 2, out var t23);
        
        //TODO update normals when missing tile is loaded, same as corners are updated
        //TODO handle missing t21,t22,t12
        var isStretched = false;
        isStretched |= CalculateNormal(LandTile, t10, t21, t12, t01, out normals[0]);
        isStretched |= CalculateNormal(t21 ?? LandTile, t20, t31, t22, LandTile, out normals[1]);
        isStretched |= CalculateNormal(t22 ?? LandTile, t21, t32, t23, t12, out normals[2]);
        isStretched |= CalculateNormal(t12 ?? LandTile, LandTile, t22, t13, t02, out normals[3]);

        return isStretched;
    }
    
    //Thank you ClassicUO :)
    private bool CalculateNormal(LandTile tile, LandTile? top, LandTile? right, LandTile? bottom, LandTile? left, out Vector3 normal)
    {
        var tileZ = tile.Z;
        var topZ = top?.Z ?? tile.Z;
        var rightZ = right?.Z ?? tile.Z;
        var bottomZ = bottom?.Z ?? tile.Z;
        var leftZ = left?.Z ?? tile.Z;
        if (tileZ == topZ && tileZ == rightZ && tileZ == bottomZ && tileZ == leftZ)
        {
            normal.X = 0;
            normal.Y = 0;
            normal.Z = 1f;

            return false;
        }

        Vector3 u = new Vector3();
        Vector3 v = new Vector3();
        Vector3 ret = new Vector3();


        // ========================== 
        u.X = -22;
        u.Y = -22;
        u.Z = (leftZ - tileZ) * 4;

        v.X = -22;
        v.Y = 22;
        v.Z = (bottomZ - tileZ) * 4;

        Vector3.Cross(ref v, ref u, out ret);
        // ========================== 


        // ========================== 
        u.X = -22;
        u.Y = 22;
        u.Z = (bottomZ - tileZ) * 4;

        v.X = 22;
        v.Y = 22;
        v.Z = (rightZ - tileZ) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 


        // ========================== 
        u.X = 22;
        u.Y = 22;
        u.Z = (rightZ - tileZ) * 4;

        v.X = 22;
        v.Y = -22;
        v.Z = (topZ - tileZ) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 


        // ========================== 
        u.X = 22;
        u.Y = -22;
        u.Z = (topZ - tileZ) * 4;

        v.X = -22;
        v.Y = -22;
        v.Z = (leftZ - tileZ) * 4;

        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 


        Vector3.Normalize(ref ret, out normal);

        return true;
    }
}