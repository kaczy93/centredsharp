namespace CentrED.Client; 

public class Landscape {
    private CentrEDClient _client;
    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }

    private BlockCache _blockCache;
    public Landscape(CentrEDClient client, ushort width, ushort height) {
        Width = width;
        Height = height;
        CellWidth = (ushort)(width * 8);
        CellHeight = (ushort)(height * 8);
        _blockCache = new BlockCache(FreeBlock, 256); 
    }

    private void FreeBlock(Block block) {
        _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
    }
}