namespace CentrED;

public abstract class BaseTile
{
    internal ushort _id;
    internal ushort _x;
    internal ushort _y;
    internal short _z;

    public virtual ushort Id
    {
        get => _id;
        set => throw new InvalidOperationException();
    }

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