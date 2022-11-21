using System.IO.Compression;
using Cedserver;
using Shared;
using static Server.PacketHandler;

namespace Server; 

public static class PacketHandlers {
    
    public static PacketHandler[] Handlers { get; }
    
    
    static PacketHandlers() {
        Handlers = new PacketHandler[0xFF];
        
        RegisterPacketHandler(0x01, 0, OnCompressedPacket);
        // RegisterPacketHandler(0x02, 0, OnConnectionHandlerPacket);
        // RegisterPacketHandler(0x03, 0, OnAdminHandlerPacket);
        RegisterPacketHandler(0x04, 0, OnRequestBlocksPacket);
        RegisterPacketHandler(0x05, 0, OnFreeBlockPacket);
        //
        // RegisterPacketHandler(0x0C, 0, OnClientHandlerPacket);
        RegisterPacketHandler(0xFF, 0, OnNoOpPacket);
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
        var targetSize = (int)buffer.ReadUInt32();
        var uncompBuffer = new GZipStream(buffer.BaseStream, CompressionMode.Decompress);
        var uncompStream = new MemoryStream();
        uncompBuffer.CopyTo(uncompStream, targetSize);
        uncompStream.Position = 0;
        var packetId = uncompStream.ReadByte();
        if (Handlers[packetId] != null) {
            if (Handlers[packetId].Length == 0) uncompStream.Position += 4;
            //uncompStream.Lock(uncompStream.Position, uncompStream.Length - uncompStream.Position) // There's no such functionality, do we really need to lock here?
            Handlers[packetId].OnReceive(new BinaryReader(uncompStream), ns);
            //uncompStream.Unlock()
        }
        else {
            Console.WriteLine($"[{DateTime.Now}] Dropping client due to unknown packet: {ns.Socket.RemoteEndPoint}");
            ns.ReceiveQueue.SetLength(0);
            CEDServer.Disconnect(ns);
        }
}

    private static void OnRequestBlocksPacket(BinaryReader buffer, NetState ns) {
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var blocksCount = (buffer.BaseStream.Length - buffer.BaseStream.Position) / 4; // x and y, both 2 bytes
        var blocks = new BlockCoords[blocksCount];
        for (int i = 0; i < blocksCount; i++) {
            blocks[i] = new BlockCoords(buffer);
        }

        CEDServer.SendPacket(ns, new CompressedPacket(new BlockPacket(blocks, ns)));
    }

    private static void OnFreeBlockPacket(BinaryReader buffer, NetState ns) {
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        var blockSubscriptions = CEDServer.Landscape.BlockSubscriptions(x, y);
        if (blockSubscriptions != null) {
            blockSubscriptions.Remove(ns);
            ns.Subscriptions.Remove(blockSubscriptions);
        }
    }

    private static void OnNoOpPacket(BinaryReader buffer, NetState netstate) {

    }
}