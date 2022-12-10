using System.Net;
using System.Net.Sockets;
using Cedserver;

namespace Server; 

public class NetState {
    public TcpClient TcpClient { get; }
    public MemoryStream ReceiveStream { get; private set;}
    public MemoryStream SendStream { get; }
    public Account? Account { get; set; }
    public DateTime LastAction { get; set; }
    public EndPoint? Address => TcpClient.Client.RemoteEndPoint;

    public NetState(TcpClient tcpClient) {
        TcpClient = tcpClient;
        ReceiveStream = new MemoryStream();
        SendStream = new MemoryStream();
        Account = null;
        LastAction = DateTime.Now;
    }
    
    public void LogInfo(string log) {
        Log("INFO", log);
    }

    public void LogError(string log) {
        Log("ERROR", log);
    }

    public void LogDebug(string log) {
        if (CEDServer.DEBUG) Log("DEBUG", log);
    }

    private void Log(string level, string log) {
        Console.WriteLine($"[{level}] {DateTime.Now}@{Address} {log}");
    }
}