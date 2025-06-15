using System.Buffers;
using CentrED.Network;

namespace CentrED.Client.Map;

public partial class ClientLandscape
{
    private void OnBlockPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnBlockPacket");
        while (reader.Remaining > 0)
        {
            var coords = reader.ReadBlockCoords();

            var landBlockReader = new SpanReader(reader.Buffer.Slice(reader.Position, LandBlock.SIZE));
            var landBlock = new LandBlock(this, coords.X, coords.Y, landBlockReader);
            reader.Seek(landBlockReader.Length, SeekOrigin.Current);
            
            var staticsCount = reader.ReadUInt16();
            var staticBlockReader = new SpanReader(reader.Buffer.Slice(reader.Position, staticsCount * StaticTile.SIZE));
            var staticBlock = new StaticBlock(this, coords.X, coords.Y, staticBlockReader);
            reader.Seek(staticBlockReader.Length, SeekOrigin.Current);
            
            var block = new Block(landBlock, staticBlock);
            if(ns.Parent.StaticTileData != null)
                block.StaticBlock.SortTiles(ref ns.Parent.StaticTileData);
            if (BlockCache.Contains(Block.Id(block)))
            {
                ns.Parent.OnBlockReleased(block);
            }
            BlockCache.Add(block);
            ns.Parent.OnBlockLoaded(block);
            ns.Parent.RequestedBlocks.Remove(coords);
        }
    }

    private void OnDrawMapPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnDrawMapPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        var tile = GetLandTile(x, y);

        var newZ = reader.ReadSByte();
        if (tile.Z != newZ)
        {
            ns.Parent.OnLandElevated(tile, newZ);
            InternalSetLandZ(tile, newZ);
        }

        var newId = reader.ReadUInt16();
        // AssertLandTileId(newId); //Bring me back once we have TileDataProvider in client :)
        ns.Parent.OnLandReplaced(tile, newId, newZ);
        InternalSetLandId(tile, newId);
    }

    private void OnInsertStaticPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnInsertStaticPacket");
        var staticInfo = reader.ReadStaticInfo();

        var block = GetStaticBlock(staticInfo);
        var newTile = new StaticTile(staticInfo);
        // AssertStaticTileId(newTile.Id);
        // AssertHue(newTile.Hue);

        ns.Parent.OnStaticTileAdded(newTile);
        InternalAddStatic(block, newTile);
        if(ns.Parent.StaticTileData != null)
            block.SortTiles(ref ns.Parent.StaticTileData);
        ns.Parent.OnAfterStaticChanged(newTile);
    }

    private void OnDeleteStaticPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnDeleteStaticPacket");
        var staticInfo = reader.ReadStaticInfo();

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        if (tile == null)
        {
            ns.LogError($"OnDeleteStaticPacket static not found {staticInfo}");
            return;
        }
        InternalRemoveStatic(block, tile);
        ns.Parent.OnStaticTileRemoved(tile);
    }

    private void OnElevateStaticPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnElevateStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var newZ = reader.ReadSByte();

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        if (tile == null)
        {
            ns.LogError($"OnElevateStaticPacket static not found {staticInfo}");
            return;
        }

        ns.Parent.OnStaticTileElevated(tile, newZ);
        InternalSetStaticZ(tile, newZ);
        if(ns.Parent.StaticTileData != null)
            block.SortTiles(ref ns.Parent.StaticTileData);
        ns.Parent.OnAfterStaticChanged(tile);
    }

    private void OnMoveStaticPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnMoveStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var newX = reader.ReadUInt16();
        var newY = reader.ReadUInt16();

        var sourceBlock = GetStaticBlock(staticInfo);
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        var tile = sourceBlock.Find(staticInfo);
        if (tile == null)
        {
            ns.LogError($"OnMoveStaticPacket static not found {staticInfo}");
            return;
        }

        InternalRemoveStatic(sourceBlock, tile);
        ns.Parent.OnStaticTileMoved(tile, newX, newY);
        InternalSetStaticPos(tile, newX, newY);
        InternalAddStatic(targetBlock, tile);
        ns.Parent.OnAfterStaticChanged(tile);
    }

    private void OnHueStaticPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnHueStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var newHue = reader.ReadUInt16();
        // AssertHue(newTile.Hue);

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        if (tile == null)
        {
            ns.LogError($"OnHueStaticPacket static not found {staticInfo}");
            return;
        }

        ns.Parent.OnStaticTileHued(tile, newHue);
        InternalSetStaticHue(tile, newHue);
        ns.Parent.OnAfterStaticChanged(tile);
    }
}