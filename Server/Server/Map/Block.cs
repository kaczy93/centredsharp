namespace CentrED.Server; 

public class Block {
    public Block(LandBlock land, StaticBlock statics) {
        LandBlock = land;
        StaticBlock = statics;
    }
    
    public LandBlock LandBlock { get; }
    public StaticBlock StaticBlock { get; }
}