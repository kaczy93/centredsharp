namespace CentrED.Map;

public abstract class TileObject : MapObject
{
    public static readonly float TILE_SIZE = (float)(44 / Math.Sqrt(2));
    public const float TILE_Z_SCALE = 4.0f;
    public BaseTile Tile;
    public bool? Walkable;

    public virtual void Reset()
    {
        Visible = true;
    }
}