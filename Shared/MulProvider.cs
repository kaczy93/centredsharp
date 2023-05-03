using System.Text;

namespace CentrED; 

public abstract class MulProvider<T> where T : MulEntry {
    protected MulProvider(String filePath, bool writeable) {
        Stream = File.Open(filePath, FileMode.Open, writeable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);
        Reader = new BinaryReader(Stream, Encoding.UTF8);
        if(writeable)
            Writer = new BinaryWriter(Stream, Encoding.UTF8);
    }
    
    protected FileStream Stream { get; }
    protected BinaryReader Reader { get; }
    protected BinaryWriter? Writer { get; }

    protected abstract int CalculateOffset(int id);

    protected abstract T GetData(int id, int offset);

    protected virtual void SetData(int id, int offset, T block) {
        if (Writer == null) return;
        Stream.Position = offset;
        block.Write(Writer);
    }

    public virtual T GetBlock(int id) {
        return GetData(id, CalculateOffset(id));
    }

    public virtual void SetBlock(int id, T block) {
        if (Writer == null) return;
        SetData(id, CalculateOffset(id), block);
    }

    public T this[int index] {
        get => GetBlock(index);
        set => SetBlock(index, value);
    }
}