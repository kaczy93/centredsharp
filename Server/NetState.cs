using System.Net.Sockets;
using System.Text;
using Cedserver;

namespace Server; 

public class NetState {
    public TcpClient TcpClient { get; }
    public MemoryStream ReceiveStream { get; }
    public BinaryReader Reader { get; }
    public Account Account { get; set; }
    public DateTime LastAction { get; set; }
    public bool LoggedIn { get; }

    public NetState(TcpClient tcpClient) {
        TcpClient = tcpClient;
        ReceiveStream = new MemoryStream(4096);
        Reader = new BinaryReader(ReceiveStream, Encoding.UTF8);
        Account = null!; //Account will be null only when something goes wrong during login
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
        Console.WriteLine($"[{level}] {DateTime.Now}@{TcpClient?.Client?.RemoteEndPoint?.ToString() ?? ""} {log}");
    }
}