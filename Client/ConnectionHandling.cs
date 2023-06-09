using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public class ConnectionHandling {
    private static PacketHandler<CentrEDClient>?[] Handlers { get; }

    static ConnectionHandling() {
        Handlers = new PacketHandler<CentrEDClient>?[0x100];

        Handlers[0x01] = new PacketHandler<CentrEDClient>(0, OnProtocolVersionPacket);
        Handlers[0x03] = new PacketHandler<CentrEDClient>(0, OnLoginResponsePacket);
        Handlers[0x04] = new PacketHandler<CentrEDClient>(0, OnServerStatePacket);
    }

    public static void OnConnectionHandlerPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        Logger.LogDebug("OnConnectionHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = Handlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnProtocolVersionPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        Logger.LogDebug("OnProtocolVersionPacket");
        var version = reader.ReadUInt32();
        ns.ProtocolVersion = (ProtocolVersion)version switch {
            ProtocolVersion.CentrED => ProtocolVersion.CentrED,
            ProtocolVersion.CentrEDPlus => ProtocolVersion.CentrEDPlus,
            _ => throw new ArgumentException($"Unsupported protocol version {version}")
        };
    }

    private static void OnLoginResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("OnLoginResponsePacket");
        var loginState = (LoginState)reader.ReadByte();
        switch (loginState) {
            case LoginState.Ok:
                ns.LogInfo("Initializing");
                ns.Parent.AccessLevel = (AccessLevel)reader.ReadByte();
                if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus) {
                    reader.ReadUInt32(); //server uptime
                }
                var width = reader.ReadUInt16();
                var height = reader.ReadUInt16();
                if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus) {
                    reader.ReadUInt32(); //flags
                }

                ns.Parent.InitLandscape(width, height);
                ClientHandling.ReadAccountRestrictions(reader);
                break;
            case LoginState.InvalidUser:
                ns.LogError("The username you specified is incorrect.");
                ns.Disconnect();
                break;
            case LoginState.InvalidPassword:
                ns.LogError("The password you specified is incorrect.");
                ns.Disconnect();
                break;
            case LoginState.AlreadyLoggedIn:
                ns.LogError("There is already a client logged in using that account.");
                ns.Disconnect();
                break;
            case LoginState.NoAccess:
                ns.LogError("This account has no access.");
                ns.Disconnect();
                break;
            default:
                throw new ArgumentException($"Unknown login state{loginState}");
        }
    }

    private static void OnServerStatePacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        throw new NotImplementedException();
    }
}