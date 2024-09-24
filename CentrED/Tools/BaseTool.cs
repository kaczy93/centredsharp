using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

//BaseTool allows for out of the box continuous and area drawing
public abstract class BaseTool : Tool
{
    protected static readonly Random Random = new();
    protected abstract void GhostApply(TileObject? o);
    protected abstract void GhostClear(TileObject? o);
    protected abstract void InternalApply(TileObject? o);

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
            TileObject to = o;
            var tilesWindow = UIManager.GetWindow<TilesWindow>();
            if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && o is VirtualLayerTile)
            {
                to = CEDGame.MapManager.LandTiles[o.Tile.X, o.Tile.Y];
            }
            _areaStartTile = to;
        }
        CEDClient.BeginUndoGroup();
    }
    
    public sealed override void OnMouseReleased(TileObject? o)
    {
        var tilesWindow = UIManager.GetWindow<TilesWindow>();

        if (_pressed)
        {
            if (_areaMode)
            {
                foreach (var to in MapManager.GetTopTiles(_areaStartTile, o))
                {
                    TileObject to2 = to;                    
                    if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to2 is VirtualLayerTile)
                    {
                        to2 = CEDGame.MapManager.LandTiles[to2.Tile.X, to2.Tile.Y];
                    }
                    InternalApply(to2);   
                    GhostClear(to2);
                }
            }
            else
            {
                TileObject to = o;
                if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to is VirtualLayerTile)
                {
                    to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
                }
                InternalApply(to);
                GhostClear(to);
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

        var tilesWindow = UIManager.GetWindow<TilesWindow>();

        if (_areaMode && _pressed)
        {
            foreach (var to in MapManager.GetTopTiles(_areaStartTile, o))
            {
                if (Random.Next(100) < _chance)
                {
                    TileObject to2 = to;
                    if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to2 is VirtualLayerTile)
                    {
                        to2 = CEDGame.MapManager.LandTiles[to2.Tile.X, to2.Tile.Y];
                    }
                    GhostApply(to2);
                }
            }
        }
        else
        {
            
            if (Random.Next(100) < _chance)
            {
                TileObject to = o;                
                if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to is VirtualLayerTile)
                {
                    to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
                }
                GhostApply(to);
            }
        }
    }
    
    public sealed override void OnMouseLeave(TileObject? o)
    {
        var tilesWindow = UIManager.GetWindow<TilesWindow>();

        if (_pressed && !_areaMode)
        {
            TileObject to = o;
            if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to is VirtualLayerTile)
            {
                to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
            }
            InternalApply(to);
        }
        if (_pressed && _areaMode)
        {
            foreach (var to in MapManager.GetTopTiles(_areaStartTile, o))
            {
                TileObject to2 = to;
                if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to2 is VirtualLayerTile)
                {
                    to2 = CEDGame.MapManager.LandTiles[to2.Tile.X, to2.Tile.Y];
                }
                GhostClear(to2);
            }
        }
        else
        {
            TileObject to = o;
            if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && to is VirtualLayerTile)
            {
                to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
            }
            GhostClear(to);
        }
    }

    public override void Apply(TileObject? o)
    {
        GhostApply(o);
        InternalApply(o);
    }
}