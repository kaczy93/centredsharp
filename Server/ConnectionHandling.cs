using Server;
using Shared;

namespace Cedserver;

public class ConnectionHandling {
    private static PacketHandler?[] ConnectionHandlers { get; }

    static ConnectionHandling() {
        ConnectionHandlers = new PacketHandler?[0x100];

        ConnectionHandlers[0x03] = new PacketHandler(0, OnLoginRequestPacket);
        ConnectionHandlers[0x05] = new PacketHandler(0, OnQuitPacket);
    }
    public static void OnConnectionHandlerPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnConnectionHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = ConnectionHandlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnLoginRequestPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnLoginRequestPacket");
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var account = Config.Accounts.Find(a => a.Name == username);
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
        else if (CEDServer.Clients.Any(client => client.Account == account)) {
            ns.Send(new LoginResponsePacket(LoginState.AlreadyLoggedIn));
            ns.Dispose();
        }
        else {
            ns.LogInfo($"Login {username}");
            ns.Account = account;
            ns.Send(new LoginResponsePacket(LoginState.Ok, account));
            ns.Send(new CompressedPacket(new ClientListPacket(ns)));
            CEDServer.Send(new ClientConnectedPacket(ns.Account));
            ns.Send(new SetClientPosPacket(account.LastPos));
        }
    }

    private static void OnQuitPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnQuitPacket");
        ns.Dispose();
    }
}