namespace CentrED.Map;

public abstract class TileObject : MapObject
{
    public BaseTile Tile;
    public bool? Walkable;

    public virtual void Reset()
    {
        Visible = true;
    }
}