using System.IO.Compression;
using CentrED.Utility;
using static CentrED.Client.PacketHandler;

namespace CentrED.Client;

public static class PacketHandlers {
    private static PacketHandler?[] Handlers { get; }

    static PacketHandlers() {
        Handlers = new PacketHandler?[0x100];
        RegisterPacketHandler(0x01, 0, OnCompressedPacket);
        RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        // RegisterPacketHandler(0x03, 0, AdminHandling.OnAdminHandlerPacket);
        RegisterPacketHandler(0x04, 0, OnRequestBlocksPacket);
        RegisterPacketHandler(0x05, 5, OnFreeBlockPacket);
        //0x06-0x0B handled by landscape 
        RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        //0x0D handled by radarmap
        //0x0E handled by landscape 
        RegisterPacketHandler(0xFF, 1, OnNoOpPacket);
    }

    public static void RegisterPacketHandler(int packetId, uint length, PacketProcessor packetProcessor) {
        Handlers[packetId] = new PacketHandler(length, packetProcessor);
    }
    
    private static void OnCompressedPacket(BinaryReader buffer, CentrEDClient c) {
        c.LogDebug("OnCompressedPacket");
        var targetSize = (int)buffer.ReadUInt32();
        var uncompBuffer = new GZipStream(buffer.BaseStream, CompressionMode.Decompress);
        var uncompStream = new MemoryStream();
        uncompBuffer.CopyBytesTo(uncompStream, targetSize);
        uncompStream.Position = 0;
        var packetId = uncompStream.ReadByte();
        var handler = Handlers[packetId];
        if (handler != null) {
            if (handler.Length == 0) 
                uncompStream.Position += 4;
            handler.OnReceive(new BinaryReader(uncompStream), c);
        }
        else {
            c.LogError($"Dropping client due to unknown packet: {packetId}");
            c.Dispose();
        }
    }
}