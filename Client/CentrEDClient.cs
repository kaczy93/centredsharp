using System.Net;
using System.Net.Sockets;
using CentrED.Client.Map;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public delegate void Connected();
public delegate void Disconnected();
public delegate void Moved(ushort newX, ushort newY);
public sealed class CentrEDClient : IDisposable {
    private NetState<CentrEDClient> NetState { get; set; }
    private ClientLandscape? Landscape { get; set; }
    public bool CentrEdPlus { get; internal set; }
    public bool Initialized { get; internal set; }
    public string Username { get; private set; }
    public string Password { get; private set; }
    public AccessLevel AccessLevel { get; internal set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public List<String> Clients { get; } = new();
    public bool Running;
    
    public void Connect(string hostname, int port, string username, string password) {
        Username = username;
        Password = password;
        var ipAddress = Dns.GetHostAddresses(hostname)[0];
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipEndPoint);
        NetState = new NetState<CentrEDClient>(this, socket, PacketHandlers.Handlers);
        NetState.Send(new LoginRequestPacket(username, password));
        Running = true;

        do {
            Update();
        } while (!Initialized);
        Connected?.Invoke();
    }

    public void Disconnect() {
        if (Running) {
            Send(new QuitPacket());
            while (NetState.FlushPending)
                NetState.Flush();
        }
        NetState.Disconnect();
        Running = false;
        Landscape = null;
        Initialized = false;
        Disconnected?.Invoke();
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
            while (NetState.FlushPending)
                NetState.Flush();
            NetState.Dispose();
        }
    }

    public void Update() {
        if (Running) {
            try {
                if (DateTime.Now - TimeSpan.FromMinutes(1) > NetState.LastAction) {
                    Send(new NoOpPacket());
                }

                NetState.Receive();

                if (NetState.FlushPending) {
                    if (!NetState.Flush()) {
                        Disconnect();
                    }
                }
            }
            catch {
                Disconnect();
            }
        }
    }

    public ushort Width => Landscape?.Width ?? 0;
    public ushort Height => Landscape?.Height ?? 0;

    public void InitLandscape(ushort width, ushort height) {
        Landscape = new ClientLandscape(this, width, height);
        Landscape.BlockCache.Resize(1024);
        Initialized = true;
    }

    public List<Block> LoadBlocks(List<BlockCoords> blockCoords) {
        var filteredBlockCoords = blockCoords.FindAll(b => 
            !Landscape.BlockCache.Contains(Block.Id(b.X, b.Y)) && 
            isValidX(b.X) && 
            isValidY(b.Y));
        if (filteredBlockCoords.Count <= 0) return new List<Block>();
        Send(new RequestBlocksPacket(filteredBlockCoords));
        List<Block> result = new List<Block>(filteredBlockCoords.Count);
        foreach (var block in filteredBlockCoords) {
            var blockId = Block.Id(block.X, block.Y);
            while (!Landscape.BlockCache.Contains(blockId)) {
                Thread.Sleep(1);
                Update();
            }
            result.Add(Landscape.BlockCache.Get(blockId));
        }
        return result;
    }

    public bool isValidX(int x) {
        return x >= 0 && x < Width * 8;
    }
    
    public bool isValidY(int y) {
        return y >= 0 && y < Height * 8;
    }

    public ushort ClampX(int x) {
        return (ushort)Math.Min(x, Width - 1);
    }
    
    public ushort ClampY(int y) {
        return (ushort)Math.Min(y, Height - 1);
    }

    public void SetPos(ushort x, ushort y) {
        if (x == X && y == Y) return;
        
        X = x;
        Y = y;
        Send(new UpdateClientPosPacket(x, y));
        Moved?.Invoke(x,y);
    }

    public void ChatMessage(string sender, ushort message) {
        Logger.LogInfo($"{sender}: {message}");
    }
    
    public LandTile GetLandTile(int x, int y) {
        return Landscape.GetLandTile(Convert.ToUInt16(x), Convert.ToUInt16(y));
    }
    
    public IEnumerable<StaticTile> GetStaticTiles(int x, int y) {
        return Landscape.GetStaticTiles(Convert.ToUInt16(x), Convert.ToUInt16(y));
    }

    public void Add(StaticTile tile) {
        NetState.Send(new InsertStaticPacket(tile));
    }

    public void Remove(StaticTile tile) {
        NetState.Send(new DeleteStaticPacket(tile));
    }
    
    internal void Send(Packet p) {
        NetState.Send(p);
    }

    public void ResizeCache(int newSize) {
        Landscape.BlockCache.Resize(newSize);
    }

    public void Flush() {
        NetState.Send(new ServerFlushPacket());
    }

    #region events
    
    /*
     * Client emits events of changes that came from the server
     */

    public event Connected? Connected;
    public event Disconnected? Disconnected;
    public event MapChanged? MapChanged;
    public event BlockChanged? BlockUnloaded;
    public event BlockChanged? BlockLoaded;
    public event LandReplaced? LandTileReplaced;
    public event LandElevated? LandTileElevated;
    public event StaticChanged? StaticTileAdded;
    public event StaticChanged? StaticTileRemoved;
    public event StaticReplaced? StaticTileReplaced;
    public event StaticMoved? StaticTileMoved;
    public event StaticElevated? StaticTileElevated;
    public event StaticHued? StaticTileHued;
    public event Moved? Moved;
    
    
    internal void OnMapChanged() {
        MapChanged?.Invoke();
    }

    internal void OnBlockReleased(Block block) {
        BlockUnloaded?.Invoke(block);
        OnMapChanged();
    }

    internal void OnBlockLoaded(Block block) {
        BlockLoaded?.Invoke(block);
        OnMapChanged();
    }

    internal void OnLandReplaced(LandTile landTile, ushort newId) {
        LandTileReplaced?.Invoke(landTile, newId);
        OnMapChanged();
    }

    internal void OnLandElevated(LandTile landTile, sbyte newZ) {
        LandTileElevated?.Invoke(landTile, newZ);
        OnMapChanged();
    }

    internal void OnStaticTileAdded(StaticTile staticTile) {
        StaticTileAdded?.Invoke(staticTile);
        OnMapChanged();
    }

    internal void OnStaticTileRemoved(StaticTile staticTile) {
        StaticTileRemoved?.Invoke(staticTile);
        OnMapChanged();
    }
    
    internal void OnStaticTileReplaced(StaticTile staticTile, ushort newId) {
        StaticTileReplaced?.Invoke(staticTile, newId);
        OnMapChanged();
    }
    
    internal void OnStaticTileMoved(StaticTile staticTile, ushort newX, ushort newY) {
        StaticTileMoved?.Invoke(staticTile, newX, newY);
        OnMapChanged();
    }

    internal void OnStaticTileElevated(StaticTile staticTile, sbyte newZ) {
        StaticTileElevated?.Invoke(staticTile, newZ);
        OnMapChanged();
    }

    internal void OnStaticTileHued(StaticTile staticTile, ushort newHue) {
        StaticTileHued?.Invoke(staticTile, newHue);
        OnMapChanged();
    }
    
    #endregion
}