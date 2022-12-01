using Shared;
using Shared.MulProvider;

namespace Server; 

public class SeparatedStaticBlock : StaticBlock {
    public SeparatedStaticBlock(Stream data, GenericIndex index, ushort x, ushort y) {
    }
    
    public SeparatedStaticBlock(Stream data, GenericIndex index) {
    }
    
    public List<StaticItem>[] Cells;
    
    public TileDataProvider TileDataProvider { get; set; }

    public SeparatedStaticBlock Clone() {
        
    }

    public int GetSize() {
        
    }

    public void RebuildList() {
        
    }
}