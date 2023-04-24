using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client; 

public class CentrEDClient {
    private NetState<CentrEDClient> NetState { get; }
    public bool CentrEdPlus { get; internal set; }
    public bool Initialized { get; internal set; }
    public string Username { get; }
    public string Password { get; }
    public AccessLevel AccessLevel { get; internal set; }
    public DateTime LastAction { get; private set; }
    public ushort Width { get; internal set; }
    public ushort Height { get; internal set; }
    public List<String> Clients { get; } = new();
    
    public CentrEDClient(string hostname, int port, string username, string password) {
        Username = username;
        Password = password;
        var ipAddress = Dns.GetHostAddresses(hostname)[0];
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipEndPoint);
        NetState = new NetState<CentrEDClient>(this, socket, PacketHandlers.Handlers);
        
        new Task(() => NetState.Receive()).Start();
        NetState.Send(new LoginRequestPacket(username, password));

        while (!Initialized) {
            Thread.Sleep(1);
        }
    }
    
    

    public void InitLandscape() {
        
        Initialized = true;
    }

    public void SetPos(ushort x, ushort y) {
        //nothing to do yet
    }

    public void ChatMessage(string sender, ushort message) {
        Logger.LogInfo($"{sender}: {message}");
    }
}