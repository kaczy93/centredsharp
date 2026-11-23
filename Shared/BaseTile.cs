namespace CentrED;

public abstract class BaseTile
{
    internal ushort _x;
    internal ushort _y;
    internal short _z;

    // Land ids are ushort, static ids are uint: LandTile and StaticTile expose a settable Id of their own width
    public uint Id => GetId();

    protected abstract uint GetId();

    public virtual ushort X
    {
        get => _x;
        set => throw new InvalidOperationException();
    }

    public virtual ushort Y
    {
        get => _y;
        set => throw new InvalidOperationException();
    }

    public virtual short Z
    {
        get => _z;
        set => throw new InvalidOperationException();
    }

    public abstract string ShortString();
}