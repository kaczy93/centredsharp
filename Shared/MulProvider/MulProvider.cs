namespace Shared.MulProvider; 

public abstract class MulProvider<T> where T : MulBlock {

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

    protected abstract int CalculateOffset(int id);

    protected abstract MulBlock GetData(int id, int offset);

    protected virtual void SetData(int id, int offset, MulBlock block) {
        if (ReadOnly) return;
        Data.Position = offset;
        block.Write(new BinaryWriter(Data));
    }

    public virtual MulBlock GetBlock(int id) {
        return GetData(id, CalculateOffset(id));
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