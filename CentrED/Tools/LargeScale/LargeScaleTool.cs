using CentrED.Network;

namespace CentrED.Tools;

public abstract class LargeScaleTool
{
    public abstract string Name { get; }
    public abstract void OnSelected();
    public abstract bool DrawUI();
    public abstract string SubmitStatus { get; }
    public virtual bool CanSubmit(AreaInfo area)
    {
        return true;
    }

    public abstract void Submit(AreaInfo area);

    public virtual bool IsRunning => false;
}