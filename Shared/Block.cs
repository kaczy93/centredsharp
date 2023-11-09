namespace CentrED;

public class Block
{
    public Block(LandBlock land, StaticBlock statics)
    {
        LandBlock = land;
        StaticBlock = statics;
    }

    public LandBlock LandBlock { get; }
    public StaticBlock StaticBlock { get; }

    public static int Id(Block block)
    {
        return Id(block.LandBlock.X, block.LandBlock.Y);
    }

    public static int Id(ushort x, ushort y)
    {
        return HashCode.Combine(x, y);
    }
}