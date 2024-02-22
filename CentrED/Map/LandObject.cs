using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class LandObject : TileObject
{
    public LandTile LandTile;
    
    public sbyte AverageZ() //TODO Calculate me once
    { 
        int zTop = (int)(Vertices[0].Position.Z / TILE_Z_SCALE);
        int zRight= (int)(Vertices[1].Position.Z/ TILE_Z_SCALE);
        int zLeft= (int)(Vertices[2].Position.Z/ TILE_Z_SCALE);
        int zBottom= (int)(Vertices[3].Position.Z/ TILE_Z_SCALE);
        if (Math.Abs(zTop - zBottom) <= Math.Abs(zLeft - zRight))
        {
            return(sbyte) ((zTop + zBottom) >> 1);
        }
        else
        {
            return (sbyte) ((zLeft + zRight) >> 1);
        }
    }

    public LandObject(LandTile tile)
    {
        Tile = LandTile = tile;

        UpdateCorners(Tile.Id);
        UpdateId(Tile.Id);
    }

    private bool AlwaysFlat(ushort id)
    {
        ref var tileData = ref TileDataLoader.Instance.LandData[id];
        // Water tiles are always flat
        return tileData.TexID == 0 || tileData.IsWet;
    }

    private bool IsFlat(float x, float y, float z, float w)
    {
        return x == y && x == z && x == w;
    }

    public void UpdateCorners(ushort id)
    {
        Vector4 cornerZ = AlwaysFlat(id) ? new Vector4(Tile.Z * TILE_Z_SCALE) : GetCornerZ();

        var posX = (Tile.X - 1) * TILE_SIZE;
        var posY = (Tile.Y - 1) * TILE_SIZE;

        var coordinates = new Vector3[4];
        coordinates[0] = new Vector3(posX, posY, cornerZ.X);
        coordinates[1] = new Vector3(posX + TILE_SIZE, posY, cornerZ.Y);
        coordinates[2] = new Vector3(posX, posY + TILE_SIZE, cornerZ.Z);
        coordinates[3] = new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W);
        
        for (int i = 0; i < 4; i++)
        {
            Vertices[i].Position = coordinates[i];
        }
    }
    
    public void UpdateId(ushort newId)
    {
        Rectangle bounds;
        var isStretched = !IsFlat(Vertices[0].Position.Z, Vertices[1].Position.Z, Vertices[2].Position.Z, Vertices[3].Position.Z);
        var isTexMapValid = TexmapsLoader.Instance.GetValidRefEntry(newId).Length > 0;
        var isLandTileValid = ArtLoader.Instance.GetValidRefEntry(newId).Length > 0;
        if (isTexMapValid && !AlwaysFlat(newId))
        {
            isStretched |= CalculateNormals(out var normals);
            for (int i = 0; i < 4; i++)
            {
                Vertices[i].Normal = normals[i];
            }
        }
        var useTexMap = isTexMapValid && (Config.Instance.PreferTexMaps || isStretched || !isLandTileValid);
        if (useTexMap)
        {
            Texture = TexmapsLoader.Instance.GetLandTexture(TileDataLoader.Instance.LandData[newId].TexID, out bounds);
        }
        else
        {
            Texture = ArtLoader.Instance.GetLandTexture(newId, out bounds);
        }
        
        if (Texture == null)
        {
            Console.WriteLine($"No texture found for land {Tile.X},{Tile.Y},{Tile.Z}:0x{newId:X}, texmap:{useTexMap}");
            Valid = false;
            return;
        }

        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);

        var texX = bounds.X / (float)Texture.Width + onePixel / 2f;
        var texY = bounds.Y / (float)Texture.Height + onePixel / 2f;
        var texWidth = bounds.Width / (float)Texture.Width - onePixel;
        var texHeight = bounds.Height / (float)Texture.Height - onePixel;
        
        var texCoords = new Vector3[4];
        var applyLightingFlag = useTexMap ? 0.00001f : 0f;
        if (useTexMap)
        {
            texCoords[0] = new Vector3(texX, texY, applyLightingFlag);
            texCoords[1] = new Vector3(texX + texWidth, texY, applyLightingFlag);
            texCoords[2] = new Vector3(texX, texY + texHeight, applyLightingFlag);
            texCoords[3] = new Vector3(texX + texWidth, texY + texHeight, applyLightingFlag);
        }
        else
        {
            texCoords[0] = new Vector3(texX + texWidth / 2f, texY, applyLightingFlag);
            texCoords[1] = new Vector3(texX + texWidth, texY + texHeight / 2f, applyLightingFlag);
            texCoords[2] = new Vector3(texX, texY + texHeight / 2f, applyLightingFlag);
            texCoords[3] = new Vector3(texX + texWidth / 2f, texY + texHeight, applyLightingFlag);
        }
        for (int i = 0; i < 4; i++)
        {
            Vertices[i].Texture = texCoords[i];
        }
    }
    
    public void UpdateRightCorner(float z)
    {
        if (AlwaysFlat(Tile.Id))
            return;
        
        Vertices[1].Position.Z = z * TILE_Z_SCALE;
        UpdateId(Tile.Id); //Reassign same Id, to reconsider art vs tex
    }
    public void UpdateLeftCorner(float z)
    {
        if (AlwaysFlat(Tile.Id))
            return;
        
        Vertices[2].Position.Z = z * TILE_Z_SCALE;
        UpdateId(Tile.Id);
    }
    
    public void UpdateBottomCorner(float z)
    {
        if (AlwaysFlat(Tile.Id))
            return;
        
        Vertices[3].Position.Z = z * TILE_Z_SCALE;
        UpdateId(Tile.Id);
    }

    private Vector4 GetCornerZ()
    {
        var client = Application.CEDClient;
        var x = Tile.X;
        var y = Tile.Y;
        var top = Tile;
        var right = client.TryGetLandTile
            (Math.Min(client.Width * 8 - 1, x + 1), y, out var rightTile) ?
            rightTile :
            Tile;

        var left = client.TryGetLandTile(x, Math.Min(client.Height * 8 - 1, y + 1), out var leftTile) ? leftTile : Tile;
        var bottom = client.TryGetLandTile
            (Math.Min(client.Width * 8 - 1, x + 1), Math.Min(client.Height * 8 - 1, y + 1), out var bottomTile) ?
            bottomTile :
            Tile;

        return new Vector4(top.Z, right.Z, left.Z, bottom.Z) * TILE_Z_SCALE;
    }
    
    //Thank you ClassicUO :)
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
        
        //TODO handle missing t21,t22,t12
        var isStretched = false;
        isStretched |= CalculateNormal(LandTile, t10, t21, t12, t01, out normals[0]);
        isStretched |= CalculateNormal(t21 ?? LandTile, t20, t31, t22, LandTile, out normals[1]);
        isStretched |= CalculateNormal(t22 ?? LandTile, t21, t32, t23, t12, out normals[2]);
        isStretched |= CalculateNormal(t12 ?? LandTile, LandTile, t22, t13, t02, out normals[3]);

        return isStretched;
    }
    
    private bool CalculateNormal(LandTile tile, LandTile? top, LandTile? right, LandTile? bottom, LandTile? left, out Vector3 normal)
    {
        var tileZ = tile.Z;
        LandTile topTile = top ?? tile;
        LandTile rightTile = right ?? tile;
        LandTile bottomTile = bottom ?? tile;
        LandTile leftTile = left ?? tile;
        if (tileZ == topTile.Z && tileZ == rightTile.Z && tileZ == bottomTile.Z && tileZ == leftTile.Z)
        {
            normal.X = 0;
            normal.Y = 0;
            normal.Z = 1f;

            return false;
        }

        var pairs = new []
        {
            (leftTile, topTile), (topTile, rightTile), (rightTile, bottomTile), (bottomTile, leftTile)
        };
        
        
        Vector3 u = new Vector3();
        Vector3 v = new Vector3();
        Vector3 ret = new Vector3();
        normal = new Vector3();

        if (!Application.CEDGame.UIManager.DebugWindow.ClassicUONormals)
        {
            foreach (var (tx, ty) in pairs)
            {
                u.X = (tx.X - tile.X) * TILE_SIZE;
                u.Y = (tx.Y - tile.Y) * TILE_SIZE;
                u.Z = (tx.Z - tile.Z) * TILE_Z_SCALE;
                v.X = (ty.X - tile.X) * TILE_SIZE;
                v.Y = (ty.Y - tile.Y) * TILE_SIZE;
                v.Z = (ty.Z - tile.Z) * TILE_Z_SCALE;
                Vector3.Cross(ref u, ref v, out var tmp);
                Vector3.Add(ref normal, ref tmp, out normal);
            }

            Vector3.Normalize(ref normal, out normal);
            
            return true;
        }
        
        // ========================== 
        u.X = -22;
        u.Y = -22;
        u.Z = (leftTile.Z - tileZ) * 4;
        
        v.X = -22;
        v.Y = 22;
        v.Z = (bottomTile.Z - tileZ) * 4;
        
        Vector3.Cross(ref v, ref u, out ret);
        // ========================== 
        
        
        // ========================== 
        u.X = -22;
        u.Y = 22;
        u.Z = (bottomTile.Z - tileZ) * 4;
        
        v.X = 22;
        v.Y = 22;
        v.Z = (rightTile.Z - tileZ) * 4;
        
        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 
        
        
        // ========================== 
        u.X = 22;
        u.Y = 22;
        u.Z = (rightTile.Z - tileZ) * 4;
        
        v.X = 22;
        v.Y = -22;
        v.Z = (topTile.Z - tileZ) * 4;
        
        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 
        
        
        // ========================== 
        u.X = 22;
        u.Y = -22;
        u.Z = (topTile.Z - tileZ) * 4;
        
        v.X = -22;
        v.Y = -22;
        v.Z = (leftTile.Z - tileZ) * 4;
        
        Vector3.Cross(ref v, ref u, out normal);
        Vector3.Add(ref ret, ref normal, out ret);
        // ========================== 
        
        
        Vector3.Normalize(ref ret, out normal);

        return true;
    }
}