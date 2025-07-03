using CentrED.Client;
using CentrED.Client.Map;
using CentrED.Network;
using static CentrED.Application;

namespace CentrED.Tools;

public abstract class RemoteLargeScaleTool : LargeScaleTool
{
    protected abstract ILargeScaleOperation SubmitLSO();
    
    protected string _submitStatus = "";
    public override string SubmitStatus => _submitStatus;
    
    public override void OnSelected()
    {
        _submitStatus = "";
    }

    public override void Submit(RectU16 area)
    {
        CEDClient.Send(new LargeScaleOperationPacket([area], SubmitLSO()).Compile());
        _submitStatus = "Done";
    }
}
