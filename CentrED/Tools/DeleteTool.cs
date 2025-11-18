using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class DeleteTool : BaseTool
{
    public override string Name => LangManager.Get(LangEntry.DELETE_TOOL);
    public override Keys Shortcut => Keys.F5;
    
    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.Highlighted = true;
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
        if(o is StaticObject { Highlighted: true } so)
            Client.Remove(so.StaticTile);
    }
}