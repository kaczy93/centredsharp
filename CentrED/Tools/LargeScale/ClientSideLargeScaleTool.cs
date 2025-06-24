using CentrED.Client;
using CentrED.Network;

namespace CentrED.Tools;

public abstract class LocalLargeScaleTool : LargeScaleTool
{
    public sealed override LargeScaleToolRunner Submit(CentrEDClient client, AreaInfo area)
    {
        return new LocalLargeScaleToolRunner(client, area, ProcessTile);
    }
    
    protected abstract void ProcessTile(CentrEDClient client, ushort x, ushort y);
}

public sealed class LocalLargeScaleToolRunner(
    CentrEDClient client,
    AreaInfo area,
    Action<CentrEDClient, ushort, ushort> action
) : LargeScaleToolRunner
{
    private int _ticks = 0;
    public override int Ticks => _ticks;
    public override double Progress => _ticks / (double)area.Width * area.Height;
    
    private readonly TileRangeEnumerator _enumerator = new(area);

    public override bool Tick()
    {
        if (client is not { Running: true })
            return false;
        if (!_enumerator.MoveNext())
            return false;
        
        var cur = _enumerator.Current;
        action(client, cur.x, cur.y);
        _ticks++;
        return false;
    }
}