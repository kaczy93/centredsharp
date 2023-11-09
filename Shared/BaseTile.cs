namespace CentrED;

public abstract class BaseTile
{
    internal ushort _id;
    internal ushort _x;
    internal ushort _y;
    internal sbyte _z;

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

    public virtual sbyte Z
    {
        get => _z;
        set => throw new InvalidOperationException();
    }
}