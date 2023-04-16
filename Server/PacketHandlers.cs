using System.IO.Compression;
using Cedserver;
using Shared;
using static Server.PacketHandler;

namespace Server; 

public static class PacketHandlers {
    
    public static PacketHandler[] Handlers { get; }
    
    static PacketHandlers() {
        Handlers = new PacketHandler[0x100];
        
        RegisterPacketHandler(0x01, 0, OnCompressedPacket);
        RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        RegisterPacketHandler(0x03, 0, AdminHandling.OnAdminHandlerPacket);
        RegisterPacketHandler(0x04, 0, OnRequestBlocksPacket);
        RegisterPacketHandler(0x05, 5, OnFreeBlockPacket);
        //0x06-0x0B handled by landscape 
        RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        //0x0D handled by radarmap
        //0x0E handled by landscape 
        RegisterPacketHandler(0xFF, 1, OnNoOpPacket);
    }

    public static void RegisterPacketHandler(int packetId, uint length, PacketProcessor packetProcessor)
    {
        Handlers[packetId] = new PacketHandler(length, packetProcessor);
    }

    public static bool ValidateAccess(NetState ns, AccessLevel accessLevel) {
        return ns.Account?.AccessLevel >= accessLevel;
    }

    public static bool ValidateAccess(NetState ns, AccessLevel accessLevel, uint x, uint y) {
        if (!ValidateAccess(ns, accessLevel)) return false;
        if (ns.Account.Regions.Count == 0 || ns.Account.AccessLevel >= AccessLevel.Administrator) return true;

        foreach (var regionName in ns.Account.Regions) {
            var region = Config.Regions.Find(r => r.Name == regionName);
            if(region != null) {
                if (region.Area.Any(a => a.Contains(x, y))) return true;
            }
        }
        return false;
    }

    private static void OnCompressedPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnCompressedPacket");
        var targetSize = (int)buffer.ReadUInt32();
        var uncompBuffer = new GZipStream(buffer.BaseStream, CompressionMode.Decompress);
        var uncompStream = new MemoryStream();
        uncompBuffer.CopyBytesTo(uncompStream, targetSize);
        uncompStream.Position = 0;
        var packetId = uncompStream.ReadByte();
        if (Handlers[packetId] != null) {
            if (Handlers[packetId].Length == 0) 
                uncompStream.Position += 4;
            Handlers[packetId].OnReceive(new BinaryReader(uncompStream), ns);
        }
        else {
            ns.LogError($"Dropping client due to unknown packet: {packetId}");
            CEDServer.Disconnect(ns);
        }
}

    private static void OnRequestBlocksPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnRequestBlocksPacket");
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var blocksCount = (buffer.BaseStream.Length - buffer.BaseStream.Position) / 4; // x and y, both 2 bytes
        var blocks = new BlockCoords[blocksCount];
        for (int i = 0; i < blocksCount; i++) {
            blocks[i] = new BlockCoords(buffer);
            ns.LogDebug($"Requested x={blocks[i].X} y={blocks[i].Y}");
        }

        CEDServer.SendPacket(ns, new CompressedPacket(new BlockPacket(new List<BlockCoords>(blocks), ns)));
    }

    private static void OnFreeBlockPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnFreeBlockPacket");
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        var blockSubscriptions = CEDServer.Landscape.GetBlockSubscriptions(x, y);
        blockSubscriptions?.Remove(ns);
    }

    private static void OnNoOpPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnNoOpPacket");
    }

    public static PacketHandler? GetHandler(byte index) {
        return Handlers[index];
    }
}