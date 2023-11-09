namespace CentrED;

public abstract class MulEntry
{
    public int Id { get; set; }
    public abstract void Write(BinaryWriter writer);
}