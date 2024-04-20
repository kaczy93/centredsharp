using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => "Select";
    public override Keys Shortcut => Keys.F1;

    private bool _pressed;
    private bool _pickTile;
    private bool _pickHue;
    
    public override void OnMousePressed(TileObject? o)
    {
        _pressed = true;
        OnMouseEnter(o);
    }

    public override void OnMouseReleased(TileObject? o)
    {
        _pressed = false;
    }

    public sealed override void OnKeyPressed(Keys key)
    {
        if (key == Keys.LeftAlt && !_pressed)
        {
            _pickTile = true;
        }
        if (key == Keys.LeftShift && !_pressed)
        {
            _pickHue = true;
        }
    }
    
    public sealed override void OnKeyReleased(Keys key)
    {
        if (key == Keys.LeftAlt && !_pressed)
        {
            _pickTile = false;
        }
        if (key == Keys.LeftAlt && !_pressed)
        {
            _pickHue = false;
        }
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (_pressed)
        {
            UIManager.InfoWindow.Selected = o;
            if (_pickTile)
            {
                if (o is StaticObject)
                {
                    UIManager.TilesWindow.SelectedStaticId = o.Tile.Id;
                    UIManager.TilesWindow.StaticMode = true;
                }
                else if (o is LandObject)
                {
                    UIManager.TilesWindow.SelectedLandId = o.Tile.Id;
                    UIManager.TilesWindow.StaticMode = false;
                }
                UIManager.TilesWindow.UpdateScroll = true;
            }
            if (_pickHue && o is StaticObject so)
            {
                UIManager.HuesWindow.SelectedId = so.StaticTile.Hue;
                UIManager.HuesWindow.UpdateScroll = true;
            }
        }
    }

    public override void OnActivated(TileObject? o)
    {
        UIManager.InfoWindow.Show = true;
    }
}