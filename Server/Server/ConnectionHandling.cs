using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Server;

public class ConnectionHandling {
    private static PacketHandler<CEDServer>?[] ConnectionHandlers { get; }

    static ConnectionHandling() {
        ConnectionHandlers = new PacketHandler<CEDServer>?[0x100];

        ConnectionHandlers[0x03] = new PacketHandler<CEDServer>(0, OnLoginRequestPacket);
        ConnectionHandlers[0x05] = new PacketHandler<CEDServer>(0, OnQuitPacket);
    }
    public static void OnConnectionHandlerPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnConnectionHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = ConnectionHandlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnLoginRequestPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnLoginRequestPacket");
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var account = ns.Parent.GetAccount(username);
        if (account == null) {
            ns.LogDebug($"Invalid account specified: {username}");
            ns.Send(new LoginResponsePacket(LoginState.InvalidUser));
            ns.Dispose();
        }
        else if (account.AccessLevel <= AccessLevel.None) {
            ns.LogDebug("Access Denied");
            ns.Send(new LoginResponsePacket(LoginState.NoAccess));
            ns.Dispose();
        }
        else if (!account.CheckPassword(password)) {
            ns.LogDebug("Invalid password");
            ns.Send(new LoginResponsePacket(LoginState.InvalidPassword));
            ns.Dispose();
        }
        else if (ns.Parent.Clients.Any(client => client.Username == account.Name)) {
            ns.Send(new LoginResponsePacket(LoginState.AlreadyLoggedIn));
            ns.Dispose();
        }
        else {
            ns.LogInfo($"Login {username}");
            ns.Username = account.Name;
            ns.Send(new LoginResponsePacket(LoginState.Ok, ns));
            ns.Send(new CompressedPacket(new ClientListPacket(ns)));
            ns.Parent.Send(new ClientConnectedPacket(ns));
            ns.Send(new SetClientPosPacket(ns));
        }
    }

    private static void OnQuitPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("OnQuitPacket");
        ns.Dispose();
    }
}