using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using CentrED.Client.Map;
using CentrED.Network;
using static CentrED.Client.AdminHandling;

namespace CentrED.Client;

public delegate void Connected();
public delegate void Disconnected();
public delegate void Moved(ushort newX, ushort newY);
public delegate void ClientConnection(string used);
public delegate void ChatMessage(string user, string message);
public delegate void LogMessage(string message);

public record struct User(string Username, AccessLevel AccessLevel, List<string> Regions);
public record struct Region(string Name, List<Rect> Areas);
public record struct Admin(List<User> Users, List<Region> Regions);

public enum ClientState
{
    Error,
    Disconnected,
    Connected,
    Running,
}

public sealed class CentrEDClient : ILogging
{
    private const int RecvPipeSize = 1024 * 256;
    private NetState<CentrEDClient>? NetState { get; set; }
    private ClientLandscape? Landscape { get; set; }
    public bool CentrEdPlus { get; internal set; }
    public ClientState State { get; internal set; } = ClientState.Disconnected;
    public ServerState ServerState { get; internal set; } = ServerState.Running;
    public string ServerStateReason { get; internal set; } = "";
    public string Hostname { get; private set; }
    public int Port { get; private set; }
    public string? Username => NetState?.Username;
    public string? Password { get; private set; }
    public AccessLevel AccessLevel { get; internal set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public Stack<Packet[]> UndoStack { get; private set; } = new();
    internal List<Packet>? UndoGroup;
    
    internal Queue<PointU16> RequestedBlocksQueue = new();
    internal HashSet<PointU16> RequestedBlocks = [];
    public List<String> Clients { get; } = new();
    public bool Running => State == ClientState.Running;
    public string Status { get; internal set; } = "";
    internal TileDataLand[]? LandTileData;
    internal TileDataStatic[]? StaticTileData;
    public Admin Admin = new([], []);

    private void Reset()
    {
        NetState?.Dispose();
        NetState = null;
        Landscape = null;
        Hostname = "";
        Port = 0;
        Password = "";
        AccessLevel = AccessLevel.None;
        X = 0;
        Y = 0;
        UndoStack.Clear();
        UndoGroup = null;
        RequestedBlocksQueue.Clear();
        RequestedBlocks.Clear();
        Clients.Clear();
        State = ClientState.Disconnected;
        Status = "";
        Admin = new Admin([],[]);
    }

    private void RegisterPacketHandlers(NetState<CentrEDClient> ns)
    {
        ns.RegisterPacketHandler(0x01, 0, Zlib.OnCompressedPacket);
        ns.RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        ns.RegisterPacketHandler(0x03, 0, AdminHandling.OnAdminHandlerPacket);
        ns.RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        ns.RegisterPacketHandler(0x0D, 0, RadarMap.OnRadarHandlerPacket);
    }

    public void Connect(string hostname, int port, string username, string password)
    {
        Reset();
        Hostname = hostname;
        Port = port;
        Password = password;
        var ipAddress = Dns.GetHostAddresses(hostname)[0];
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(ipEndPoint);
        State = ClientState.Connected;
        NetState = new NetState<CentrEDClient>(this, socket, recvPipeSize: RecvPipeSize);
        RegisterPacketHandlers(NetState);
        NetState.Username = username;
        NetState.Send(new LoginRequestPacket(username, password));
        NetState.Flush();
        
        do
        {
            NetState.Receive();
        } while (State == ClientState.Connected);
    }

    public void InitTileData(TileDataLand[] landTileData, TileDataStatic[] staticTileData)
    {
        LandTileData = landTileData;
        StaticTileData = staticTileData;
    }

    public void Disconnect()
    {
        if (Running)
        {
            Send(new QuitPacket());
            while (NetState.FlushPending)
            {
                NetState.Flush();
            }
            while (NetState.Receive())
            {
                //Wait for QuitAckPacket
            }
        }
        else
        {
            Shutdown();
        }
    }
    
    public void Shutdown()
    {
        NetState?.Disconnect();
        Landscape = null;
        State = ClientState.Disconnected;
        while (NetState.FlushPending)
            NetState.Flush();
        NetState.Dispose();
        Disconnected?.Invoke();
        Status = "Disconnected";
    }

    public void Update()
    {
        if (Running)
        {
            try
            {
                if (DateTime.Now - TimeSpan.FromSeconds(30) > NetState.LastAction)
                {
                    Send(new NoOpPacket());
                }
                UpdateRequestedBlocks();
                
                if (NetState.FlushPending)
                {
                    if (!NetState.Flush())
                    {
                        Disconnect();
                        State = ClientState.Error;
                    }
                }

                if (!NetState.Receive())
                {
                    State = ClientState.Error;
                }
            }
            catch(Exception e)
            {
                Shutdown();
                State = ClientState.Error;
                throw;
            }
        }
    }

    public ushort Width => Landscape?.Width ?? 0;
    public ushort Height => Landscape?.Height ?? 0;
    public ushort WidthInTiles => Landscape?.WidthInTiles ?? 0;
    public ushort HeightInTiles => Landscape?.HeightInTiles ?? 0;

    public void LoadBlocks(AreaInfo areaInfo)
    {
        RequestBlocks(areaInfo);
        while (WaitingForBlocks)
        {
            Update();
        }
    }

    public void RequestBlocks(AreaInfo areaInfo)
    {
        List<PointU16> requested = new List<PointU16>();
        for (var x = areaInfo.Left / 8; x <= areaInfo.Right / 8; x++)
        {
            for (var y = areaInfo.Top / 8; y <= areaInfo.Bottom / 8; y++)
            {
                requested.Add(new PointU16((ushort)x, (ushort)y));
            }
        }

        ResizeCache(Math.Max(1, areaInfo.Width * areaInfo.Height / 8));
        RequestBlocks(requested);
    }

    public void RequestBlocks(List<PointU16> blockCoords)
    {
        var filteredBlockCoords = blockCoords.FindAll
            (b => !Landscape.BlockCache.Contains(Block.Id(b.X, b.Y)) && !RequestedBlocks.Contains(b) && IsValidX(b.X) && IsValidY(b.Y));
        
        filteredBlockCoords.ForEach(b => RequestedBlocks.Add(b));
        filteredBlockCoords.ForEach(b => RequestedBlocksQueue.Enqueue(b));;
    }

    private void UpdateRequestedBlocks()
    {
        if (RequestedBlocksQueue.Count > 0)
        {
            var blocksCount = Math.Min(RequestedBlocksQueue.Count, 1000);
            var packet = new RequestBlocksPacket(Enumerable.Range(0, blocksCount).Select(_ => RequestedBlocksQueue.Dequeue()));
            if (blocksCount > 20)
            {
                SendCompressed(packet);
            }
            else
            {
                Send(packet);
            }
        }
    }

    public bool WaitingForBlocks => RequestedBlocks.Count > 0;

    public bool IsValidX(int x)
    {
        return x >= 0 && x < WidthInTiles;
    }

    public bool IsValidY(int y)
    {
        return y >= 0 && y < HeightInTiles;
    }
    
    public bool InternalSetPos(ushort x, ushort y)
    {
        if (x == X && y == Y)
            return false;
        if(!IsValidX(x) || !IsValidY(y))
            return false;

        if(Landscape.TileBlockIndex(x,y) != Landscape.TileBlockIndex(X,Y))
        {
            Send(new UpdateClientPosPacket(x, y));
        }
        X = x;
        Y = y;
        return true;
    }
    
    public void SetPos(ushort x, ushort y)
    {
        if(InternalSetPos(x, y))
        {
            Moved?.Invoke(x, y);
        }
    }
    
    
    public LandTile GetLandTile(int x, int y)
    {
        return Landscape.GetLandTile(Convert.ToUInt16(x), Convert.ToUInt16(y));
    }
    
    public bool TryGetLandTile(int x, int y, [MaybeNullWhen(false)] out LandTile landTile)
    {
        if (!IsValidX(x) || !IsValidY(y))
        {
            landTile = null;
            return false;
        }
        return Landscape.TryGetLandTile(Convert.ToUInt16(x), Convert.ToUInt16(y), out landTile);
    }

    public IEnumerable<StaticTile> GetStaticTiles(int x, int y)
    {
        return Landscape.GetStaticTiles(Convert.ToUInt16(x), Convert.ToUInt16(y));
    }
    
    public bool TryGetStaticTiles(int x, int y, [MaybeNullWhen(false)] out IEnumerable<StaticTile> staticTiles)
    {
        if (!IsValidX(x) || !IsValidY(y))
        {
            staticTiles = Enumerable.Empty<StaticTile>();
            return false;
        }
        return Landscape.TryGetStaticTiles(Convert.ToUInt16(x), Convert.ToUInt16(y), out staticTiles);
    }

    public void Add(StaticTile tile)
    {
        Landscape.AddTile(tile);
    }

    public void Remove(StaticTile tile)
    {
        Landscape.RemoveTile(tile);
    }

    public void Send(Packet p)
    {
        NetState.Send(p);
    }

    public void Send(ReadOnlySpan<byte> data)
    {
        NetState.Send(data);
    }

    public void SendCompressed(Packet p)
    {
        NetState.SendCompressed(p);
    }

    public void ResizeCache(int newSize)
    {
        Landscape?.BlockCache.Resize(newSize);
    }

    public void Flush()
    {
        NetState.Send(new ServerFlushPacket());
    }
    
    public bool BeginUndoGroup()
    {
        if (UndoGroup != null)
        {
            return false; //Group already opened, 
        }
        UndoGroup = new List<Packet>();
        return true;
    }

    public void EndUndoGroup()
    {
        if (UndoGroup?.Count > 0)
        {
            UndoStack.Push(UndoGroup.ToArray());
        }
        UndoGroup = null;
    }

    internal void PushUndoPacket(Packet p)
    {
        if (UndoGroup != null)
        {
            UndoGroup.Add(p);
        }
        else
        {
            UndoStack.Push([p]);
        }
    }

    public void Undo()
    {
        if (UndoStack.Count > 0)
        {
            foreach (var packet in UndoStack.Pop().Reverse())
            {
                NetState.Send(packet);
            }
        }
           
    }
    
    internal void InitLandscape(ushort width, ushort height)
    {
        Landscape = new ClientLandscape(this, width, height);
        Landscape.RegisterPacketHandlers(NetState!);
        Landscape.BlockCache.Resize(Math.Max(width, height) + 1);
        Connected?.Invoke();
        State = ClientState.Running;
        if (AccessLevel == AccessLevel.Administrator)
        {
            Send(new ListUsersPacket());
            Send(new ListRegionsPacket());
        }
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
    public event StaticChanged? AfterStaticChanged;
    public event StaticHued? StaticTileHued;
    public event ClientConnection? ClientConnected;
    public event ClientConnection? ClientDisconnected;
    public event Moved? Moved;
    public event ChatMessage? ChatMessage;
    public event RadarChecksum? RadarChecksum;
    public event RadarData? RadarData;
    public event RadarUpdate? RadarUpdate;
    public event UserModified? UserModified;
    public event UserDeleted? UserDeleted;
    public event RegionModified? RegionModified;
    public event RegionDeleted? RegionDeleted;
    public event LogMessage? LoggedInfo;
    public event LogMessage? LoggedWarn;
    public event LogMessage? LoggedError;
    public event LogMessage? LoggedDebug;
    
    internal void OnMapChanged()
    {
        MapChanged?.Invoke();
    }

    internal void OnBlockReleased(Block block)
    {
        BlockUnloaded?.Invoke(block);
        OnMapChanged();
    }

    internal void OnBlockLoaded(Block block)
    {
        BlockLoaded?.Invoke(block);
        OnMapChanged();
    }

    internal void OnLandReplaced(LandTile landTile, ushort newId, sbyte newZ)
    {
        LandTileReplaced?.Invoke(landTile, newId, newZ);
        OnMapChanged();
    }

    internal void OnLandElevated(LandTile landTile, sbyte newZ)
    {
        LandTileElevated?.Invoke(landTile, newZ);
        OnMapChanged();
    }

    internal void OnStaticTileAdded(StaticTile staticTile)
    {
        StaticTileAdded?.Invoke(staticTile);
        OnMapChanged();
    }

    internal void OnStaticTileRemoved(StaticTile staticTile)
    {
        StaticTileRemoved?.Invoke(staticTile);
        OnMapChanged();
    }

    internal void OnStaticTileReplaced(StaticTile staticTile, ushort newId)
    {
        StaticTileReplaced?.Invoke(staticTile, newId);
        OnMapChanged();
    }

    internal void OnStaticTileMoved(StaticTile staticTile, ushort newX, ushort newY)
    {
        StaticTileMoved?.Invoke(staticTile, newX, newY);
        OnMapChanged();
    }

    internal void OnStaticTileElevated(StaticTile staticTile, sbyte newZ)
    {
        StaticTileElevated?.Invoke(staticTile, newZ);
        OnMapChanged();
    }
    
    internal void OnAfterStaticChanged(StaticTile staticTile)
    {
        AfterStaticChanged?.Invoke(staticTile);
        OnMapChanged();
    }

    internal void OnStaticTileHued(StaticTile staticTile, ushort newHue)
    {
        StaticTileHued?.Invoke(staticTile, newHue);
        OnMapChanged();
    }

    internal void OnClientConnected(string user)
    {
        ClientConnected?.Invoke(user);   
    }
    
    internal void OnClientDisconnected(string user)
    {
        ClientDisconnected?.Invoke(user);
    }

    internal void OnChatMessage(string user, string message)
    {
        ChatMessage?.Invoke(user, message);
    }

    internal void OnRadarChecksum(uint checksum)
    {
        RadarChecksum?.Invoke(checksum);
    }

    internal void OnRadarData(ReadOnlySpan<ushort> data)
    {
        RadarData?.Invoke(data);
    }

    internal void OnRadarUpdate(ushort x, ushort y, ushort color)
    {
        RadarUpdate?.Invoke(x, y, color);
    }

    internal void OnUserModified(string username, ModifyUserStatus status)
    {
        UserModified?.Invoke(username, status);
    }
    
    internal void OnUserDeleted(string username)
    {
        UserDeleted?.Invoke(username);
    }
    
    internal void OnRegionModified(string name, ModifyRegionStatus status)
    {
        RegionModified?.Invoke(name, status);
    }
    
    internal void OnRegionDeleted(string name)
    {
        RegionDeleted?.Invoke(name);
    }
    public void LogInfo(string message)
    {
        LoggedInfo?.Invoke(message);
    }

    public void LogWarn(string message)
    {
        LoggedWarn?.Invoke(message);
    }

    public void LogError(string message)
    {
        LoggedError?.Invoke(message);
    }

    public void LogDebug(string message)
    {
        LoggedDebug?.Invoke(message);
    }

    #endregion
}