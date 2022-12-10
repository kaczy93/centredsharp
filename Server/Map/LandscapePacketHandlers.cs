using Cedserver;
using Shared;

namespace Server; 

public partial class Landscape {
    private void OnDrawMapPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnDrawMapPacket");
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var cell = GetMapCell(x, y);
        if (cell == null) return;

        cell.Altitude = buffer.ReadSByte();
        cell.TileId = buffer.ReadUInt16();

        var packet = new DrawMapPacket(cell);
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnInsertStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnInsertStaticPacket");
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;

        var staticItem = new StaticItem();
        staticItem.X = x;
        staticItem.Y = y;
        staticItem.Z = buffer.ReadSByte();
        staticItem.TileId = buffer.ReadUInt16();
        staticItem.Hue = buffer.ReadUInt16();
        var targetStaticList = block.Cells[GetCellId(x,y)];
        targetStaticList.Add(staticItem);
        SortStaticList(targetStaticList);
        staticItem.Owner = block;

        var packet = new InsertStaticPacket(staticItem);
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnDeleteStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnDeleteStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;

        var statics = block.Cells[GetCellId(x,y)];
        for (var i = 0; i < statics.Count; i++) {
            var staticItem = statics[i];
            if (staticItem.Z != staticInfo.Z || 
                staticItem.TileId != staticInfo.TileId ||
                staticItem.Hue != staticInfo.Hue) continue;

            var packet = new DeleteStaticPacket(staticItem);
            
            staticItem.Delete();
            statics.RemoveAt(i);
            
            var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
            foreach (var netState in subscriptions) {
                CEDServer.SendPacket(netState, packet);
            }

            UpdateRadar(x, y);

            break;
        }
    }

    private void OnElevateStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnElevateStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;
        
        var statics = block.Cells[GetCellId(x,y)];
        
        var staticItem = statics.Find(s => s.Z == staticInfo.Z && s.TileId == staticInfo.TileId && s.Hue == staticInfo.Hue);
        if (staticItem == null) return;
       
        var newZ = buffer.ReadSByte();
        var packet = new ElevateStaticPacket(staticItem, newZ);
        staticItem.Z = newZ;
        SortStaticList(statics);
        
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnMoveStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnMoveStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var newX = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellWidth - 1);
        var newY = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellHeight - 1);
        
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y)) return;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, newX, newY)) return;

        if (staticInfo.X == newX && staticInfo.Y == newY) return;
        
        if((Math.Abs(staticInfo.X - newX) > 8 || Math.Abs(staticInfo.Y - newY) > 8) && 
           !PacketHandlers.ValidateAccess(ns, AccessLevel.Administrator)) return;

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        if (sourceBlock == null || targetBlock == null) return;

        var statics = sourceBlock.Cells[GetCellId(staticInfo.X, staticInfo.Y)];
        int i;
        StaticItem? staticItem = null;
        for(i = 0;i < statics.Count; i++) {
            if (statics[i].Z != staticInfo.Z || 
                statics[i].TileId != staticInfo.TileId ||
                statics[i].Hue != staticInfo.Hue) continue;
            staticItem = statics[i];
            break;
        }

        if (staticItem == null) return;
        var deletePacket = new DeleteStaticPacket(staticItem);
        var movePacket = new MoveStaticPacket(staticItem, newX, newY);

        i = statics.IndexOf(staticItem);
        statics.RemoveAt(i);

        statics = targetBlock.Cells[GetCellId(newX, newY)];
        statics.Add(staticItem);
        staticItem.UpdatePos(newX, newY, staticItem.Z);
        staticItem.Owner = targetBlock;

        var insertPacket = new InsertStaticPacket(staticItem);
        
        SortStaticList(statics);
        
        var sourceSubscriptions = _blockSubscriptions[GetSubBlockId(staticInfo.X, staticInfo.Y)];
        var targetSubscriptions = _blockSubscriptions[GetSubBlockId(newX, newY)];
        
        foreach (var netState in sourceSubscriptions) {
            if(targetSubscriptions.Contains(netState))
                CEDServer.SendPacket(netState, movePacket);
            else {
                CEDServer.SendPacket(netState, deletePacket);
            }
        }

        foreach (var netState in sourceSubscriptions) {
            CEDServer.SendPacket(netState, insertPacket);
        }

        UpdateRadar(staticInfo.X, staticInfo.Y);
        UpdateRadar(newX, newY);
    }

    private void OnHueStaticPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnHueStaticPacket");
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;
        
        var statics = block.Cells[GetCellId(x, y)];
        
        var staticItem = statics.Find(s => s.Z == staticInfo.Z && s.TileId == staticInfo.TileId && s.Hue == staticInfo.Hue);
        if (staticItem == null) return;
       
        var newHue = buffer.ReadUInt16();
        var packet = new HueStaticPacket(staticItem, newHue);
        staticItem.Hue = newHue;
        
        var subscriptions = _blockSubscriptions[GetSubBlockId(x, y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }
    }

    private void OnLargeScaleCommandPacket(BinaryReader buffer, NetState ns) {
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Administrator)) return;
        var logMsg = $"{ns.Account.Name} begins large scale operation";
        ns.LogInfo(logMsg);
        CEDServer.SendPacket(null, new ConnectionHandling.ServerStatePacket(ServerState.Other, logMsg));
        
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

        var areaCount = buffer.ReadByte();
        var areaInfo = new AreaInfo[areaCount];
        for (int i = 0; i < areaCount; i++) {
            areaInfo[i].Left = Math.Max(buffer.ReadUInt16(), (ushort)0);
            areaInfo[i].Top = Math.Max(buffer.ReadUInt16(), (ushort)0);
            areaInfo[i].Right = Math.Max(buffer.ReadUInt16(), (ushort)(CellWidth -1));
            areaInfo[i].Top = Math.Max(buffer.ReadUInt16(), (ushort)(CellHeight -1));
            for (ushort x = areaInfo[i].Left; x < areaInfo[i].Right; x++) {
                for (ushort y = areaInfo[i].Top; y < areaInfo[i].Bottom; y++) {
                    var blockId = GetBlockId(x, y);
                    var cellId = GetCellId(x, y);
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

        if (buffer.ReadBoolean()) {
            cmOperation = new LsCopyMove(buffer, this);
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
        if(buffer.ReadBoolean()) operations.Add(new LsSetAltitude(buffer, this));
        if(buffer.ReadBoolean()) operations.Add(new LsDrawTerrain(buffer, this));
        if(buffer.ReadBoolean()) operations.Add(new LsDeleteStatics(buffer, this));
        if(buffer.ReadBoolean()) operations.Add(new LsInsertStatics(buffer, this));
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
                        var mapTile = GetMapCell(x, y);
                        var statics = GetStaticList(x, y);
                        foreach (var operation in operations) {
                            operation.Apply(mapTile, statics, ref additionalAffectedBlocks);
                        }
                        SortStaticList(statics);
                        UpdateRadar(x, y);
                    }
                }
                
                //Notify affected clients
                foreach (var netState in _blockSubscriptions[realBlockY * Width + realBlockX]) {
                    clients[netState].Add(new BlockCoords(realBlockX, realBlockY));
                }
            }
        }
        
        //aditional blocks
        for (ushort blockX = 0; blockX < Width; blockX++) {
            for (ushort blockY = 0; blockY < Height; blockY++) {
                var blockId = (ushort)(blockX * Height + blockY);
                if (bitMask[blockId] != 0 || !additionalAffectedBlocks[blockId]) continue;

                foreach (var netState in _blockSubscriptions[blockY * Width + blockX]) {
                    clients[netState].Add(new BlockCoords(blockX, blockY));
                }
                
                UpdateRadar((ushort)(blockX * 8), (ushort)(blockY * 8));
            }
        }
        _radarMap.EndUpdate();
        
        foreach (var (netState, blocks) in clients) {
            if (blocks.Count > 0) {
                CEDServer.SendPacket(netState, new CompressedPacket(new BlockPacket(blocks, null)));
                netState.LastAction = DateTime.Now;
            }
        }
        CEDServer.SendPacket(null, new ConnectionHandling.ServerStatePacket(ServerState.Running));
        ns.LogInfo("Large scale operation ended.");
    }
}