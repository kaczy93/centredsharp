using System.Collections.Concurrent;

namespace CentrED.Server; 

public class BlockCache {
    public delegate void OnRemovedCachedObject(Block block);

    private readonly ConcurrentDictionary<int, Block> _blocks;
    private readonly ConcurrentQueue<int> _queue;
    private readonly int _maxSize;
    private readonly OnRemovedCachedObject _onExpiredHandler;

    public BlockCache(OnRemovedCachedObject onRemovedCachedObject, int maxSize = 256) {
        _maxSize = maxSize;
        _queue = new ConcurrentQueue<int>();
        _blocks = new ConcurrentDictionary<int, Block>();
        _onExpiredHandler = onRemovedCachedObject;
    }

    public void Add(Block block) {
        var blockId = BlockId(block.LandBlock.X, block.LandBlock.Y);
        _blocks.TryAdd(blockId, block);
        _queue.Enqueue(blockId);
        if (_blocks.Count > _maxSize) {
            Dequeue();
        }
    }

    public void Clear() {
        while (_queue.Count > 0) {
            Dequeue();
        }
    }
    
    public Block? Get(ushort x, ushort y) {
        _blocks.TryGetValue(BlockId(x, y), out Block? value);
        return value;
    }

    private Block? Dequeue() {
        if (!_queue.TryDequeue(out var blockId)) return null;
        if (!_blocks.TryRemove(blockId, out Block? dequeued)) return null;
        _onExpiredHandler.Invoke(dequeued);
        return dequeued;
    }
    
    private int BlockId(ushort x, ushort y) {
        return HashCode.Combine(x, y);
    }
}