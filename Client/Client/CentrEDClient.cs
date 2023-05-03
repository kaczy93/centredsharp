using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public sealed class CentrEDClient : IDisposable {
    private NetState<CentrEDClient> NetState { get; }
    private Landscape Landscape { get; set; }
    public bool CentrEdPlus { get; internal set; }
    public bool Initialized { get; internal set; }
    public string Username { get; }
    public string Password { get; }
    public AccessLevel AccessLevel { get; internal set; }
    public List<String> Clients { get; } = new();
    public bool Running = true;
    private Task netStateTask;


    public CentrEDClient(string hostname, int port, string username, string password) {
        Username = username;
        Password = password;
        var ipAddress = Dns.GetHostAddresses(hostname)[0];
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipEndPoint);
        NetState = new NetState<CentrEDClient>(this, socket, PacketHandlers.Handlers);

        netStateTask = new Task(() => RunLoop());
        netStateTask.Start();
        NetState.Send(new LoginRequestPacket(username, password));

        while (!Initialized) {
            Thread.Sleep(1);
        }
    }

    ~CentrEDClient() {
        Dispose(false);
    }
    
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing) {
        if (disposing) {
            Running = false;
            netStateTask.Wait();
            while (NetState.FlushPending)
                NetState.Flush();
            NetState.Dispose();
        }
    }

    public void RunLoop() {
        try {
            do {
                NetState.Receive();

                if (NetState.FlushPending) {
                    NetState.Flush();
                }

                Thread.Sleep(1);
            } while (Running);
        }
        catch {
            NetState.Dispose();
        }
    }

    public void InitLandscape(ushort width, ushort height) {
        Landscape = new Landscape(this, width, height);
        Initialized = true;
    }

    public void SetPos(ushort x, ushort y) {
        //nothing to do yet
    }

    public void ChatMessage(string sender, ushort message) {
        Logger.LogInfo($"{sender}: {message}");
    }
    
    public LandTile GetLandTile(ushort x, ushort y) {
        return Landscape.GetLandTile(x, y);
    }
    
    public void SetLandTile(LandTile tile) {
        NetState.Send(new DrawMapPacket(tile));
    }

    public ReadOnlyCollection<StaticTile> GetStaticTiles(ushort x, ushort y) {
        return Landscape.GetStaticTiles(x, y);
    }

    public void AddStaticTile(StaticTile tile) {
        NetState.Send(new InsertStaticPacket(tile));
    }

    public void RemoveStaticTile(StaticTile tile) {
        NetState.Send(new DeleteStaticPacket(tile));
    }
    
    internal void Send(Packet p) {
        NetState.Send(p);
    }
}