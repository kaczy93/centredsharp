using CentrED.Map;
using CentrED.Network;
using CentrED.UI;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

//BaseTool allows for out of the box continuous and area drawing
public abstract class BaseTool : Tool
{
    // Properties to track area operations
    protected bool AreaMode;
    protected TileObject? AreaStartTile;
    protected RectU16 Area;
    
    protected virtual void OnAreaOperationStart(TileObject? o)
    {
        if (o == null)
            return;
        
        AreaStartTile = o;
        Area = new RectU16(o.Tile.X, o.Tile.Y, o.Tile.X, o.Tile.Y);
    }
    
    protected virtual void OnAreaOperationUpdate(TileObject? to)
    {
        if (to == null)
            return;
        
        Area.X2 = to.Tile.X;
        Area.Y2 = to.Tile.Y;
    }

    protected virtual void OnAreaOperationEnd()
    {
        AreaStartTile = null;
    }
    
    protected abstract void GhostApply(TileObject? o);
    protected abstract void GhostClear(TileObject? o);
    protected abstract void InternalApply(TileObject? o);

    protected static int _chance = 100;
    protected bool Pressed;
    protected bool TopTilesOnly = true;

    internal override void Draw()
    {
        ImGuiEx.DragInt(LangManager.Get(LangEntry.CHANCE), ref _chance, 1, 0, 100);
    }

    public override void OnDeactivated(TileObject? o)
    {
        Pressed = false;
        AreaMode = false;
        TopTilesOnly = false;
    }

    public sealed override void OnKeyPressed(Keys key)
    {
        if (!Pressed)
        {
            if (key == Keys.LeftControl)
            {
                AreaMode = true;
            }
            if (key == Keys.LeftShift)
            {
                TopTilesOnly = false;
            }
        }
    }
    
    public sealed override void OnKeyReleased(Keys key)
    {
        if (!Pressed)
        {
            if (key == Keys.LeftControl)
            {
                AreaMode = false;
            }
            if (key == Keys.LeftShift)
            {
                TopTilesOnly = true;
            }
        }
    }
    
    public sealed override void OnMousePressed(TileObject? o)
    {
        Pressed = true;
        if (AreaMode)
        {
            OnAreaOperationStart(o);
        }
        CEDClient.BeginUndoGroup();
    }
    
    public sealed override void OnMouseReleased(TileObject? o)
    {
        if (Pressed)
        {
            if (AreaMode)
            {
                foreach (var to in MapManager.GetTiles(AreaStartTile, o, TopTilesOnly))
                {
                    InternalApply(to);   
                    GhostClear(to);
                }
                OnAreaOperationEnd();
            }
            else
            {
                InternalApply(o);
                GhostClear(o);
            }
        }
        Pressed = false;
        
        CEDClient.EndUndoGroup();
    }

    
    public sealed override void OnMouseEnter(TileObject? o)
    {
        if (AreaMode && Pressed)
        {
            OnAreaOperationUpdate(o);
            foreach (var to in MapManager.GetTiles(AreaStartTile, o, TopTilesOnly))
            {
                if (Random.Shared.Next(100) < _chance)
                {
                    GhostApply(to);
                }
            }
        }
        else
        {
            if (Random.Shared.Next(100) < _chance)
            {
                GhostApply(o);
            }
        }
    }
    
    public sealed override void OnMouseLeave(TileObject? o)
    {
        if (Pressed)
        {
            if (AreaMode)
            {
                foreach (var to in MapManager.GetTiles(AreaStartTile, o, TopTilesOnly))
                {
                    GhostClear(to);
                }
            }
            else
            {
                InternalApply(o);
            }
        }
        GhostClear(o);
    }

    public override void Apply(TileObject? o)
    {
        GhostApply(o);
        InternalApply(o);
    }
}