using CentrED.Client;
using CentrED.Client.Map;
using CentrED.Network;

namespace CentrED.Tools;

public abstract class RemoteLargeScaleTool : LargeScaleTool
{
    protected abstract ILargeScaleOperation SubmitLSO();

    public override LargeScaleToolRunner Submit(CentrEDClient client, AreaInfo area)
    {
        client.Send(new LargeScaleOperationPacket([area], SubmitLSO()).Compile());
        return new RemoteLargeScaleToolRunner();
    }
}

//We do everything in Submit, so this runner is only for the framework compatibility
public sealed class RemoteLargeScaleToolRunner() : LargeScaleToolRunner
{
    public override int Ticks => 0;
    public override double Progress => 0;
    public override bool Tick() => false;
}