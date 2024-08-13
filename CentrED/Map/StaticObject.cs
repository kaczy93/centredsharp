using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class StaticObject : TileObject
{
    public StaticTile StaticTile;
    public bool IsAnimated;
    public bool IsLight;
    public Rectangle RealBounds;

    public StaticObject(StaticTile tile)
    {
        Tile = StaticTile = tile;
        
        RealBounds = Application.CEDGame.MapManager.Arts.GetRealArtBounds(Tile.Id);
        UpdateId(Tile.Id);
        UpdatePos(tile.X, tile.Y, tile.Z);
        UpdateHue(tile.Hue);
        for (int i = 0; i < 4; i++)
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
        var spriteInfo = Application.CEDGame.MapManager.Arts.GetArt((uint)(newId + index.AnimOffset));
        if (spriteInfo.Equals(SpriteInfo.Empty))
        {
            Console.WriteLine($"No texture found for static {Tile.X},{Tile.Y},{Tile.Z}:0x{newId:X}");
            //VOID texture of land is by default all pink, so it should be noticeable that something is not right
            spriteInfo = Application.CEDGame.MapManager.Texmaps.GetTexmap(0x0001);
        }
        
        Texture = spriteInfo.Texture;
        TextureBounds = spriteInfo.UV;
        
        float onePixel = Math.Max(1.0f / Texture.Width, Epsilon.value);
        var texX = TextureBounds.X / (float)Texture.Width + onePixel / 2f;
        var texY = TextureBounds.Y / (float)Texture.Height + onePixel / 2f;
        var texWidth = TextureBounds.Width / (float)Texture.Width - onePixel;
        var texHeight = TextureBounds.Height / (float)Texture.Height - onePixel;

        Vertices[0].Texture = new Vector3(texX, texY, 0f);
        Vertices[1].Texture = new Vector3(texX + texWidth, texY, 0f);
        Vertices[2].Texture = new Vector3(texX, texY + texHeight, 0f);
        Vertices[3].Texture = new Vector3(texX + texWidth, texY + texHeight, 0f);
        UpdateDepthOffset();
        IsAnimated = TileDataLoader.Instance.StaticData[newId].IsAnimated;
    }

    public void UpdateDepthOffset()
    {
        var depthOffset = StaticTile.CellIndex * 0.0001f;
        for (int i = 0; i < 4; i++)
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
        
        var projectedWidth = (TextureBounds.Width / 2f) * INVERSE_SQRT2;
        
        Vertices[0].Position = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ + TextureBounds.Height);
        Vertices[1].Position = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ + TextureBounds.Height);
        Vertices[2].Position = new Vector3(posX - projectedWidth, posY + projectedWidth, posZ);
        Vertices[3].Position = new Vector3(posX + projectedWidth, posY - projectedWidth, posZ);
    }

    public void UpdateHue(ushort newHue)
    {
        var hueVec = HuesManager.Instance.GetHueVector(Tile.Id, newHue);
        for (int i = 0; i < 4; i++)
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
}