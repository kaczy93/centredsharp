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

        netStateTask = new Task(() => Update());
        netStateTask.Start();
        NetState.Send(new LoginRequestPacket(username, password));

        do {
            Update();
        } while (!Initialized);
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

    public void Update() {
        try {
            if(DateTime.Now - TimeSpan.FromMinutes(1) > NetState.LastAction)
            {
                Send(new NoOpPacket());
            }
            NetState.Receive();

            if (NetState.FlushPending) {
                NetState.Flush();
            }
        }
        catch {
            NetState.Dispose();
        }
    }

    public ushort Width => _landscape.Width;
    public ushort Height => _landscape.Height;

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
        _landscape.BlockCache.Resize(1024);
    }

    public void LoadBlocks(List<BlockCoords> blockCoords) {
        var filteredBlocks = blockCoords.FindAll(b => !_landscape.BlockCache.Contains(b.X, b.Y));
        if (filteredBlocks.Count <= 0) return;
        Send(new RequestBlocksPacket(filteredBlocks));
        foreach (var block in filteredBlocks) {
            while (!_landscape.BlockCache.Contains(block.X, block.Y)) {
                Thread.Sleep(1);
                Update();
            }
        }
    }

    public void SetPos(ushort x, ushort y) {
        //nothing to do yet
    }

    public void ChatMessage(string sender, ushort message) {
        Logger.LogInfo($"{sender}: {message}");
    }
    
    public LandTile GetLandTile(int x, int y) {
        return _landscape.GetLandTile(Convert.ToUInt16(x), Convert.ToUInt16(y));
    }
    
    public void SetLandTile(LandTile tile) {
        NetState.Send(new DrawMapPacket(tile));
    }

    public IEnumerable<StaticTile> GetStaticTiles(int x, int y) {
        return _landscape.GetStaticTiles(Convert.ToUInt16(x), Convert.ToUInt16(y));
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

    public void ResizeCache(int newSize) {
        _landscape.BlockCache.Resize(newSize);
    }
}