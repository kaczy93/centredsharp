using CentrED.Lights;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using static CentrED.Constants;

namespace CentrED.Map;

public class LandObject : TileObject
{
    public LandTile LandTile;

    public bool IsGhost => LandTile.Block == null;
    
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

        Update();
    }

    public void Update()
    {
        UpdateCorners(Tile.Id);
        UpdateId(Tile.Id);
    }

    private bool AlwaysFlat(ushort id)
    {
        ref var tileData = ref CEDGame.MapManager.UoFileManager.TileData.LandData[id];
        // Water tiles are always flat
        return tileData.TexID == 0 || tileData.IsWet;
    }

    private bool IsFlat(float x, float y, float z, float w)
    {
        return x == y && x == z && x == w;
    }

    public void UpdateCorners(ushort id)
    {
        var alwaysFlat = AlwaysFlat(id);
        var flatView = CEDGame.MapManager.FlatView;
        Vector4 cornerZ = flatView ? Vector4.Zero : alwaysFlat ? new Vector4(Tile.Z * TILE_Z_SCALE) : GetCornerZ();

        var posX = (Tile.X - 1) * TILE_SIZE;
        var posY = (Tile.Y - 1) * TILE_SIZE;

        Vertices[0].Position = new Vector3(posX, posY, cornerZ.X);
        Vertices[1].Position = new Vector3(posX + TILE_SIZE, posY, cornerZ.Y);
        Vertices[2].Position = new Vector3(posX, posY + TILE_SIZE, cornerZ.Z);
        Vertices[3].Position = new Vector3(posX + TILE_SIZE, posY + TILE_SIZE, cornerZ.W);
    }
    
    public void UpdateId(ushort newId)
    {
        var mapManager = CEDGame.MapManager;
        SpriteInfo spriteInfo = default;
        var isStretched = !IsFlat
            (Vertices[0].Position.Z, Vertices[1].Position.Z, Vertices[2].Position.Z, Vertices[3].Position.Z);
        var isTexMapValid = CEDGame.MapManager.UoFileManager.Texmaps.File.GetValidRefEntry(newId).Length > 0;
        var isLandTileValid = CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(newId).Length > 0;
        var alwaysFlat = AlwaysFlat(newId);
        if (mapManager.FlatView)
        {
            isStretched = false;
            for (int i = 0; i < 4; i++)
            {
                Vertices[i].Normal = Vector3.Up;
            }
        }
        else if (isTexMapValid && !alwaysFlat)
        {
            isStretched |= CalculateNormals(out var normals);
            for (int i = 0; i < 4; i++)
            {
                Vertices[i].Normal = normals[i];
            }
        }
        var useTexMap = !alwaysFlat && isTexMapValid && (Config.Instance.PreferTexMaps || isStretched || !isLandTileValid);
        if (newId < 0x4000)
        {
            if (useTexMap)
            {
                spriteInfo = mapManager.Texmaps.GetTexmap(CEDGame.MapManager.UoFileManager.TileData.LandData[newId].TexID);
            }
            else
            {
                spriteInfo = mapManager.Arts.GetLand(newId);
               
            }
        }
        
        if (spriteInfo.Equals(SpriteInfo.Empty))
        {
            if(mapManager.DebugLogging)
                Console.WriteLine($"No texture found for land {Tile.X},{Tile.Y},{Tile.Z}:0x{newId:X}, texmap:{useTexMap}");
            //VOID texture is by default all pink, so it should be noticeable that something is not right
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(0x0001);
        }
        
        Texture = spriteInfo.Texture;
        var bounds = spriteInfo.UV;
        
        var texX = bounds.X / (float)Texture.Width + Epsilon.value;
        var texY = bounds.Y / (float)Texture.Height + Epsilon.value;
        var texWidth = bounds.Width / (float)Texture.Width - Epsilon.value;
        var texHeight = bounds.Height / (float)Texture.Height - Epsilon.value;
        
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
    
    private Vector4 GetCornerZ()
    {
        var client = CEDClient;
        var mapManager = CEDGame.MapManager;
        var x = Tile.X;
        var y = Tile.Y;
        var top = IsGhost ? 
            Tile : mapManager.TryGetLandTile(Tile.X, Tile.Y, out var topTile) ? 
                topTile : Tile;
        var right = mapManager.TryGetLandTile
            (Math.Min(client.Width * 8 - 1, x + 1), y, out var rightTile) ?
            rightTile :
            Tile;

        var left = mapManager.TryGetLandTile(x, Math.Min(client.Height * 8 - 1, y + 1), out var leftTile) ? leftTile : Tile;
        var bottom = mapManager.TryGetLandTile
            (Math.Min(client.Width * 8 - 1, x + 1), Math.Min(client.Height * 8 - 1, y + 1), out var bottomTile) ?
            bottomTile :
            Tile;

        return new Vector4(top.Z, right.Z, left.Z, bottom.Z) * TILE_Z_SCALE;
    }
    
    //Thank you ClassicUO :)
    private bool CalculateNormals(out Vector3[] normals)
    {
        normals = new Vector3[4];
        var mapManager = CEDGame.MapManager;
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
        mapManager.TryGetLandTile(x, y - 1, out var t10);
        mapManager.TryGetLandTile(x + 1, y - 1, out var t20);
        mapManager.TryGetLandTile(x - 1, y, out var t01);
        mapManager.TryGetLandTile(x + 1, y, out var t21);
        mapManager.TryGetLandTile(x + 2, y, out var t31);
        mapManager.TryGetLandTile(x - 1, y + 1, out var t02);
        mapManager.TryGetLandTile(x, y + 1, out var t12);
        mapManager.TryGetLandTile(x + 1, y + 1, out var t22);
        mapManager.TryGetLandTile(x + 2, y + 1, out var t32);
        mapManager.TryGetLandTile(x, y + 2, out var t13);
        mapManager.TryGetLandTile(x + 1, y + 2, out var t23);
        
        //TODO handle missing t21,t22,t12
        var isStretched = false;
        isStretched |= CalculateNormal(LandTile, t10, t21, t12, t01, out normals[0]);
        isStretched |= CalculateNormal(t21 ?? LandTile, t20, t31, t22, LandTile, out normals[1]);
        isStretched |= CalculateNormal(t12 ?? LandTile, LandTile, t22, t13, t02, out normals[2]);
        isStretched |= CalculateNormal(t22 ?? LandTile, t21, t32, t23, t12, out normals[3]);
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

        if (LightsManager.Instance.ClassicUONormals)
        {
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
        }
        else
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
        }
        return true;
    }
}