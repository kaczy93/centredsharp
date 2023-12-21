namespace CentrED;

public class BlockCache
{
    public delegate void CacheChanged(Block block);
    public CacheChanged? OnRemovedItem;
    
    private readonly Dictionary<int, Block> _blocks = new();
    private readonly Queue<int> _queue = new();
    private int _maxSize = 256;

    public bool Add(int id, Block block)
    {
        if (_blocks.ContainsKey(id))
            return false;
        _blocks.TryAdd(id, block);
        _queue.Enqueue(id);
        if (_blocks.Count > _maxSize)
        {
            Dequeue(out _);
        }
        return true;
    }

    public void Clear()
    {
        _blocks.Clear();
        _queue.Clear();
    }

    public bool Contains(int id)
    {
        return _blocks.ContainsKey(id);
    }

    public Block? Get(int id)
    {
        _blocks.TryGetValue(id, out Block? block);
        return block;
    }

    private bool Dequeue(out Block? block)
    {
        block = default;
        if (!_queue.TryDequeue(out var id))
            return false;
        if (!_blocks.Remove(id, out block))
            return false;
        OnRemovedItem?.Invoke(block);
        return true;
    }

    public void Resize(int newSize)
    {
        if (newSize < 0)
            newSize = 0;
        _maxSize = newSize;
        while (_blocks.Count > _maxSize)
        {
            Dequeue(out _);
        }
    }
}