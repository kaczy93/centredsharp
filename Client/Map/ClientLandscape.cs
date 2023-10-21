using CentrED.Network;

namespace CentrED.Client.Map;

public partial class ClientLandscape : BaseLandscape {
    private CentrEDClient _client;

    public ClientLandscape(CentrEDClient client, ushort width, ushort height) : base(width, height) {
        _client = client;
        PacketHandlers.RegisterPacketHandler(0x04, 0, OnBlockPacket);
        PacketHandlers.RegisterPacketHandler(0x06, 8, OnDrawMapPacket);
        PacketHandlers.RegisterPacketHandler(0x07, 10, OnInsertStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x08, 10, OnDeleteStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x09, 11, OnElevateStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x0A, 14, OnMoveStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x0B, 12, OnHueStaticPacket);
        
        // ClientLandscape events are used to send changes done by the user to the server
        BlockUnloaded += block => _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
        
        LandTileReplaced += (tile, newId) => _client.Send(new DrawMapPacket(tile, newId));
        LandTileElevated += (tile, newZ) => _client.Send(new DrawMapPacket(tile, newZ));
        
        StaticTileAdded += tile => _client.Send(new InsertStaticPacket(tile));
        StaticTileRemoved += tile => _client.Send(new DeleteStaticPacket(tile));
        StaticTileReplaced += (tile, newId) => {
            _client.Send(new DeleteStaticPacket(tile));
            _client.Send(new InsertStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
        };
        StaticTileMoved += (tile, newX, newY) => _client.Send(new MoveStaticPacket(tile, newX, newY));
        StaticTileElevated += (tile, newZ) => _client.Send(new ElevateStaticPacket(tile, newZ));
        StaticTileHued += (tile, newHue) => _client.Send(new HueStaticPacket(tile, newHue));
    }
    
    protected override Block LoadBlock(ushort x, ushort y) {
        AssertBlockCoords(x, y);
        _client.Send(new RequestBlocksPacket(new BlockCoords(x, y)));
        var blockId = Block.Id(x, y);
        var block = BlockCache.Get(blockId);
        while (_client.Running && block == null) {
            Thread.Sleep(1);
            _client.Update();
            block = BlockCache.Get(blockId);
        }

        return block;
    }
}