//UOLib/UWorldItem.pas

namespace Shared;

//TWorldBlock
public abstract class WorldBlock : MulBlock {
    public WorldBlock() {
        RefCount = 0;
        Changed = false;
    }

    public ulong X { get; set; }
    public ulong Y { get; set; }
    public int RefCount { get; private set; }
    public bool Changed { get; set; }

    public void AddRef() {
        RefCount++;
    }

    public void RemoveRef() {
        if (RefCount > 0) RefCount--;
    }
}