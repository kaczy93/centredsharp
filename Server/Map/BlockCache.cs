namespace Server; 

public class BlockCache {
    public delegate void OnRemovedCachedObject(Block block);

    private readonly Dictionary<int, Block> _blocks;
    private readonly Queue<int> _queue;
    private readonly int _maxSize;
    private readonly OnRemovedCachedObject _onExpiredHandler;

    public BlockCache(OnRemovedCachedObject onRemovedCachedObject, int maxSize = 256) {
        _maxSize = maxSize;
        _queue = new Queue<int>(_maxSize + 1);
        _blocks = new Dictionary<int, Block>(_maxSize + 1);
        _onExpiredHandler = onRemovedCachedObject;
    }

    public void Add(Block block) {
        var blockId = BlockId(block.LandBlock.X, block.LandBlock.Y);
        _blocks.Add(blockId, block);
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
        if (_blocks.Remove(_queue.Dequeue(), out Block? dequeued)) {
            _onExpiredHandler.Invoke(dequeued);
            return dequeued;
        }
        return null;
    }
    
    private int BlockId(ushort x, ushort y) {
        return HashCode.Combine(x, y);
    }
}