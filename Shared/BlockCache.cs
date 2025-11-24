namespace CentrED;

public class BlockCache
{
    public delegate void CacheChanged(Block block);
    public CacheChanged? OnRemovedItem;
    
    private readonly Dictionary<int, Block> _blocks = new();
    private readonly Queue<int> _queue = new();
    private int _maxSize = 256;

    public void Add(Block block)
    {
        var id = Block.Id(block);
        if (!_blocks.ContainsKey(id))
        {
            _queue.Enqueue(id);
        }
        _blocks[id] = block;
        if (_blocks.Count > _maxSize)
        {
            Dequeue(out _);
        }
    }

    public void Clear()
    {
        //Don't just clear, we need to Dequeue() to trigger OnItemRemoved!
        while (Dequeue(out _))
        {
            
        }
    }

    public void Reset()
    {
        _queue.Clear();
        _blocks.Clear();
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

    public void Grow(int newSize)
    {
        if (newSize > _maxSize)
            Resize(newSize);
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