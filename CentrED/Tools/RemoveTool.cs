using CentrED.Map;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;

namespace CentrED.Tools;

public class RemoveTool : BaseTool
{
    public override string Name => "Remove";
    public override Keys Shortcut => Keys.F5;
    
    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 0.2f;
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Alpha = 1.0f;
        }
    }

    protected override void Apply(TileObject? o)
    {
        if(o is StaticObject so)
            CEDClient.Remove(so.StaticTile);
    }
}