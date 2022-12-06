using System.Collections;
using System.Net.Sockets;
using Cedserver;

namespace Server; 

public class NetState {
    public TcpClient TcpClient { get; }
    public MemoryStream ReceiveStream { get; }
    public MemoryStream SendStream { get; }
    public Account? Account { get; set; }
    public ArrayList Subscriptions { get; } //TODO: Fill in correct element type
    public DateTime LastAction { get; set; }

    public byte[] ReadBuffer { get; private set; }

    private const int BufferSize = 4096;

    public NetState(TcpClient tcpClient) {
        TcpClient = tcpClient;
        ReceiveStream = new MemoryStream();
        SendStream = new MemoryStream();
        Account = null;
        Subscriptions = new ArrayList();
        LastAction = DateTime.Now;
    }

    public IAsyncResult BeginRead(AsyncCallback? callback) {
        ReadBuffer = new byte[BufferSize];
        return TcpClient.GetStream().BeginRead(ReadBuffer, 0, ReadBuffer.Length, callback, this);
    }

    public int EndRead(IAsyncResult ar) {
        return TcpClient.GetStream().EndRead(ar);
    }
}