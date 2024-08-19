using CentrED.Map;
using CentrED.UI.Windows;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => "Select";
    public override Keys Shortcut => Keys.F1;

    private bool _pressed;
    private bool _pickTile;
    private bool _pickHue;
    
    internal override void Draw()
    {
        ImGui.TextDisabled("(?)");
        UI.UIManager.Tooltip("Click to show tile in info window\nAlt+Click to pick tile\nShift+Click to pick hue");
    }
    
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
        if (key == Keys.LeftShift && !_pressed)
        {
            _pickHue = false;
        }
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (_pressed)
        {
            UIManager.GetWindow<InfoWindow>().Selected = o;
            if (_pickTile && o != null)
            {
                UIManager.GetWindow<TilesWindow>().UpdateSelectedId(o);
            }
            if (_pickHue && o is StaticObject so)
            {
                UIManager.GetWindow<HuesWindow>().UpdateSelectedHue(so);
            }
        }
    }

    public override void OnActivated(TileObject? o)
    {
        UIManager.GetWindow<InfoWindow>().Show = true;
    }
}