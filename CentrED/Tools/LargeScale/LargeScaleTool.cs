using CentrED.Client;
using CentrED.Network;

namespace CentrED.Tools;

public abstract class LargeScaleTool
{
    public abstract string Name { get; }
    public abstract void DrawUI();
    
    public virtual bool CanSubmit(CentrEDClient client, AreaInfo area, out string message)
    {
        message = "";
        return true;
    }

    public abstract LargeScaleToolRunner Submit(CentrEDClient client, AreaInfo area);
}

public abstract class LargeScaleToolRunner
{
    public abstract int Ticks { get; }
    public abstract double Progress { get; }
    public abstract bool Tick();
}