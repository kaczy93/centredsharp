using CentrED.Network;

namespace CentrED.Client.Map;

public partial class ClientLandscape : BaseLandscape
{
    private readonly CentrEDClient _client;

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
            if (block.Disposed)
            {
                _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
            }
            // Do not re-add removed blocks to the cache.
        };

        LandTileReplaced += (tile, newId, newZ) =>
        {
            _client.PushUndoPacket(new DrawMapPacket(tile));
            var packet = new DrawMapPacket(tile, newId, newZ);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };
        LandTileElevated += (tile, newZ) =>
        {
            _client.PushUndoPacket(new DrawMapPacket(tile));
            var packet = new DrawMapPacket(tile, newZ);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };

        StaticTileAdded += tile =>
        {
            _client.PushUndoPacket(new DeleteStaticPacket(tile));
            var packet = new InsertStaticPacket(tile);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };
        StaticTileRemoved += tile =>
        {
            _client.PushUndoPacket(new InsertStaticPacket(tile));
            var packet = new DeleteStaticPacket(tile);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };
        StaticTileReplaced += (tile, newId) =>
        {
            var shouldEndGroup = _client.BeginUndoGroup();
            _client.PushUndoPacket(new InsertStaticPacket(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue));
            _client.PushUndoPacket(new DeleteStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
            if(shouldEndGroup)
                _client.EndUndoGroup();

            var delPacket = new DeleteStaticPacket(tile);
            var insPacket = new InsertStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue);
            if (_client.BulkMode)
            {
                _client.Send(delPacket);
                _client.Send(insPacket);
            }
            else
            {
                _client.SendAndWait(delPacket);
                _client.SendAndWait(insPacket);
            }
        };
        StaticTileMoved += (tile, newX, newY) =>
        {
            _client.PushUndoPacket(new MoveStaticPacket(newX, newY, tile.Z, tile.Id, tile.Hue, tile.X, tile.Y));
            var packet = new MoveStaticPacket(tile, newX, newY);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };
        StaticTileElevated += (tile, newZ) =>
        {
            _client.PushUndoPacket(new ElevateStaticPacket(tile.X, tile.Y, newZ, tile.Id, tile.Hue, tile.Z));
            var packet = new ElevateStaticPacket(tile, newZ);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
        };
        StaticTileHued += (tile, newHue) =>
        {
            _client.PushUndoPacket(new HueStaticPacket(tile.X, tile.Y, tile.Z, tile.Id,newHue, tile.Hue));
            var packet = new HueStaticPacket(tile, newHue);
            if (_client.BulkMode)
                _client.Send(packet);
            else
                _client.SendAndWait(packet);
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

    public override void LogInfo(string message)
    {
        _client.LogInfo(message);
    }

    public override void LogWarn(string message)
    {
        _client.LogWarn(message);
    }

    public override void LogError(string message)
    {
        _client.LogError(message);
    }

    public override void LogDebug(string message)
    {
        _client.LogDebug(message);
    }
}