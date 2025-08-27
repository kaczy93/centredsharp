using CentrED.Renderer;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using static CentrED.Constants;

namespace CentrED.Map;

public class StaticObject : TileObject, IComparable<StaticObject>
{
    public StaticTile StaticTile;
    public bool IsAnimated;
    public bool IsLight;
    public Rectangle RealBounds;

    public StaticObject(StaticTile tile)
    {
        //Static are constructed from two rectangles
        Vertices = new MapVertex[8];
        Tile = StaticTile = tile;
        
        RealBounds = Application.CEDGame.MapManager.Arts.GetRealArtBounds(Tile.Id);
        UpdateId(Tile.Id);
        UpdatePos(tile.X, tile.Y, tile.Z);
        UpdateHue(tile.Hue);
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Normal = Vector3.Zero;
        }
        var tiledata = TileDataLoader.Instance.StaticData[Tile.Id];
        IsAnimated = tiledata.IsAnimated;
        IsLight = tiledata.IsLight;
    }

    public void Update()
    {
        //Only UpdatePos for now, mainly for FlatView
        UpdatePos(Tile.X, Tile.Y, Tile.Z);
    }

    public void UpdateId()
    {
        UpdateId(Tile.Id);
    }
    
    public void UpdateId(ushort newId)
    {
        ref var index = ref ArtLoader.Instance.GetValidRefEntry(newId + 0x4000);
        var mapManager = Application.CEDGame.MapManager;
        var spriteInfo = mapManager.Arts.GetArt((uint)(newId + index.AnimOffset));
        if (spriteInfo.Equals(SpriteInfo.Empty))
        {
            if(mapManager.DebugLogging)
                Console.WriteLine($"No texture found for static {Tile.X},{Tile.Y},{Tile.Z}:0x{newId:X}");
            //VOID texture of land is by default all pink, so it should be noticeable that something is not right
            spriteInfo = Application.CEDGame.MapManager.Texmaps.GetTexmap(0x0001);
        }
        
        Texture = spriteInfo.Texture;
        TextureBounds = spriteInfo.UV;
        
        var texX = TextureBounds.X / (float)Texture.Width;
        var texY = TextureBounds.Y / (float)Texture.Height;
        var texWidth = TextureBounds.Width / (float)Texture.Width;
        var halfTexWidth = texWidth * 0.5f;
        var texHeight = TextureBounds.Height / (float)Texture.Height;

        //Left half
        Vertices[0].Texture = new Vector3(texX, texY, 0f);
        Vertices[1].Texture = new Vector3(texX + halfTexWidth, texY, 0f);
        Vertices[2].Texture = new Vector3(texX, texY + texHeight, 0f);
        Vertices[3].Texture = new Vector3(texX + halfTexWidth, texY + texHeight, 0f);
        
        //Right half
        Vertices[4].Texture = new Vector3(texX + halfTexWidth, texY, 0f);
        Vertices[5].Texture = new Vector3(texX + texWidth, texY, 0f);
        Vertices[6].Texture = new Vector3(texX + halfTexWidth, texY + texHeight, 0f);
        Vertices[7].Texture = new Vector3(texX + texWidth, texY + texHeight, 0f);
        UpdateDepthOffset();
        IsAnimated = TileDataLoader.Instance.StaticData[newId].IsAnimated;
    }

    public void UpdateDepthOffset()
    {
        var depthOffset = StaticTile.CellIndex * 0.0001f;
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Texture.Z = depthOffset;
        }
    }
    
    public void UpdatePos(ushort newX, ushort newY, sbyte newZ)
    {
        var flatStatics = Application.CEDGame.MapManager.FlatView && Application.CEDGame.MapManager.FlatStatics;
        var posX = newX * TILE_SIZE;
        var posY = newY * TILE_SIZE;
        var posZ = flatStatics ? 0 : newZ * TILE_Z_SCALE;

        float projectedWidth = TextureBounds.Width  * RSQRT2;
        float halfWidth = TextureBounds.Width * 0.5f;
        
        //Left half
        Vertices[0].Position = new Vector3(posX - projectedWidth, posY, posZ + TextureBounds.Height - halfWidth);
        Vertices[1].Position = new Vector3(posX, posY, posZ + TextureBounds.Height);
        Vertices[2].Position = new Vector3(posX - projectedWidth, posY, posZ - halfWidth);
        Vertices[3].Position = new Vector3(posX, posY, posZ);
        
        //Right Half
        Vertices[4].Position = new Vector3(posX, posY , posZ + TextureBounds.Height );
        Vertices[5].Position = new Vector3(posX, posY - projectedWidth, posZ + TextureBounds.Height - halfWidth);
        Vertices[6].Position = new Vector3(posX, posY , posZ);
        Vertices[7].Position = new Vector3(posX, posY - projectedWidth, posZ - halfWidth);
    }

    public void UpdateHue(ushort newHue)
    {
        var hueVec = HuesManager.Instance.GetHueVector(Tile.Id, newHue);
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Hue = hueVec;
        }
    }

    public override void Reset()
    {
        base.Reset();
        UpdateHue(StaticTile.Hue);
        _ghostHue = -1;
    }
    
    private int _ghostHue = -1;
    
    public int GhostHue
    {
        get => _ghostHue;
        set
        {
            _ghostHue = value;
            for (var index = 0; index < Vertices.Length; index++)
            {
                Vertices[index].Hue = HuesManager.Instance.GetHueVector(Tile.Id, (ushort)_ghostHue, Vertices[index].Hue.Z);
            }
        }
    }
    
    public float Alpha
    {
        get => Vertices[0].Hue.Z;
        set
        {
            for (var index = 0; index < Vertices.Length; index++)
            {
                Vertices[index].Hue.Z = value;
            }
        }
    }

    public int CompareTo(StaticObject? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }
        if (other is null)
        {
            return 1;
        }
        return StaticTile.PriorityZ.CompareTo(other.StaticTile.PriorityZ);
    }
}