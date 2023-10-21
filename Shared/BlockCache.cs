using System.Collections.Concurrent;

namespace CentrED; 

public class BlockCache {
    public delegate void CacheChanged(Block block);

    private readonly ConcurrentDictionary<int, Block> blocks;
    private readonly ConcurrentQueue<int> _queue;
    private int _maxSize;
    public CacheChanged? OnRemovedItem;
    public CacheChanged? OnAddedItem;

    public BlockCache(int maxSize = 256) {
        _maxSize = maxSize;
        _queue = new ConcurrentQueue<int>();
        blocks = new ConcurrentDictionary<int, Block>();
    }

    public void Add(int id, Block block) {
        blocks.TryAdd(id, block);
        _queue.Enqueue(id);
        if (blocks.Count > _maxSize) {
            Dequeue(out _);
        }
    }

    public void Clear() {
        while (!_queue.IsEmpty) {
            Dequeue(out _);
        }
    }

    public bool Contains(int id) {
        return Get(id) != null;
    }

    public Block? Get(int id) {
        blocks.TryGetValue(id, out Block? block);
        return block;
    }

    private bool Dequeue(out Block? block) {
        block = default;
        if (!_queue.TryDequeue(out var id)) return false;
        if (!blocks.TryRemove(id, out block)) return false;
        OnRemovedItem?.Invoke(block);
        return true;
    }

    public void Resize(int newSize) {
        _maxSize = newSize;
        while (blocks.Count > _maxSize) {
            Dequeue(out _);
        }
    }
}