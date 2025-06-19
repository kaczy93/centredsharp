using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CentrED.Network;

namespace CentrED.Server.Map;

public partial class ServerLandscape
{
    private static readonly Random Random = new();
    private void OnDrawMapPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnDrawMapPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y))
            return;

        var tile = GetLandTile(x, y);

        var newZ = reader.ReadSByte();
        InternalSetLandZ(tile, newZ);
        var newId = reader.ReadUInt16();
        AssertLandTileId(newId);
        InternalSetLandId(tile, newId);

        LandBlock block = tile.Block!;
        var packet = new DrawMapPacket(tile);
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y))
        {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnInsertStaticPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnInsertStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y))
            return;

        var block = GetStaticBlock(staticInfo);

        var tile = new StaticTile(staticInfo);
        AssertStaticTileId(tile.Id);
        AssertHue(tile.Hue);
        InternalAddStatic(block, tile);

        block.SortTiles(ref TileDataProvider.StaticTiles);

        var packet = new InsertStaticPacket(tile);
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y))
        {
            netState.Send(packet);
        }

        UpdateRadar(ns, staticInfo.X, staticInfo.Y);
    }

    private void OnDeleteStaticPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnDeleteStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y))
            return;

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        if (tile == null)
            return;

        InternalRemoveStatic(block, tile);

        var packet = new DeleteStaticPacket(tile);
        foreach (var netState in GetBlockSubscriptions(block.X, block.Y))
        {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnElevateStaticPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnElevateStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y))
            return;

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        if (tile == null)
            return;

        var newZ = reader.ReadSByte();
        var packet = new ElevateStaticPacket(tile, newZ);
        InternalSetStaticZ(tile, newZ);
        block.SortTiles(ref TileDataProvider.StaticTiles);

        foreach (var netState in GetBlockSubscriptions(block.X, block.Y))
        {
            netState.Send(packet);
        }

        UpdateRadar(ns, x, y);
    }

    private void OnMoveStaticPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnMoveStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var newX = (ushort)Math.Clamp(reader.ReadUInt16(), 0, WidthInTiles - 1);
        var newY = (ushort)Math.Clamp(reader.ReadUInt16(), 0, HeightInTiles - 1);

        if (staticInfo.X == newX && staticInfo.Y == newY)
            return;

        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y))
            return;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, newX, newY))
            return;

        if ((Math.Abs(staticInfo.X - newX) > 8 || Math.Abs(staticInfo.Y - newY) > 8) &&
            !PacketHandlers.ValidateAccess(ns, AccessLevel.Developer))
            return;

        var sourceBlock = GetStaticBlock(staticInfo);
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        
        var tile = sourceBlock.Find(staticInfo);
        if (tile == null)
        {
            ns.LogError($"Tile not found {staticInfo}");
            return;
        }

        var deletePacket = new DeleteStaticPacket(tile);
        var movePacket = new MoveStaticPacket(tile, newX, newY);

        ns.LogDebug($"Moving {tile} to {newX},{newY}");
        InternalRemoveStatic(sourceBlock, tile);
        InternalSetStaticPos(tile, newX, newY);
        InternalAddStatic(targetBlock, tile);

        var insertPacket = new InsertStaticPacket(tile);

        targetBlock.SortTiles(ref TileDataProvider.StaticTiles);

        var sourceSubscriptions = GetBlockSubscriptions(sourceBlock.X, sourceBlock.Y);
        var targetSubscriptions = GetBlockSubscriptions(targetBlock.X, targetBlock.Y);

        var moveSubscriptions = sourceSubscriptions.Intersect(targetSubscriptions);
        var deleteSubscriptions = sourceSubscriptions.Except(targetSubscriptions);
        var insertSubscriptions = targetSubscriptions.Except(sourceSubscriptions);

        foreach (var netState in insertSubscriptions)
        {
            netState.Send(insertPacket);
        }
        foreach (var netState in deleteSubscriptions)
        {
            netState.Send(deletePacket);
        }
        foreach (var netState in moveSubscriptions)
        {
            netState.Send(movePacket);
        }

        UpdateRadar(ns, staticInfo.X, staticInfo.Y);
        UpdateRadar(ns, newX, newY);
    }

    private void OnHueStaticPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnHueStaticPacket");
        var staticInfo = reader.ReadStaticInfo();
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y))
            return;

        var block = GetStaticBlock(staticInfo);
        var tile = block.Find(staticInfo);
        var newHue = reader.ReadUInt16();
        if (tile == null)
            return;
        AssertHue(newHue);
        var packet = new HueStaticPacket(tile, newHue);
        InternalSetStaticHue(tile, newHue);

        foreach (var netState in GetBlockSubscriptions(block.X, block.Y))
        {
            netState.Send(packet);
        }
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private void OnLargeScaleCommandPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Developer))
            return;
        var logMsg = $"{ns.Username} begins large scale operation";
        ns.LogInfo(logMsg);
        ns.Parent.Send(new ServerStatePacket(ServerState.Other, logMsg));
        ns.Parent.Flush();
        try
        {
            var affectedBlocks = new bool[Width * Height];
            var affectedTiles = new bool[Width * Height, 64];
            var extraAffectedBlocks = new List<(ushort, ushort)>();

            var clients = new Dictionary<NetState<CEDServer>, HashSet<BlockCoords>>();
            foreach (var netState in ns.Parent.Clients)
            {
                clients.Add(netState, new HashSet<BlockCoords>());
            }

            var areaCount = reader.ReadByte();
            var areaInfos = new AreaInfo[areaCount];
            for (int i = 0; i < areaCount; i++)
            {
                areaInfos[i] = reader.ReadAreaInfo();
                for (ushort x = areaInfos[i].Left; x <= areaInfos[i].Right; x++)
                {
                    for (ushort y = areaInfos[i].Top; y <= areaInfos[i].Bottom; y++)
                    {
                        var blockId = TileBlockIndex(x, y);
                        var tileId = LandBlock.GetTileIndex(x, y);
                        affectedBlocks[blockId] = true;
                        affectedTiles[blockId, tileId] = true;
                    }
                }
            }

            var minBlockX = Math.Max(0, areaInfos.Min(ai => ai.Left) / 8);
            var maxBlockX = Math.Min(Width, areaInfos.Max(ai => ai.Right) / 8 + 1);
            var minBlockY = Math.Max(0, areaInfos.Min(ai => ai.Top) / 8);
            var maxBlockY = Math.Min(Height, areaInfos.Max(ai => ai.Bottom) / 8 + 1);

            List<LargeScaleOperation> operations = new();

            var xBlockRange = Enumerable.Range(minBlockX, maxBlockX - minBlockX);
            var yBlockRange = Enumerable.Range(minBlockY, maxBlockY - minBlockY);
            var xTileRange = Enumerable.Range(0, 8);
            var yTileRange = Enumerable.Range(0, 8);

            if (reader.ReadBoolean())
            {
                var copyMove = new LsCopyMove(ref reader);
                if (copyMove.OffsetX > 0)
                {
                    xBlockRange = xBlockRange.Reverse();
                    xTileRange = xTileRange.Reverse();
                }
                if (copyMove.OffsetY > 0)
                {
                    yBlockRange = yBlockRange.Reverse();
                    yTileRange = yTileRange.Reverse();
                }
                operations.Add(copyMove);
            }

            if (reader.ReadBoolean())
                operations.Add(new LsSetAltitude(ref reader));
            if (reader.ReadBoolean())
                operations.Add(new LsDrawTerrain(ref reader));
            if (reader.ReadBoolean())
                operations.Add(new LsDeleteStatics(ref reader));
            if (reader.ReadBoolean())
                operations.Add(new LsInsertStatics(ref reader));
            //We have read everything, now we can validate
            foreach (var operation in operations)
            {
                operation.Validate(this);
            }

            _radarMap.BeginUpdate();
            foreach (ushort blockX in xBlockRange)
            {
                foreach (ushort blockY in yBlockRange)
                {
                    var blockId = BlockIndex(blockX, blockY);
                    if (!affectedBlocks[blockId])
                        continue;

                    foreach (ushort tileY in yTileRange)
                    {
                        foreach (ushort tileX in xTileRange)
                        {
                            var tileIndex = LandBlock.GetTileIndex(tileX, tileY);
                            if (!affectedTiles[blockId, tileIndex])
                                continue;

                            var x = (ushort)(blockX * 8 + tileX);
                            var y = (ushort)(blockY * 8 + tileY);
                            var mapTile = GetLandTile(x, y);
                            var staticBlock = GetStaticBlock(blockX, blockY);
                            var statics = staticBlock.GetTiles(x, y);
                            foreach (var operation in operations)
                            {
                                LsoApply(operation, mapTile, statics.ToArray(), extraAffectedBlocks);
                            }

                            staticBlock.SortTiles(ref TileDataProvider.StaticTiles);
                            UpdateRadar(ns, x, y);
                        }
                    }

                    //Notify affected clients
                    foreach (var netState in GetBlockSubscriptions(blockX, blockY))
                    {
                        clients[netState].Add(new BlockCoords(blockX, blockY));
                    }
                }
            }

            foreach (var (blockX, blockY) in extraAffectedBlocks)
            {
                var blockId = BlockIndex(blockX, blockY);
                if(affectedBlocks[blockId])
                    continue;
                
                foreach (var netState in GetBlockSubscriptions(blockX, blockY)!)
                {
                    clients[netState].Add(new BlockCoords(blockX, blockY));
                }

                UpdateRadar(ns, (ushort)(blockX * 8), (ushort)(blockY * 8));
            }
            

            foreach (var (netState, blocks) in clients)
            {
                if (blocks.Count > 0)
                {
                    netState.SendCompressed(new BlockPacket(blocks, netState, false));
                }
            }
        }
        catch (Exception e)
        {
            ns.LogError($"LSO Failed {e}");
        }
        finally
        {
            _radarMap.EndUpdate(ns);
            ns.Parent.Send(new ServerStatePacket(ServerState.Running));
        }
        ns.LogInfo("Large scale operation ended.");
    }

    //It's extremely ugly, but I will get rid of lso once I implement scripting in client
    private void LsoApply(LargeScaleOperation lso, LandTile landTile, IEnumerable<StaticTile> staticTiles, List<(ushort, ushort)> additionalAffectedBlocks)
    {
        if (lso is LsCopyMove copyMove)
        {
            ushort x = (ushort)Math.Clamp(landTile.X + copyMove.OffsetX, 0, WidthInTiles - 1);
            ushort y = (ushort)Math.Clamp(landTile.Y + copyMove.OffsetY, 0, HeightInTiles - 1);
            var targetLandTile = GetLandTile(x, y);
            var targetStaticsBlock = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
            if (copyMove.Erase)
            {
                foreach (var targetStatic in GetStaticTiles(x, y).ToArray())
                {
                    InternalRemoveStatic(targetStaticsBlock, targetStatic);
                }
            }

            InternalSetLandId(targetLandTile, landTile.Id);
            InternalSetLandZ(targetLandTile, landTile.Z);

            switch (copyMove.Type)
            {
                case LSO.CopyMove.Copy:
                {
                    foreach (var staticTile in staticTiles)
                    {
                        InternalAddStatic(targetStaticsBlock, new StaticTile(staticTile.Id, x, y, staticTile.Z, staticTile.Hue));
                    }
                    break;
                }
                case LSO.CopyMove.Move:
                {
                    foreach (var staticTile in staticTiles)
                    {
                        InternalRemoveStatic(staticTile.Block!, staticTile);
                        InternalSetStaticPos(staticTile, x, y);
                        InternalAddStatic(targetStaticsBlock, staticTile);
                    }
                    break;
                }
            }
            additionalAffectedBlocks.Add(((ushort)(x / 8), (ushort)(y/ 8)));
        }
        else if (lso is LsSetAltitude setAltitude)
        {
            var minZ = setAltitude.MinZ;
            var maxZ = setAltitude.MaxZ;
            sbyte diff = 0;
            switch (setAltitude.Type)
            {
                case LSO.SetAltitude.Terrain:
                {
                    var newZ = (sbyte)(minZ + Random.Next(maxZ - minZ + 1));
                    diff = (sbyte)(newZ - landTile.Z);
                    InternalSetLandZ(landTile, newZ);
                    break;
                }
                case LSO.SetAltitude.Relative:
                {
                    diff = setAltitude.RelativeZ;
                    InternalSetLandZ(landTile, (sbyte)Math.Clamp(landTile.Z + diff, -128, 127));
                    break;
                }
            }

            foreach (var staticTile in staticTiles)
            {
                InternalSetStaticZ(staticTile, (sbyte)Math.Clamp(staticTile.Z + diff, -128, 127));
            }
        }
        else if (lso is LsDrawTerrain drawTerrain)
        {
            var tileIds = drawTerrain.TileIds;
            if (tileIds.Length <= 0)
                return;

            InternalSetLandId(landTile, tileIds[Random.Next(tileIds.Length)]);
        }
        else if (lso is LsDeleteStatics deleteStatics)
        {
            var staticBlock = GetStaticBlock((ushort)(landTile.X / 8), (ushort)(landTile.Y / 8));
            foreach (var staticTile in staticTiles)
            {
                if (staticTile.Z < deleteStatics.MinZ || staticTile.Z > deleteStatics.MaxZ)
                    continue;

                if (deleteStatics.TileIds.Length > 0)
                {
                    if (deleteStatics.TileIds.Contains(staticTile.Id))
                    {
                        InternalRemoveStatic(staticBlock, staticTile);
                    }
                }
                else
                {
                    InternalRemoveStatic(staticBlock, staticTile);
                }
            }
        }
        else if (lso is LsInsertStatics addStatics)
        {
            if (addStatics.TileIds.Length == 0 || Random.Next(100) >= addStatics.Probability)
                return;

            var staticItem = new StaticTile(addStatics.TileIds[Random.Next(addStatics.TileIds.Length)], landTile.X, landTile.Y, 0, 0);
            switch (addStatics.PlacementType)
            {
                case LSO.StaticsPlacement.Terrain:
                {
                    InternalSetStaticZ(staticItem, landTile.Z);
                    break;
                }
                case LSO.StaticsPlacement.Top:
                {
                    var topZ = landTile.Z;
                    foreach (var staticTile in staticTiles)
                    {
                        sbyte staticTop = Math.Clamp
                        (
                            (sbyte)(staticTile.Z + TileDataProvider.StaticTiles[staticTile.Id].Height),
                            (sbyte)-128,
                            (sbyte)127
                        );
                        if (staticTop > topZ)
                            topZ = staticTop;
                    }
                    InternalSetStaticZ(staticItem, topZ);
                    break;
                }
                case LSO.StaticsPlacement.Fix:
                {
                    InternalSetStaticZ(staticItem, addStatics.FixedZ);
                    break;
                }
            }
            var staticBlock = GetStaticBlock((ushort)(staticItem.X / 8), (ushort)(staticItem.Y / 8));
            InternalAddStatic(staticBlock, staticItem);
        }
    }
}