namespace CentrED.Map;

public abstract class TileObject : MapObject
{
    public const float TILE_SIZE = 31.11f;
    public const float TILE_Z_SCALE = 4.0f;
    public const float INVERSE_SQRT2 = 0.70711f;
    public BaseTile Tile;
    public bool? Walkable;

    public virtual void Reset()
    {
        Visible = true;
    }
}