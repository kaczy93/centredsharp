using CentrED.Network;

namespace CentrED.Client; 

public partial class Landscape : BaseLandscape{

    private CentrEDClient _client;
    
    public Landscape(CentrEDClient client, ushort width, ushort height) : base(width, height) {
        _client = client;
        OnFreeBlock = FreeBlock;
        PacketHandlers.RegisterPacketHandler(0x04, 0, OnBlockPacket);
    }
    
    protected override Block LoadBlock(ushort x, ushort y) {
        _client.Send(new RequestBlocksPacket(new BlockCoords(x,y)));
        var blockId = BlockCache.BlockId(x, y);
        var block = BlockCache.Get(blockId);
        while (block == null) {
            Thread.Sleep(1);
            block = BlockCache.Get(blockId);
        }

        return block;
    }

    private void FreeBlock(Block block) {
        _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
    }
}