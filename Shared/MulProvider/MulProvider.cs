namespace Shared.MulProvider; 

public abstract class MulProvider<T> where T : MulBlock {
    public delegate void OnProgressEvent(long total, long current);
    

    public MulProvider(Stream data, bool readOnly = false) {
        Data = data;
        OwnsData = false;
        ReadOnly = readOnly;
    }
    
    public MulProvider(string dataPath, bool readOnly = false) {
        var fileAccess = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
        Data = File.Open(dataPath, FileMode.Open, fileAccess, FileShare.Read);
        OwnsData = true;
        ReadOnly = readOnly;
    }
    
    public Stream Data { get; }
    
    protected bool OwnsData { get; }
    
    protected bool ReadOnly { get; }

    public event MulBlock.MulBlockChanged? ChangeEvents;

    public event MulBlock.MulBlockChanged? FinishedEvents;

    protected abstract int CalculateOffset(int id);

    protected abstract MulBlock GetData(int id, int offset);

    protected virtual void SetData(int id, int offset, MulBlock block) {
        if (ReadOnly) return;
        Data.Position = offset;
        block.Write(new BinaryWriter(Data));
    }

    protected void OnChanged(MulBlock block) {
        SetBlock(block.Id, (T)block);
        ChangeEvents?.Invoke(block);
    }

    protected void OnFinished(MulBlock block) {
        FinishedEvents?.Invoke(block);
    }

    public virtual MulBlock GetBlock(int id) {
        MulBlock result = GetData(id, CalculateOffset(id));
        result.OnChanged = OnChanged;
        result.OnFinished = OnFinished;
        return result;
    }

    public virtual void SetBlock(int id, MulBlock block) {
        if (ReadOnly) return;
        SetData(id, CalculateOffset(id), block);
    }

    public MulBlock this[int index] {
        get => GetBlock(index);
        set => SetBlock(index, value);
    }
}