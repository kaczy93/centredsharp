using System.Collections;
using System.Net.Sockets;
using Cedserver;

namespace Server; 

public class NetState {
    public TcpClient TcpClient { get; }
    public MemoryStream ReceiveStream { get; private set;}
    public MemoryStream SendStream { get; }
    public Account? Account { get; set; }
    public DateTime LastAction { get; set; }

    public NetState(TcpClient tcpClient) {
        TcpClient = tcpClient;
        ReceiveStream = new MemoryStream();
        SendStream = new MemoryStream();
        Account = null;
        LastAction = DateTime.Now;
    }
    
    public void Dequeue(uint size) {
        var newStream = new MemoryStream();
        ReceiveStream.Seek(size, SeekOrigin.Begin);
        ReceiveStream.CopyTo(newStream);
        ReceiveStream = newStream;
        ReceiveStream.Position = 0;
    }
}