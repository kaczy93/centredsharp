using Shared;

namespace Server; 

public class Block {
    public Block(MapBlock map, SeparatedStaticBlock statics) {
        MapBlock = map;
        StaticBlock = statics;
    }
    
    public MapBlock MapBlock { get; }
    public SeparatedStaticBlock StaticBlock { get; }
}