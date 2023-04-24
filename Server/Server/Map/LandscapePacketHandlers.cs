using CentrED.Network;

namespace CentrED.Server; 

public partial class Landscape {
    private void OnDrawMapPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnDrawMapPacket");
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var tile = GetLandTile(x, y);

        lock (tile) {
            tile.Z = buffer.ReadSByte();
            tile.TileId = buffer.ReadUInt16();

            WorldBlock block = tile.Owner!;
            var packet = new DrawMapPacket(tile);
            foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
                netState.Send(packet);
            }

            UpdateRadar(x, y);
        }
    }

    private void OnInsertStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnInsertStaticPacket");
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        lock (block) {
            var staticItem = new StaticTile {
                X = x,
                Y = y,
                Z = buffer.ReadSByte(),
                TileId = buffer.ReadUInt16(),
                Hue = buffer.ReadUInt16()
            };
            block.Tiles.Add(staticItem);
            SortStaticList(block.Tiles);
            staticItem.Owner = block;

            var packet = new InsertStaticPacket(staticItem);
            foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
                netState.Send(packet);
            }

            UpdateRadar(x, y);
        }
    }

    private void OnDeleteStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnDeleteStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        lock (block) {
            var statics = block.CellItems(GetTileId(x, y));

            var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
            if (staticItem == null) return;
            
            var packet = new DeleteStaticPacket(staticItem);
            
            staticItem.Delete();
            block.Tiles.Remove(staticItem);

            foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
                netState.Send(packet);
            }

            UpdateRadar(x, y);
        }
    }

    private void OnElevateStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnElevateStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        lock (block) {
            var statics = block.CellItems(GetTileId(x, y));
            
            var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
            if (staticItem == null) return;

            var newZ = buffer.ReadSByte();
            var packet = new ElevateStaticPacket(staticItem, newZ);
            staticItem.Z = newZ;
            SortStaticList(block.Tiles);

            foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
                netState.Send(packet);
            }

            UpdateRadar(x, y);
        }
    }

    private void OnMoveStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnMoveStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var newX = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellWidth - 1);
        var newY = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellHeight - 1);
        
        if (staticInfo.X == newX && staticInfo.Y == newY) return;

        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y)) return;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, newX, newY)) return;
        
        if((Math.Abs(staticInfo.X - newX) > 8 || Math.Abs(staticInfo.Y - newY) > 8) && 
           !PacketHandlers.ValidateAccess(ns, AccessLevel.Developer)) return;

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));

        lock(sourceBlock) lock(targetBlock) {
            var statics = sourceBlock.CellItems(GetTileId(staticInfo.X, staticInfo.Y));
            
            StaticTile? staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
            if (staticItem == null) return;
            
            var deletePacket = new DeleteStaticPacket(staticItem);
            var movePacket = new MoveStaticPacket(staticItem, newX, newY);

            sourceBlock.Tiles.Remove(staticItem);

            targetBlock.Tiles.Add(staticItem);
            staticItem.UpdatePos(newX, newY, staticItem.Z);
            staticItem.Owner = targetBlock;

            var insertPacket = new InsertStaticPacket(staticItem);

            SortStaticList(targetBlock.Tiles);

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

            UpdateRadar(staticInfo.X, staticInfo.Y);
            UpdateRadar(newX, newY);
        }
    }

    private void OnHueStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnHueStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));

        lock (block) {
            var statics = block.CellItems(GetTileId(x, y));

            var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
            if (staticItem == null) return;

            var newHue = buffer.ReadUInt16();
            var packet = new HueStaticPacket(staticItem, newHue);
            staticItem.Hue = newHue;

            var subscriptions = GetBlockSubscriptions(block.X, block.Y);
            foreach (var netState in subscriptions) {
                netState.Send(packet);
            }
        }
    }

    private void OnLargeScaleCommandPacket(BinaryReader reader, NetState ns) {
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Developer)) return;
        var logMsg = $"{ns.Username} begins large scale operation";
        ns.LogInfo(logMsg);
        CEDServer.Send(new ServerStatePacket(ServerState.Other, logMsg));
        
        //Bitmask
        var bitMask = new ulong[Width * Height];
        //'additionalAffectedBlocks' is used to store whether a certain block was
        //touched during an operation which was designated to another block (for
        //example by moving items with an offset). This is (indirectly) merged later
        //on.
        var additionalAffectedBlocks = new bool[Width * Height];

        var clients = new Dictionary<NetState, List<BlockCoords>>();
        foreach (var netState in CEDServer.Clients) {
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
        LsCopyMove cmOperation;
        var blockOffX = 0;
        var cellOffX = 0;
        var modX = 1;
        var blockOffY = 0;
        var cellOffY = 0;
        var modY = 1;

        if (reader.ReadBoolean()) {
            cmOperation = new LsCopyMove(reader, this);
            if (cmOperation.OffsetX != 0 || cmOperation.OffsetY != 0) {
                operations.Add(cmOperation);

                if (cmOperation.OffsetX > 0) {
                    blockOffX = Width - 1;
                    cellOffX = 7;
                    modX = -1;
                }

                if (cmOperation.OffsetY > 0) {
                    blockOffY = Height - 1;
                    cellOffY = 7;
                    modY = -1;
                }
            }
        }
        if(reader.ReadBoolean()) operations.Add(new LsSetAltitude(reader, this));
        if(reader.ReadBoolean()) operations.Add(new LsDrawTerrain(reader, this));
        if(reader.ReadBoolean()) operations.Add(new LsDeleteStatics(reader, this));
        if(reader.ReadBoolean()) operations.Add(new LsInsertStatics(reader, this));
        _radarMap.BeginUpdate();
        for (ushort blockX = 0; blockX < Width; blockX++) {
            var realBlockX = (ushort)(blockOffX + modX * blockX);
            for (ushort blockY = 0; blockY < Height; blockY++) {
                var realBlockY = (ushort)(blockOffY + modY * blockY);
                var blockId = (ushort)(realBlockX * Height + realBlockY);
                if(bitMask[blockId] == 0) continue;

                for (int cellY = 0; cellY < 8; cellY++) {
                    var realCellY = (ushort)(cellOffY + modY * cellY);
                    for (int cellX = 0; cellX < 8; cellX++) {
                        var realCellX = (ushort)(cellOffX + modX * cellX);
                        if((bitMask[blockId] & 1u << (realCellY * 8 + realCellX)) == 0) continue;

                        var x = (ushort)(realBlockX * 8 * realCellX);
                        var y = (ushort)(realBlockY * 8 * realCellY);
                        var mapTile = GetLandTile(x, y);
                        var staticBlock = GetStaticBlock(x, y);
                        var statics = staticBlock.CellItems(GetTileId(x, y));
                        foreach (var operation in operations) {
                            operation.Apply(mapTile, statics, ref additionalAffectedBlocks);
                        }
                        SortStaticList(staticBlock.Tiles);
                        UpdateRadar(x, y);
                    }
                }
                
                //Notify affected clients
                foreach (var netState in GetBlockSubscriptions(realBlockX, realBlockY)) {
                    clients[netState].Add(new BlockCoords(realBlockX, realBlockY));
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
                
                UpdateRadar((ushort)(blockX * 8), (ushort)(blockY * 8));
            }
        }
        _radarMap.EndUpdate();
        
        foreach (var (netState, blocks) in clients) {
            if (blocks.Count > 0) {
                netState.Send(new CompressedPacket(new BlockPacket(blocks, null)));
                netState.LastAction = DateTime.Now;
            }
        }
        CEDServer.Send(new ServerStatePacket(ServerState.Running));
        ns.LogInfo("Large scale operation ended.");
    }
}