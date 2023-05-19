using System.Diagnostics.CodeAnalysis;
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

        LandBlock block = tile.Owner!;
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
        
        var statics = block.GetTiles(x, y);

        var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;
        
        block.RemoveTile(staticItem);
        
        var packet = new DeleteStaticPacket(staticItem);
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

        var statics = block.GetTiles(x, y);
        
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

        var statics = GetStaticTiles(staticInfo.X, staticInfo.Y);
        
        StaticTile? staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        if (staticItem == null) return;
        
        var deletePacket = new DeleteStaticPacket(staticItem);
        var movePacket = new MoveStaticPacket(staticItem, newX, newY);

        sourceBlock.RemoveTile(staticItem);
        targetBlock.AddTile(staticItem);
        staticItem.UpdatePos(newX, newY, staticItem.Z);

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

        var statics = block.GetTiles(x, y);

        var staticItem = statics.Where(staticInfo.Match).FirstOrDefault();
        var newHue = reader.ReadUInt16();
        if (staticItem == null) return;
        AssertHue(newHue);
        var packet = new HueStaticPacket(staticItem, newHue);
        staticItem.Hue = newHue;
        
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y)) {
            netState.Send(packet);
        }
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private void OnLargeScaleCommandPacket(BinaryReader reader, NetState<CEDServer> ns) {
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Developer)) return;
        var logMsg = $"{ns.Username} begins large scale operation";
        ns.LogInfo(logMsg);
        ns.Parent.Send(new ServerStatePacket(ServerState.Other, logMsg));
        try {
            var affectedBlocks = new bool[Width * Height];
            var affectedTiles = new bool[Width * Height, 64];
            var extraAffectedBlocks = new bool[Width * Height];

            var clients = new Dictionary<NetState<CEDServer>, HashSet<BlockCoords>>();
            foreach (var netState in ns.Parent.Clients) {
                clients.Add(netState, new HashSet<BlockCoords>());
            }

            var areaCount = reader.ReadByte();
            var areaInfos = new AreaInfo[areaCount];
            for (int i = 0; i < areaCount; i++) {
                areaInfos[i] = new AreaInfo(reader);
                for (ushort x = areaInfos[i].Left; x < areaInfos[i].Right; x++) {
                    for (ushort y = areaInfos[i].Top; y < areaInfos[i].Bottom; y++) {
                        var blockId = GetBlockId(x, y);
                        var tileId = GetTileId(x, y);
                        affectedBlocks[blockId] = true;
                        affectedTiles[blockId, tileId] = true;
                    }
                }
            }

            var minBlockX = Math.Max(0, areaInfos.Min(ai => ai.Left) / 8);
            var maxBlockX = Math.Min(Width, areaInfos.Max(ai => ai.Right) / 8 + 1);
            var minBlockY = Math.Max(0, areaInfos.Min(ai => ai.Top) / 8);
            var maxBlockY = Math.Min(Height, areaInfos.Max(ai => ai.Bottom) / 8 + 1);

            List<LargeScaleOperation> operations = new List<LargeScaleOperation>();

            var xBlockRange = Enumerable.Range(minBlockX, maxBlockX - minBlockX);
            var yBlockRange = Enumerable.Range(minBlockY, maxBlockY - minBlockY);
            var xTileRange = Enumerable.Range(0, 8);
            var yTileRange = Enumerable.Range(0, 8);
            
            if (reader.ReadBoolean()) {
                var copyMove = new LsCopyMove(reader, this);
                if (copyMove.OffsetX > 0) {
                    xBlockRange = xBlockRange.Reverse();
                    xTileRange = xTileRange.Reverse();
                }
                if (copyMove.OffsetY > 0) {
                    yBlockRange = yBlockRange.Reverse();
                    yTileRange = yTileRange.Reverse();
                }
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
            foreach(ushort blockX in xBlockRange) {
                foreach(ushort blockY in yBlockRange) {
                    var blockId = blockX * Height + blockY;
                    if (!affectedBlocks[blockId]) continue;

                    foreach (ushort tileY in yTileRange) {
                        foreach (ushort tileX in xTileRange) {
                            var tileId = GetTileId(tileX, tileY);
                            if (!affectedTiles[blockId, tileId]) continue;

                            var x = (ushort)(blockX * 8 + tileX);
                            var y = (ushort)(blockY * 8 + tileY);
                            var mapTile = GetLandTile(x, y);
                            var staticBlock = GetStaticBlock(blockX, blockY);
                            var statics = staticBlock.GetTiles(x,y);
                            foreach (var operation in operations) {
                                operation.Apply(mapTile, statics, ref extraAffectedBlocks);
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
                    if (affectedBlocks[blockId] || !extraAffectedBlocks[blockId]) continue;

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