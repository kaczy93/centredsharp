//Server/UCEDServer.pas

using System.Net.Sockets;
using Cedserver;

namespace Server; 

//TCedServer
public class CEDServer {
    public static Landscape Landscape { get; }
    public static TcpListener TCPServer { get; }
    public static List<NetState> Clients { get; }
    public static bool Quit { get; set; }

    private static DateTime _lastFlush;
    private static bool _valid;

    public static void Init() {
        throw new NotImplementedException();
    }

    public static void Run() {
        throw new NotImplementedException();
    }

    public static void Stop() {
        throw new NotImplementedException();
    }

    public static void Disconnect(NetState ns) {
        throw new NotImplementedException();
    }

    public static void SendPacket(NetState? ns, Packet packet) {
        throw new NotImplementedException();
    }
}
