namespace CentrED.Map;

public abstract class TileObject : MapObject
{
    public BaseTile Tile;
    
    public ushort Hue
    {
        set
        {
            for (var index = 0; index < Vertices.Length; index++)
            {
                Vertices[index].HueVec = HuesManager.Instance.GetHueVector(Tile.Id, value, Vertices[index].HueVec.Z);
            }
        }
    }
}