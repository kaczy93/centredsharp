using CentrED.Map;

namespace CentrED.Tools;

public abstract class Tool
{
    public abstract string Name { get; }

    internal virtual void DrawWindow()
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
}