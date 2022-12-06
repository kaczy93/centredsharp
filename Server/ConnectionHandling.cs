using Server;
using Shared;

namespace Cedserver;

public class ConnectionHandling {
    public static PacketHandler[] ConnectionHandlers { get; }

    static ConnectionHandling() {
        ConnectionHandlers = new PacketHandler[0x100];

        ConnectionHandlers[0x03] = new PacketHandler(0, OnLoginRequestPacket);
        ConnectionHandlers[0x05] = new PacketHandler(0, OnQuitPacket);
    }

    public class ProtocolVersionPacket : Packet {
        public ProtocolVersionPacket(uint version) : base(0x02, 0) {
            Stream.Write((byte)0x01);
            Stream.Write(version);    
        }
    }

    public class LoginResponsePacket : Packet {
        public LoginResponsePacket(LoginState state, Account account = null) : base(0x02, 0) {
            Stream.Write((byte)0x01);
            Stream.Write((byte)state);
            if (state == LoginState.Ok) {
                Stream.Write((byte)account.AccessLevel);
                Stream.Write(Config.Map.Width);
                Stream.Write(Config.Map.Height);
                ClientHandling.WriteAccountRestrictions(Stream, account);
            }
        }
    }

    public class ServerStatePacket : Packet {
        public ServerStatePacket(ServerState state, string message = "") : base(0x02, 0) {
            Stream.Write((byte)0x01);
            Stream.Write((byte)state);
            if(state == ServerState.Other)
                Stream.WriteStringNull(message); 
        }
    }

    public static void OnConnectionHandlerPacket(BinaryReader reader, NetState ns) {
        var packetHandler = ConnectionHandlers[reader.ReadByte()];
        if (packetHandler != null) {
            packetHandler.OnReceive(reader, ns);
        }
    }

    public static void OnLoginRequestPacket(BinaryReader reader, NetState ns) {
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var account = Config.Accounts.Find(a => a.Name == username);
        if (account == null) {
            Console.WriteLine($"[{DateTime.Now}] Invalid account specified {ns.TcpClient.Client.RemoteEndPoint}");
            CEDServer.SendPacket(ns, new LoginResponsePacket(LoginState.InvalidUser));
            CEDServer.Disconnect(ns);
        }
        else if (account.AccessLevel <= AccessLevel.None) {
            Console.WriteLine($"[{DateTime.Now}] Access denied for {ns.TcpClient.Client.RemoteEndPoint}");
            CEDServer.SendPacket(ns, new LoginResponsePacket(LoginState.NoAccess));
            CEDServer.Disconnect(ns);
        }
        else if (!account.CheckPassword(password)) {
            Console.WriteLine($"[{DateTime.Now}] Invalid password for {ns.TcpClient.Client.RemoteEndPoint}");
            CEDServer.SendPacket(ns, new LoginResponsePacket(LoginState.InvalidPassword));
            CEDServer.Disconnect(ns);
        }
        else if (CEDServer.Clients.Any(client => client.Account == account)) {
            CEDServer.SendPacket(ns, new LoginResponsePacket(LoginState.AlreadyLoggedIn));
            CEDServer.Disconnect(ns);
        }
        else {
            Console.WriteLine($"[{DateTime.Now}] Login ({username}): {ns.TcpClient.Client.RemoteEndPoint}");
            ns.Account = account;
            CEDServer.SendPacket(ns, new LoginResponsePacket(LoginState.Ok, account));
            CEDServer.SendPacket(ns, new CompressedPacket(new ClientHandling.ClientListPacket(ns)));
            CEDServer.SendPacket(null, new ClientHandling.ClientConnectedPacket(username));
            CEDServer.SendPacket(ns, new ClientHandling.SetClientPosPacket(account.LastPos));
        }
    }

    public static void OnQuitPacket(BinaryReader reader, NetState ns) {
        CEDServer.Disconnect(ns);
    }
}