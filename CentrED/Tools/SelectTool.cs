using CentrED.Map;
using CentrED.UI;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class SelectTool : Tool
{
    public override string Name => LangManager.Get(SELECT_TOOL);
    public override Keys Shortcut => Keys.F1;

    private bool _pressed;
    private bool _pickTile;
    private bool _pickHue;
    
    internal override void Draw()
    {
        ImGui.TextDisabled("(?)"u8);
        ImGuiEx.Tooltip(LangManager.Get(SELECT_TOOL_TOOLTIP));
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
                UIManager.GetWindow<TilesWindow>().UpdateSelection(o);
            }
            if (_pickHue && o is StaticObject so)
            {
                UIManager.GetWindow<HuesWindow>().UpdateSelection(so);
            }
        }
    }

    public override void OnActivated(TileObject? o)
    {
        UIManager.GetWindow<InfoWindow>().Show = true;
    }
}