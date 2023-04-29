using CentrED.Network;

namespace CentrED.Server; 

public partial class Landscape {
    private void OnDrawMapPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnDrawMapPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var tile = GetLandTile(x, y);

        tile.Z = reader.ReadSByte();
        var newId = reader.ReadUInt16();
        AssertLandTileId(newId);
        tile.Id = newId;

        WorldBlock block = tile.Owner!;
        var packet = new DrawMapPacket(tile);
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnInsertStaticPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnInsertStaticPacket");
        var staticInfo = new StaticInfo(reader);
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y)) return;

        var block = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));

        var staticTile = new StaticTile(
            staticInfo.TileId,
            staticInfo.X,
            staticInfo.Y,
            staticInfo.Z,
            staticInfo.Hue
        );
        AssertStaticTileId(staticTile.Id);
        AssertHue(staticTile.Hue);
        block.AddTile(staticTile);
        block.SortTiles(TileDataProvider);
        staticTile.Owner = block;

        var packet = new InsertStaticPacket(staticTile);
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
            netState.Send(packet);
        }

        UpdateRadar(ns, staticInfo.X, staticInfo.Y);
    }

    private void OnDeleteStaticPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnDeleteStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        
        var statics = block.CellItems(GetTileId(x, y));

        var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;
        
        var packet = new DeleteStaticPacket(staticItem);
        
        staticItem.Delete();
        block.RemoveTile(staticItem);

        foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnElevateStaticPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnElevateStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        var statics = block.CellItems(GetTileId(x, y));
        
        var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;

        var newZ = reader.ReadSByte();
        var packet = new ElevateStaticPacket(staticItem, newZ);
        staticItem.Z = newZ;
        block.SortTiles(TileDataProvider);

        foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnMoveStaticPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnMoveStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newX = (ushort)Math.Clamp(reader.ReadUInt16(), 0, CellWidth - 1);
        var newY = (ushort)Math.Clamp(reader.ReadUInt16(), 0, CellHeight - 1);
        
        if (staticInfo.X == newX && staticInfo.Y == newY) return;

        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y)) return;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, newX, newY)) return;
        
        if((Math.Abs(staticInfo.X - newX) > 8 || Math.Abs(staticInfo.Y - newY) > 8) && 
           !PacketHandlers.ValidateAccess(ns, AccessLevel.Developer)) return;

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));

        var statics = sourceBlock.CellItems(GetTileId(staticInfo.X, staticInfo.Y));
        
        StaticTile? staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;
        
        var deletePacket = new DeleteStaticPacket(staticItem);
        var movePacket = new MoveStaticPacket(staticItem, newX, newY);

        sourceBlock.RemoveTile(staticItem);

        targetBlock.AddTile(staticItem);
        staticItem.UpdatePos(newX, newY, staticItem.Z);
        staticItem.Owner = targetBlock;

        var insertPacket = new InsertStaticPacket(staticItem);

        targetBlock.SortTiles(TileDataProvider);

        var sourceSubscriptions = GetBlockSubscriptions(sourceBlock.X, sourceBlock.Y);
        var targetSubscriptions = GetBlockSubscriptions(targetBlock.X, targetBlock.Y);

        var moveSubscriptions = sourceSubscriptions.Intersect(targetSubscriptions);
        var deleteSubscriptions = sourceSubscriptions.Except(targetSubscriptions);
        var insertSubscriptions = targetSubscriptions.Except(sourceSubscriptions);

        foreach (var netState in insertSubscriptions) {
            netState.Send(insertPacket);
        }

        foreach (var netState in deleteSubscriptions) {
            netState.Send(deletePacket);
        }

        foreach (var netState in moveSubscriptions) {
            netState.Send(movePacket);
        }

        UpdateRadar(ns, staticInfo.X, staticInfo.Y);
        UpdateRadar(ns, newX, newY);
    }

    private void OnHueStaticPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnHueStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        var statics = block.CellItems(GetTileId(x, y));

        var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;

        var newHue = reader.ReadUInt16();
        AssertHue(newHue);
        var packet = new HueStaticPacket(staticItem, newHue);
        staticItem.Hue = newHue;

        var subscriptions = GetBlockSubscriptions(block.X, block.Y);
        foreach (var netState in subscriptions) {
            netState.Send(packet);
        }
    }

    private void OnLargeScaleCommandPacket(BinaryReader reader, NetState<CEDServer> ns) {
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Developer)) return;
        var logMsg = $"{ns.Username} begins large scale operation";
        ns.LogInfo(logMsg);
        ns.Parent.Send(new ServerStatePacket(ServerState.Other, logMsg));
        try {
            var bitMask = new ulong[Width * Height];
            //designated to another block (for example by moving items with an offset). This is (indirectly) merged later on.
            var additionalAffectedBlocks = new bool[Width * Height];

            var clients = new Dictionary<NetState<CEDServer>, List<BlockCoords>>();
            foreach (var netState in ns.Parent.Clients) {
                clients.Add(netState, new List<BlockCoords>());
            }

            var areaCount = reader.ReadByte();
            var areaInfos = new AreaInfo[areaCount];
            for (int i = 0; i < areaCount; i++) {
                areaInfos[i] = new AreaInfo(reader);
                for (ushort x = areaInfos[i].Left; x < areaInfos[i].Right; x++) {
                    for (ushort y = areaInfos[i].Top; y < areaInfos[i].Bottom; y++) {
                        var blockId = GetBlockId(x, y);
                        var cellId = GetTileId(x, y);
                        bitMask[blockId] |= 1u << cellId; //set bit
                    }
                }
            }

            List<LargeScaleOperation> operations = new List<LargeScaleOperation>();

            var xRange = Enumerable.Range(0, Width);
            var yRange = Enumerable.Range(0, Height);
            
            if (reader.ReadBoolean()) {
                LsCopyMove copyMove = new LsCopyMove(reader, this);
                if (copyMove.OffsetX > 0)
                    xRange = xRange.Reverse();
                if (copyMove.OffsetY > 0)
                    yRange = yRange.Reverse();
                operations.Add(copyMove);
            }

            if (reader.ReadBoolean()) operations.Add(new LsSetAltitude(reader, this));
            if (reader.ReadBoolean()) operations.Add(new LsDrawTerrain(reader, this));
            if (reader.ReadBoolean()) operations.Add(new LsDeleteStatics(reader, this));
            if (reader.ReadBoolean()) operations.Add(new LsInsertStatics(reader, this));
            foreach (var operation in operations) {
                operation.Validate();
            }
            
            _radarMap.BeginUpdate();
            foreach(ushort blockX in xRange) {
                foreach(ushort blockY in yRange) {
                    var blockId = blockX * Height + blockY;
                    if (bitMask[blockId] == 0) continue;

                    for (ushort tileY = 0; tileY < 8; tileY++) {
                        for (ushort tileX = 0; tileX < 8; tileX++) {
                            var tileId = GetTileId(tileX, tileY);
                            if ((bitMask[blockId] & 1u << tileId) == 0) continue;

                            var x = (ushort)(blockX * 8 + tileX);
                            var y = (ushort)(blockY * 8 + tileY);
                            var mapTile = GetLandTile(x, y);
                            var staticBlock = GetStaticBlock(blockX, blockY);
                            var statics = staticBlock.CellItems(tileId);
                            foreach (var operation in operations) {
                                operation.Apply(mapTile, statics, ref additionalAffectedBlocks);
                            }

                            staticBlock.SortTiles(TileDataProvider);
                            UpdateRadar(ns, x, y);
                        }
                    }

                    //Notify affected clients
                    foreach (var netState in GetBlockSubscriptions(blockX, blockY)) {
                        clients[netState].Add(new BlockCoords(blockX, blockY));
                    }
                }
            }

            //aditional blocks
            for (ushort blockX = 0; blockX < Width; blockX++) {
                for (ushort blockY = 0; blockY < Height; blockY++) {
                    var blockId = (ushort)(blockX * Height + blockY);
                    if (bitMask[blockId] != 0 || !additionalAffectedBlocks[blockId]) continue;

                    foreach (var netState in GetBlockSubscriptions(blockX, blockY)!) {
                        clients[netState].Add(new BlockCoords(blockX, blockY));
                    }

                    UpdateRadar(ns, (ushort)(blockX * 8), (ushort)(blockY * 8));
                }
            }

            _radarMap.EndUpdate(ns);

            foreach (var (netState, blocks) in clients) {
                if (blocks.Count > 0) {
                    netState.Send(new CompressedPacket(new BlockPacket(blocks, netState, false)));
                }
            }
        }
        catch (Exception e) {
            ns.LogError($"LSO Failed {e}");
        }
        finally {
            ns.Parent.Send(new ServerStatePacket(ServerState.Running));
        }
        ns.LogInfo("Large scale operation ended.");
    }
}