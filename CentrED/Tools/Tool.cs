using CentrED.Map;

namespace CentrED.Tools;

public abstract class Tool
{
    public abstract string Name { get; }

    internal virtual void DrawWindow()
    {
    }

    public virtual void OnActivated(MapObject? o)
    {
    }

    public virtual void OnDeactivated(MapObject? o)
    {
    }

    public virtual void OnMouseEnter(MapObject? o)
    {
    }

    public virtual void OnMouseLeave(MapObject? o)
    {
    }

    public virtual void OnMousePressed(MapObject? o)
    {
    }

    public virtual void OnMouseReleased(MapObject? o)
    {
    }
}