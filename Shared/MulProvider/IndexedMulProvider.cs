namespace Shared.MulProvider; 

public abstract class IndexedMulProvider : MulProvider {
    public IndexedMulProvider(Stream data, Stream index, bool readOnly = false) : base(data, readOnly) {
        Index = index;
        EntryCount = (uint)index.Length / 12;
    }
    
    public IndexedMulProvider(string dataPath, string indexPath, bool readOnly = false) : base(dataPath, readOnly) {
        var fileAccess = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
        Index = File.Open(indexPath, FileMode.Open, fileAccess, FileShare.None);
        EntryCount = (uint)Index.Length / 12;
    }
    
    public Stream Index { get; }
    
    public uint EntryCount { get; }

    protected virtual int CalculateIndexOffset(int id) {
        return id * 12;
    }

    protected abstract MulBlock GetData(int id, GenericIndex index);

    protected virtual void SetData(int id, GenericIndex index, MulBlock block) {
        if (ReadOnly) return;

        var size = block.GetSize;
        if (size == 0) {
            index.Lookup = -1;
            index.Various = -1;
        } else if (size > index.Size || index.Lookup < 0) {
            Data.Position = Data.Length;
            index.Lookup = (int)Data.Position;
            block.Write(new BinaryWriter(Data));
        }
        else {
            Data.Position = index.Lookup;
            block.Write(new BinaryWriter(Data));
        }
        index.Size = size;
    }

    protected virtual int GetVarious(int id, MulBlock block, int defaultValue) {
        return defaultValue; //??
    }

    public override MulBlock GetBlock(int id) {
        GetBlockEx(id, out MulBlock result, out GenericIndex genericIndex);
        return result;
    }

    public virtual void GetBlockEx(int id, out MulBlock block, out GenericIndex index) { // why index is out?
        Index.Position = CalculateIndexOffset(id);
        index = new GenericIndex(new BinaryReader(Index));
        block = GetData(id, index);
        block.OnChanged = OnChanged;
        block.OnFinished = OnFinished;
    }

    public override void SetBlock(int id, MulBlock block) {
        if (ReadOnly) return;

        Index.Position = CalculateIndexOffset(id);
        var genericIndex = new GenericIndex(new BinaryReader(Index));
        SetData(id, genericIndex, block);
        Index.Position = CalculateIndexOffset(id);
        genericIndex.Various = GetVarious(id, block, genericIndex.Various);
        genericIndex.Write(new BinaryWriter(Index));
    }

    public virtual bool Exists(int id) {
        Index.Position = CalculateIndexOffset(id);
        var genericIndex = new GenericIndex(new BinaryReader(Index));
        return genericIndex.Lookup > -1 && genericIndex.Size > 0;
    }

    public virtual void Defragment(Stream tempStream, OnProgressEvent? onProgress) {
        if(ReadOnly) return;
        tempStream.SetLength(Data.Length);
        tempStream.Position = 0;
        Index.Position = 0;
        while (Index.Position < Index.Length) {
            var genericIndex = new GenericIndex(new BinaryReader(tempStream));
            if (genericIndex.Lookup > -1) {
                Data.Position = genericIndex.Lookup;
                genericIndex.Lookup = (int)tempStream.Position;
                Data.CopyBytesTo(tempStream, genericIndex.Size);
                Index.Seek(-12, SeekOrigin.Current);
                genericIndex.Write(new BinaryWriter(Index));
            }

            if (Index.Position % 1200 == 0) {
                onProgress?.Invoke(Index.Length, Index.Position);
            }
        }
        Data.SetLength(tempStream.Position);
        Data.Position = 0;
        tempStream.Position = 0;
        tempStream.CopyBytesTo(Data, (int)Data.Length);
    }
}