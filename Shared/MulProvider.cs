using System.Text;

namespace CentrED; 

public abstract class MulProvider<T> where T : MulEntry {
    protected MulProvider(FileStream stream, bool readOnly = false) {
        Stream = stream;
        Reader = new BinaryReader(stream, Encoding.UTF8);
        Writer = new BinaryWriter(stream, Encoding.UTF8);
        ReadOnly = readOnly;
    }
    
    protected FileStream Stream { get; }
    protected BinaryReader Reader { get; }
    protected BinaryWriter Writer { get; }
    
    protected bool ReadOnly { get; }

    protected abstract int CalculateOffset(int id);

    protected abstract T GetData(int id, int offset);

    protected virtual void SetData(int id, int offset, T block) {
        if (ReadOnly) return;
        Stream.Position = offset;
        block.Write(Writer);
    }

    public virtual T GetBlock(int id) {
        return GetData(id, CalculateOffset(id));
    }

    public virtual void SetBlock(int id, T block) {
        if (ReadOnly) return;
        SetData(id, CalculateOffset(id), block);
    }

    public T this[int index] {
        get => GetBlock(index);
        set => SetBlock(index, value);
    }
}