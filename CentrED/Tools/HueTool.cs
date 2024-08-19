using CentrED.Map;
using CentrED.UI.Windows;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class HueTool : BaseTool
{
    public override string Name => "Hue";
    public override Keys Shortcut => Keys.F6;

    public override void OnActivated(TileObject? o)
    {
        UIManager.GetWindow<HuesWindow>().Show = true;
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.GhostHue = UIManager.GetWindow<HuesWindow>().ActiveId;
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject)
        {
            o.Reset();
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o is StaticObject so && so.GhostHue != -1)
            so.StaticTile.Hue = (ushort)so.GhostHue;
    }
}