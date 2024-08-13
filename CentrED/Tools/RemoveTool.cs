using CentrED.Map;
using Microsoft.Xna.Framework.Input;

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
        if (o is StaticObject)
        {
            o.Reset();
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if(o is StaticObject so && Math.Abs(so.Alpha - 0.2f) < 0.001f)
            Client.Remove(so.StaticTile);
    }
}