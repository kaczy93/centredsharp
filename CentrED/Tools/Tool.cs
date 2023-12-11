using CentrED.Map;
using Microsoft.Xna.Framework;

namespace CentrED.Tools;

public abstract class Tool
{
    public abstract string Name { get; }

    internal virtual void Draw()
    {
    }

    public virtual void OnActivated(TileObject? o)
    {
    }

    public virtual void OnDeactivated(TileObject? o)
    {
    }

    public virtual void OnMouseEnter(TileObject? o)
    {
    }

    public virtual void OnMouseLeave(TileObject? o)
    {
    }

    public virtual void OnMousePressed(TileObject? o)
    {
    }

    public virtual void OnMouseReleased(TileObject? o)
    {
    }
    
    public virtual void OnVirtualLayerTile(Vector3 tilePos)
    {
        
    }
}