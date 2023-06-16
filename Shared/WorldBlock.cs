namespace CentrED;

public abstract class WorldBlock : MulEntry {
    private bool _changed;
    protected WorldBlock() {
        RefCount = 0;
        Changed = false;
    }

    public ushort X { get; set; }
    public ushort Y { get; set; }
    public int RefCount { get; private set; }

    public bool Changed {
        get => _changed;
        set {
            _changed = value;
        }
    }

    public void AddRef() {
        RefCount++;
    }

    public void RemoveRef() {
        if (RefCount > 0) RefCount--;
    }
}