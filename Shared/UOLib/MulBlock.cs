namespace Shared;

public abstract class MulBlock {
    public int Id { get; set; }
    public abstract void Write(BinaryWriter writer);
}