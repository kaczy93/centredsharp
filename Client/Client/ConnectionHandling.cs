namespace CentrED.Client;

public class ConnectionHandling {
    private static PacketHandler?[] ConnectionHandlers { get; }

    static ConnectionHandling() {
        ConnectionHandlers = new PacketHandler?[0x100];

        ConnectionHandlers[0x01] = new PacketHandler(0, OnProtocolVersionPacket);
        ConnectionHandlers[0x03] = new PacketHandler(0, OnLoginResponsePacket);
        ConnectionHandlers[0x05] = new PacketHandler(0, OnQuitPacket);
    }

    public static void OnConnectionHandlerPacket(BinaryReader reader, CentrEDClient c) {
        c.LogDebug("OnConnectionHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = ConnectionHandlers[id];
        packetHandler?.OnReceive(reader, c);
    }

    private static void OnProtocolVersionPacket(BinaryReader reader, CentrEDClient c) {
        c.LogDebug("OnProtocolVersionPacket");
        var version = reader.ReadUInt32();
        c.CentrEdPlus = version switch {
            6 => false,
            0x1000 + 8 => true,
            _ => throw new ArgumentException($"Unsupported protocol version {version}")
        };
        c.Send(new LoginRequestPacket(c.Username, c.Password))
    }
}