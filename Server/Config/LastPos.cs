namespace CentrED.Server.Config;

public class LastPos(ushort x, ushort y)
{
    public LastPos() : this(0, 0)
    {
    }

    public ushort X { get; set; } = x;
    public ushort Y { get; set; } = y;

    public override string ToString()
    {
        return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
    }
}