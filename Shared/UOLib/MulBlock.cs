namespace Shared;

public abstract class MulBlock {
    public int Id { get; set; }
    public abstract int GetSize { get; }
    public abstract MulBlock Clone();
    public abstract void Write(BinaryWriter writer);
}