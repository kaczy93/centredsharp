namespace CentrED.Map;

public abstract class TileObject : MapObject
{
    public const float TILE_SIZE = 31.11f;
    public const float TILE_Z_SCALE = 4.0f;
    public BaseTile Tile;
}