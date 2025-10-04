using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

//BaseTool allows for out of the box continuous and area drawing
public abstract class BaseTool : Tool
{
    // Properties to track area operations
    protected bool IsAreaOperation { get; private set; }
    protected ushort AreaStartX { get; private set; }
    protected ushort AreaStartY { get; private set; }
    protected ushort AreaEndX { get; private set; }
    protected ushort AreaEndY { get; private set; }
    
    public void OnAreaOperationStart(ushort x, ushort y)
    {
        IsAreaOperation = true;
        AreaStartX = x;
        AreaStartY = y;
        AreaEndX = x;
        AreaEndY = y;
    }
    
    public void OnAreaOperationUpdate(ushort x, ushort y)
    {
        AreaEndX = x;
        AreaEndY = y;
    }
    
    public void OnAreaOperationEnd()
    {
        IsAreaOperation = false;
    }
    
    protected static readonly Random Random = Random.Shared;
    protected abstract void GhostApply(TileObject? o);
    protected abstract void GhostClear(TileObject? o);
    protected abstract void InternalApply(TileObject? o);

    protected static int _chance = 100;
    protected bool _pressed;
    protected bool _areaMode;
    protected bool _topTilesOnly = true;
    private TileObject? _areaStartTile;

    internal override void Draw()
    {
        ImGuiEx.DragInt("Chance", ref _chance, 1, 0, 100);
    }

    public override void OnDeactivated(TileObject? o)
    {
        _pressed = false;
        _areaMode = false;
        _topTilesOnly = false;
    }

    public sealed override void OnKeyPressed(Keys key)
    {
        if (!_pressed)
        {
            if (key == Keys.LeftControl)
            {
                _areaMode = true;
            }
            if (key == Keys.LeftShift)
            {
                _topTilesOnly = false;
            }
        }
    }
    
    public sealed override void OnKeyReleased(Keys key)
    {
        if (!_pressed)
        {
            if (key == Keys.LeftControl)
            {
                _areaMode = false;
            }
            if (key == Keys.LeftShift)
            {
                _topTilesOnly = true;
            }
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
                to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
            }
            _areaStartTile = to;
            
            OnAreaOperationStart(to.Tile.X, to.Tile.Y);
        }
        CEDClient.BeginUndoGroup();
        
        // For sequential tile sets, reset the sequence but DON'T skip GhostApply
        if (CEDGame.MapManager.UseSequentialTileSet)
        {
            CEDGame.MapManager.ResetSequence();

            // ALWAYS apply ghost to the initial tile
            if (o != null)
            {
                var tilesWindow = UIManager.GetWindow<TilesWindow>();
                TileObject to = o;
                if (CEDGame.MapManager.UseVirtualLayer && tilesWindow.LandMode && o is VirtualLayerTile)
                {
                    to = CEDGame.MapManager.LandTiles[to.Tile.X, to.Tile.Y];
                }
                GhostApply(to);
            }
        }
    }
    
    public sealed override void OnMouseReleased(TileObject? o)
    {
        var tilesWindow = UIManager.GetWindow<TilesWindow>();

        if (_pressed)
        {
            if (_areaMode)
            {
                foreach (var to in MapManager.GetTiles(_areaStartTile, o, _topTilesOnly))
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
        
        if (_areaMode)
        {
            OnAreaOperationEnd();
        }
        
        // Reset sequence when mouse is released (drawing ends)
        if (CEDGame.MapManager.UseSequentialTileSet)
        {
            CEDGame.MapManager.ResetSequence();
        }
        
        CEDClient.EndUndoGroup();
    }

    
    public sealed override void OnMouseEnter(TileObject? o)
    {
        if (o == null)
            return;

        if (_areaMode && _pressed && o != null)
        {
            OnAreaOperationUpdate(o.Tile.X, o.Tile.Y);
        }
        
        var tilesWindow = UIManager.GetWindow<TilesWindow>();

        if (_areaMode && _pressed)
        {
            foreach (var to in MapManager.GetTiles(_areaStartTile, o, _topTilesOnly))
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
            foreach (var to in MapManager.GetTiles(_areaStartTile, o, _topTilesOnly))
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