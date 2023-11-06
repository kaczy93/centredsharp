using CentrED.Network;
using static CentrED.Network.PacketHandlers;

namespace CentrED.Client;

public static class PacketHandlers {
    internal static PacketHandler<CentrEDClient>?[] Handlers { get; }

    static PacketHandlers() {
        Handlers = new PacketHandler<CentrEDClient>?[0x100];
        RegisterPacketHandler(0x01, 0, OnCompressedPacket);
        RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        RegisterPacketHandler(0x0D, 0, RadarMap.OnRadarHandlerPacket);
    }

    public static void RegisterPacketHandler(int packetId, uint length, PacketHandler<CentrEDClient>.PacketProcessor packetProcessor) {
        Handlers[packetId] = new PacketHandler<CentrEDClient>(length, packetProcessor);
    }
}