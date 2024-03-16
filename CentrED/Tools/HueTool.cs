using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class HueTool : BaseTool
{
    public override string Name => "Hue";
    public override Keys Shortcut => Keys.F6;

    public override void OnActivated(TileObject? o)
    {
        UIManager.HuesWindow.Show = true;
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.GhostHue = UIManager.HuesWindow.ActiveId;
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject)
        {
            o.Reset();
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is StaticObject so && so.GhostHue != -1)
            so.StaticTile.Hue = (ushort)so.GhostHue;
    }
}