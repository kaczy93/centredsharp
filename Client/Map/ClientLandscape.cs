using CentrED.Network;

namespace CentrED.Client.Map;

public partial class ClientLandscape : BaseLandscape
{
    private CentrEDClient _client;

    public ClientLandscape(CentrEDClient client, ushort width, ushort height) : base(width, height)
    {
        _client = client;
        PacketHandlers.RegisterPacketHandler(0x04, 0, OnBlockPacket);
        PacketHandlers.RegisterPacketHandler(0x06, 8, OnDrawMapPacket);
        PacketHandlers.RegisterPacketHandler(0x07, 10, OnInsertStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x08, 10, OnDeleteStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x09, 11, OnElevateStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x0A, 14, OnMoveStaticPacket);
        PacketHandlers.RegisterPacketHandler(0x0B, 12, OnHueStaticPacket);

        // ClientLandscape events are used to send changes done by the user to the server
        BlockUnloaded += block =>
        {
            _client.OnBlockReleased(block);
            if(block.Disposed)
            {
                _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
            }
            else
            {
                //Not disposed because still used, put it back
                BlockCache.Add(Block.Id(block), block);
            }
        };

        LandTileReplaced += (tile, newId) =>
        {
            _client.UndoStack.Push(new DrawMapPacket(tile));
            _client.Send(new DrawMapPacket(tile, newId));
        };
        LandTileElevated += (tile, newZ) =>
        {
            _client.UndoStack.Push(new DrawMapPacket(tile));
            _client.Send(new DrawMapPacket(tile, newZ));
        };

        StaticTileAdded += tile =>
        {
            _client.UndoStack.Push(new DeleteStaticPacket(tile));
            _client.Send(new InsertStaticPacket(tile));
        };
        StaticTileRemoved += tile =>
        {
            _client.UndoStack.Push(new InsertStaticPacket(tile));
            _client.Send(new DeleteStaticPacket(tile));
        };
        StaticTileReplaced += (tile, newId) =>
        {
            _client.UndoStack.Push(new InsertStaticPacket(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue));
            _client.UndoStack.Push(new DeleteStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
            _client.Send(new DeleteStaticPacket(tile));
            _client.Send(new InsertStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
        };
        StaticTileMoved += (tile, newX, newY) =>
        {
            _client.UndoStack.Push(new MoveStaticPacket(newX, newY, tile.Z, tile.Id, tile.Hue, tile.X, tile.Y));
            _client.Send(new MoveStaticPacket(tile, newX, newY));
        };
        StaticTileElevated += (tile, newZ) =>
        {
            _client.UndoStack.Push(new ElevateStaticPacket(tile.X, tile.Y, newZ, tile.Id, tile.Hue, tile.Z));
            _client.Send(new ElevateStaticPacket(tile, newZ));
        };
        StaticTileHued += (tile, newHue) =>
        {
            _client.UndoStack.Push(new HueStaticPacket(tile.X, tile.Y, tile.Z, tile.Id,newHue, tile.Hue));
            _client.Send(new HueStaticPacket(tile, newHue));
        };
    }

    protected override Block LoadBlock(ushort x, ushort y)
    {
        AssertBlockCoords(x, y);
        _client.Send(new RequestBlocksPacket(new BlockCoords(x, y)));
        var blockId = Block.Id(x, y);
        var block = BlockCache.Get(blockId);
        while (_client.Running && block == null)
        {
            Thread.Sleep(1);
            _client.Update();
            block = BlockCache.Get(blockId);
        }

        return block;
    }
}