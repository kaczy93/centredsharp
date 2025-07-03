using CentrED.Network;

namespace CentrED.Tools;

public abstract class LargeScaleTool
{
    public abstract string Name { get; }
    public abstract void OnSelected();
    public abstract bool DrawUI();
    public abstract string SubmitStatus { get; }
    public virtual bool CanSubmit(RectU16 area)
    {
        return true;
    }

    public abstract void Submit(RectU16 area);

    public virtual bool IsRunning => false;
}