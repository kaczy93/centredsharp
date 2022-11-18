//Server/UCEDServer.pas

using System.Net.Sockets;

namespace Server; 

//TCedServer
public class CEDServer {
    public Landscape Landscape { get; }
    public TcpListener TCPServer { get; }
    public bool Quit { get; set; }

    private DateTime _lastFlush;
    private bool _valid;

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
}
