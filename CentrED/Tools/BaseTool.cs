using CentrED.Map;
using CentrED.UI;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

//BaseTool allows for out of the box continuous and area drawing
public abstract class BaseTool : Tool
{
    protected static readonly Random Random = new();
    protected abstract void GhostApply(TileObject? o);
    protected abstract void GhostClear(TileObject? o);
    protected abstract void Apply(TileObject? o);

    protected static int _chance = 100;
    protected bool _pressed;
    protected bool _areaMode;
    private TileObject? _areaStartTile;

    internal override void Draw()
    {
        UIManager.DragInt("Chance", ref _chance, 1, 0, 100);
    }

    public sealed override void OnKeyPressed(Keys key)
    {
        if (key == Keys.LeftControl && !_pressed)
        {
            _areaMode = true;
        }
    }
    
    public sealed override void OnKeyReleased(Keys key)
    {
        if (key == Keys.LeftControl && !_pressed)
        {
            _areaMode = false;
        }
    }
    
    public sealed override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
        if (_areaMode && _areaStartTile == null && o != null)
        {
            _areaStartTile = o;
        }
        CEDClient.BeginUndoGroup();
    }
    
    public sealed override void OnMouseReleased(TileObject? o)
    {
        if(_pressed)
        {
            if (_areaMode)
            {
                foreach (var to in CEDGame.MapManager.GetTopTiles(_areaStartTile, o))
                {
                    Apply(to);   
                    GhostClear(to);
                }
            }
            else
            {
                Apply(o);
                GhostClear(o);
            }
        }
        _pressed = false;
        _areaStartTile = null;
        CEDClient.EndUndoGroup();
    }

    
    public sealed override void OnMouseEnter(TileObject? o)
    {
        if (o == null)
            return;

        if (_areaMode && _pressed)
        {
            foreach (var to in CEDGame.MapManager.GetTopTiles(_areaStartTile, o))
            {
                if (Random.Next(100) < _chance)
                {
                    GhostApply(to);
                }
            }
        }
        else
        {
            if (Random.Next(100) < _chance)
            {
                GhostApply(o);
            }
        }
    }
    
    public sealed override void OnMouseLeave(TileObject? o)
    {
        if (_pressed && !_areaMode)
        {
            Apply(o);
        }
        if (_pressed && _areaMode)
        {
            foreach (var to in CEDGame.MapManager.GetTopTiles(_areaStartTile, o))
            {
                GhostClear(to);
            }
        }
        else
        {
            GhostClear(o);
        }
    }
}