using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public abstract class Tool
{
    public abstract string Name { get; }
    public virtual Keys Shortcut => Keys.None;

    internal virtual void Draw()
    {
    }

    public virtual void OnActivated(TileObject? o)
    {
    }

    public virtual void OnDeactivated(TileObject? o)
    {
    }
    
    public virtual void OnMousePressed(TileObject? o)
    {
    }

    public virtual void OnMouseReleased(TileObject? o)
    {
    }

    public virtual void OnKeyPressed(Keys key)
    {
    }

    public virtual void OnKeyReleased(Keys key)
    {
    }
    
    public virtual void OnMouseEnter(TileObject? o)
    {
    }

    public virtual void OnMouseLeave(TileObject? o)
    {
    }
}