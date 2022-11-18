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
}
