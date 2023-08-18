using CentrED.Network;

namespace CentrED.Client.Map; 

public partial class ClientLandscape {
    private void OnBlockPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnBlockPacket");
        var index = new GenericIndex();
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            var coords = new BlockCoords(reader);

            var landBlock = new LandBlock(this, coords.X, coords.Y, reader);
            var staticsCount = reader.ReadUInt16();
            if(staticsCount > 0 )
                index.Lookup = (int)reader.BaseStream.Position;
            else {
                index.Lookup = -1;
            }
            index.Length = StaticTile.Size * staticsCount;
            var staticBlock = new StaticBlock(this, reader, index, coords.X, coords.Y);
            var block = new Block(landBlock, staticBlock);
            BlockCache.Add(Block.Id(block), block);
            ns.Parent.OnBlockLoaded(block);
        }
    }
    
    private void OnDrawMapPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnDrawMapPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        var tile = GetLandTile(x, y);

        var newZ = reader.ReadSByte();
        if (tile.Z != newZ) {
            ns.Parent.OnLandElevated(tile, newZ);
            InternalSetLandZ(tile, newZ);
        }

        var newId = reader.ReadUInt16();
        // AssertLandTileId(newId); //Bring me back once we have TileDataProvider in client :)
        if (tile.Id != newId) {
            ns.Parent.OnLandReplaced(tile, newId);
            InternalSetLandId(tile, newId);
        }
    }

    private void OnInsertStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnInsertStaticPacket");
        var staticInfo = new StaticInfo(reader);

        var block = GetStaticBlock((ushort)(staticInfo.X /8), (ushort)(staticInfo.Y / 8));
        var newTile = new StaticTile(staticInfo);
        // AssertStaticTileId(newTile.Id);
        // AssertHue(newTile.Hue);
        
        OnStaticTileAdded(newTile);
        InternalAddStatic(block, newTile);
    }

    private void OnDeleteStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnDeleteStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var x = staticInfo.X;
        var y = staticInfo.Y;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        var staticTile = new StaticTile(staticInfo);
        var removed = InternalRemoveStatic(block, staticTile);

        if (!removed) {
            ns.LogError($"OnDeleteStaticPacket static not found {staticInfo}");
            return;
        }
        
        ns.Parent.OnStaticTileRemoved(staticTile);
    }

    private void OnElevateStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnElevateStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newZ = reader.ReadSByte();

        var tile = GetStaticTiles(staticInfo.X, staticInfo.Y).FirstOrDefault(s => s.Match(staticInfo));
        if (tile == null) {
            ns.LogError($"OnElevateStaticPacket static not found {staticInfo}");
            return;
        }

        ns.Parent.OnStaticTileElevated(tile, newZ);
        InternalSetStaticZ(tile, newZ);
    }

    private void OnMoveStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnMoveStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newX = reader.ReadUInt16();
        var newY = reader.ReadUInt16();

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        var tile = new StaticTile(staticInfo);
        
        var removed = InternalRemoveStatic(sourceBlock, tile);
        if (!removed) {
            ns.LogError($"OnMoveStaticPacket static not found {staticInfo}");
            return;
        }

        ns.Parent.OnStaticTileMoved(tile, newX, newY);
        InternalSetStaticPos(tile, newX, newY);
        InternalAddStatic(targetBlock, tile);
    }
    private void OnHueStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnHueStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newHue = reader.ReadUInt16();
        // AssertHue(newTile.Hue);
        
        var tile = GetStaticTiles(staticInfo.X, staticInfo.Y).FirstOrDefault(s => s.Match(staticInfo));
        if (tile == null) {
            ns.LogError($"OnHueStaticPacket static not found {staticInfo}");
            return;
        }

        ns.Parent.OnStaticTileHued(tile, newHue);
        InternalSetStaticHue(tile, newHue);
    }
}