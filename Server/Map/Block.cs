//Server/ULandscape.pas
using Shared;

namespace Server; 

//This could be a record
//TBlock
public class Block {
    public Block(MapBlock map, SeparatedStaticBlock statics) {
        MapBlock = map;
        StaticBlock = statics;
    }
    
    public MapBlock MapBlock { get; }
    public SeparatedStaticBlock StaticBlock { get; }
}