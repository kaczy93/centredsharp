using Shared;

namespace Server; 

public class Block {
    public Block(MapBlock map, StaticBlock statics) {
        MapBlock = map;
        StaticBlock = statics;
    }
    
    public MapBlock MapBlock { get; }
    public StaticBlock StaticBlock { get; }
}