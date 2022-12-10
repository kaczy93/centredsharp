namespace Shared;

public abstract class MulBlock {
    public delegate void MulBlockChanged(MulBlock mulBlock);

    public MulBlockChanged OnChanged;
    public MulBlockChanged OnFinished;

    public int Id { get; set; }

    public abstract int GetSize { get; }

    //MulBlockEventHandler
    public event MulBlockChanged? OnDestroy;

    ~MulBlock() {
        OnDestroy?.Invoke(this);
    }

    public static void Change(MulBlock mulBlock) {
        mulBlock.OnChanged?.Invoke(mulBlock);
    }

    public static void Finish(MulBlock mulBlock) {
        mulBlock.OnFinished?.Invoke(mulBlock);
        //ref mulBlock and nulling after invoke?
    }

    public abstract MulBlock Clone();

    public abstract void Write(BinaryWriter writer);
}