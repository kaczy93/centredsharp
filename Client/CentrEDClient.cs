using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public sealed class CentrEDClient : IDisposable {
    public event MapChanged? MapChanged;
    public event BlockChanged? BlockUnloaded;
    public event BlockChanged? BlockLoaded;
    public event LandChanged? LandTileChanged;
    public event StaticChanged? StaticTileAdded;
    public event StaticChanged? StaticTileRemoved;
    public event StaticChanged? StaticTileElevated;
    public event StaticChanged? StaticTileHued;
        
    private NetState<CentrEDClient> NetState { get; }
    private ClientLandscape _landscape { get; set; }
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
        _landscape = new ClientLandscape(this, width, height);
        Initialized = true;
        _landscape.MapChanged += () => MapChanged?.Invoke();
        _landscape.BlockLoaded += _ => BlockLoaded?.Invoke(_);
        _landscape.BlockUnloaded += _ => BlockUnloaded?.Invoke(_);
        _landscape.LandTileChanged += _ => LandTileChanged?.Invoke(_);
        _landscape.StaticTileAdded += _ => StaticTileAdded?.Invoke(_);
        _landscape.StaticTileRemoved += _ => StaticTileRemoved?.Invoke(_);
        _landscape.StaticTileElevated += _ => StaticTileElevated?.Invoke(_);
        _landscape.StaticTileHued += _ => StaticTileHued?.Invoke(_);
    }

    public void LoadBlocks(List<BlockCoords> blockCoords) {
        Send(new RequestBlocksPacket(blockCoords));
    }

    public void SetPos(ushort x, ushort y) {
        //nothing to do yet
    }

    public void ChatMessage(string sender, ushort message) {
        Logger.LogInfo($"{sender}: {message}");
    }
    
    public LandTile GetLandTile(ushort x, ushort y) {
        return _landscape.GetLandTile(x, y);
    }
    
    public void SetLandTile(LandTile tile) {
        NetState.Send(new DrawMapPacket(tile));
    }

    public ReadOnlyCollection<StaticTile> GetStaticTiles(ushort x, ushort y) {
        return _landscape.GetStaticTiles(x, y);
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