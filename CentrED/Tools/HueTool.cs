using CentrED.Map;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class HueTool : BaseTool
{
    public override string Name => "Hue";
    public override Keys Shortcut => Keys.F6;

    private bool _pressed;

    public override void OnActivated(TileObject? o)
    {
        CEDGame.UIManager.HuesWindow.Show = true;
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.GhostHue = CEDGame.UIManager.HuesWindow.ActiveId;
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.GhostHue = -1;
        }
    }

    protected override void Apply(TileObject? o)
    {
        if (o is StaticObject so && CEDGame.UIManager.HuesWindow.SelectedId != -1)
            so.StaticTile.Hue = (ushort)so.GhostHue;
    }
}