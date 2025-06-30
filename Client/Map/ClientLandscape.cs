using CentrED.Network;

namespace CentrED.Client.Map;

public partial class ClientLandscape : BaseLandscape
{
    private readonly CentrEDClient _client;
    
    public ClientLandscape(CentrEDClient client, ushort width, ushort height) : base(width, height)
    {
        _client = client;
        // ClientLandscape events are used to send changes done by the user to the server
        BlockUnloaded += OnBlockUnloaded;

        LandTileReplaced += OnLandTileReplaced;
        LandTileElevated += OnLandTileElevated;

        StaticTileAdded += OnStaticAdded;
        StaticTileRemoved += OnStaticRemoved;
        StaticTileReplaced += OnStaticReplaced;
        StaticTileMoved += OnStaticMoved;
        StaticTileElevated += OnStaticElevated;
        StaticTileHued += OnStaticHued;
    }

    public void RegisterPacketHandlers(NetState<CentrEDClient> ns)
    {
        ns.RegisterPacketHandler(0x04, 0, OnBlockPacket);
        ns.RegisterPacketHandler(0x06, 8, OnDrawMapPacket);
        ns.RegisterPacketHandler(0x07, 10, OnInsertStaticPacket);
        ns.RegisterPacketHandler(0x08, 10, OnDeleteStaticPacket);
        ns.RegisterPacketHandler(0x09, 11, OnElevateStaticPacket);
        ns.RegisterPacketHandler(0x0A, 14, OnMoveStaticPacket);
        ns.RegisterPacketHandler(0x0B, 12, OnHueStaticPacket);
    }

    private void OnBlockUnloaded(Block block)
    {
        _client.OnBlockReleased(block);
        if (block.Disposed)
        {
            _client.Send(new FreeBlockPacket(block.LandBlock.X, block.LandBlock.Y));
        }
        else
        {
            //Not disposed because still used, put it back
            BlockCache.Add(block);
        }
    }

    private void OnLandTileReplaced(LandTile tile, ushort newId, sbyte newZ)
    {
        _client.PushUndoPacket(new DrawMapPacket(tile));
        _client.Send(new DrawMapPacket(tile, newId, newZ));
    }

    private void OnLandTileElevated(LandTile tile, sbyte newZ)
    {
        _client.PushUndoPacket(new DrawMapPacket(tile));
        _client.Send(new DrawMapPacket(tile, newZ));
    }

    private void OnStaticAdded(StaticTile tile)
    {
        _client.PushUndoPacket(new DeleteStaticPacket(tile));
        _client.Send(new InsertStaticPacket(tile));
    }

    private void OnStaticRemoved(StaticTile tile)
    {
        _client.PushUndoPacket(new InsertStaticPacket(tile));
        _client.Send(new DeleteStaticPacket(tile));
    }

    private void OnStaticReplaced(StaticTile tile, ushort newId)
    {
        var shouldEndGroup = _client.BeginUndoGroup();
        _client.PushUndoPacket(new InsertStaticPacket(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue));
        _client.PushUndoPacket(new DeleteStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
        if (shouldEndGroup)
            _client.EndUndoGroup();

        _client.Send(new DeleteStaticPacket(tile));
        _client.Send(new InsertStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
    }

    private void OnStaticMoved(StaticTile tile, ushort newX, ushort newY)
    {
        _client.PushUndoPacket(new MoveStaticPacket(newX, newY, tile.Z, tile.Id, tile.Hue, tile.X, tile.Y));
        _client.Send(new MoveStaticPacket(tile, newX, newY));
    }

    private void OnStaticElevated(StaticTile tile, sbyte newZ)
    {
        _client.PushUndoPacket(new ElevateStaticPacket(tile.X, tile.Y, newZ, tile.Id, tile.Hue, tile.Z));
        _client.Send(new ElevateStaticPacket(tile, newZ));
    }

    private void OnStaticHued(StaticTile tile, ushort newHue)
    {
        _client.PushUndoPacket(new HueStaticPacket(tile.X, tile.Y, tile.Z, tile.Id, newHue, tile.Hue));
        _client.Send(new HueStaticPacket(tile, newHue));
    }

    protected override Block LoadBlock(ushort x, ushort y)
    {
        AssertBlockCoords(x, y);
        _client.Send(new RequestBlocksPacket(new PointU16(x, y)));
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