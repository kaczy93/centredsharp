using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Server.Config;
using CentrED.Server.Map;
using CentrED.Utility;

namespace CentrED.Server; 

public class CEDServer {
    public const int MaxConnections = 1024;
    private ProtocolVersion ProtocolVersion;
    private Socket Listener { get; } = null!;
    public ConfigRoot ConfigRoot { get; }
    public ServerLandscape Landscape { get; }
    public HashSet<NetState<CEDServer>> Clients { get; } = new(8);
    
    private readonly ConcurrentQueue<NetState<CEDServer>> _connectedQueue = new ();
    private readonly ConcurrentQueue<NetState<CEDServer>> _toDispose = new ();
    private readonly ConcurrentQueue<NetState<CEDServer>> _flushPending = new ();

    public DateTime StartTime = DateTime.Now;
    private DateTime _lastFlush = DateTime.Now;
    private DateTime _lastBackup = DateTime.Now;
    
    public bool Quit { get; set; }

    private bool _valid;
    
    public bool Running { get; private set; }

    public CEDServer(string[] args) {
        Logger.LogInfo("Initialization started");
        ConfigRoot = ConfigRoot.Init(args);
        ProtocolVersion = ConfigRoot.CentrEdPlus ? ProtocolVersion.CentrEDPlus : ProtocolVersion.CentrED;
        Logger.LogInfo("Running as " + (ConfigRoot.CentrEdPlus ? "CentrED+ 0.7.9" : "CentrED 0.6.3"));
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new ServerLandscape(ConfigRoot.Map.MapPath, ConfigRoot.Map.Statics, ConfigRoot.Map.StaIdx, ConfigRoot.Tiledata,
            ConfigRoot.Radarcol, ConfigRoot.Map.Width, ConfigRoot.Map.Height, out _valid);
        Listener = Bind(new IPEndPoint(IPAddress.Any, ConfigRoot.Port));
        Quit = false;
        if(_valid) 
            Logger.LogInfo("Initialization done");
        else {
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }
    
    public NetState<CEDServer>? GetClient(string name) {
        return Clients.FirstOrDefault(ns => ns.Username == name);
    }
    
    public Account? GetAccount(string name) {
        return ConfigRoot.Accounts.Find(a => a.Name == name);
    }
    
    public Account? GetAccount(NetState<CEDServer> ns) {
        return ConfigRoot.Accounts.Find(a => a.Name == ns.Username);
    }
    
    public Region? GetRegion(string name) {
        return ConfigRoot.Regions.Find(a => a.Name == name);
    }

    private Socket Bind(IPEndPoint endPoint) {
        var s = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
            LingerState = new LingerOption(false, 0),
            ExclusiveAddressUse = false,
            NoDelay = true,
            SendBufferSize = 64 * 1024,
            ReceiveBufferSize = 64 * 1024,
        };
        
        try {
            s.Bind(endPoint);
            s.Listen(32);
            return s;
        }
        catch (Exception e) {
            if (e is SocketException se) {
                // WSAEADDRINUSE
                if (se.ErrorCode == 10048) {
                    Logger.LogError($"Listener Failed: {endPoint.Address}:{endPoint.Port} (In Use)");
                }
                // WSAEADDRNOTAVAIL
                else if (se.ErrorCode == 10049) {
                    Logger.LogError($"Listener Failed: {endPoint.Address}:{endPoint.Port} (Unavailable)");
                }
                else {
                    Logger.LogError("Listener Exception:");
                    Console.WriteLine(e);
                }
            }

            return null!;
        }
    }

    private async void Listen() {
        while (true) {
            var socket = await Listener.AcceptAsync();
            if (Clients.Count >= MaxConnections) {
                Logger.LogError("Too many connections");
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            else {
                var ns = new NetState<CEDServer>(this, socket, PacketHandlers.Handlers) {
                    ProtocolVersion = ProtocolVersion
                };
                _connectedQueue.Enqueue(ns);
            }
        }
    }
    
    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        Logger.LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public void Run() {
        if (!_valid) return;
        new Task(Listen).Start();
        Running = true;
        try {
            do {
                ProcessConnectedQueue();
                ProcessNetStates();

                AutoSave();
                AutoBackup();
                
                Thread.Sleep(1);
            } while (!Quit);
        }
        finally {
            Listener.Close();
            foreach (var ns in Clients) {
                ns.Dispose();
            }
        }
    }

    private void ProcessConnectedQueue() {
        while (_connectedQueue.TryDequeue(out var ns)) {
            Clients.Add(ns);
            ns.LogInfo($"Connected. [{Clients.Count} Online]");
            ns.Send(new ProtocolVersionPacket((uint)ProtocolVersion));
            ns.Flush();
        }
    }
    
    private void ProcessNetStates() {
        foreach (var ns in Clients) {
            if (!ns.Receive() || !ns.Active) {
                _toDispose.Enqueue(ns);
            }
            if(ns.FlushPending)
                _flushPending.Enqueue(ns);
        }

        while (_flushPending.TryDequeue(out var ns)) {
            if (!ns.Flush()) {
                _toDispose.Enqueue(ns);
            }
        }

        while (_toDispose.TryDequeue(out var ns)) {
            Clients.Remove(ns);
            if (ns.Username != "") {
                Send(new ClientDisconnectedPacket(ns));
            }
            ns.Dispose();
        }
    }

    private void AutoSave() {
        if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush) {
            Landscape.Flush();
            ConfigRoot.Flush();
            _lastFlush = DateTime.Now;
        }
    }

    private void AutoBackup() {
        if (ConfigRoot.AutoBackup.Enabled && DateTime.Now - ConfigRoot.AutoBackup.Interval > _lastBackup) {
            Backup();
            _lastBackup = DateTime.Now;
        }
    }

    public void Send(Packet packet) {
        foreach (var ns in Clients) {
            ns.Send(packet);
        }
    }
    
    private void Backup() {
        Landscape.Flush();
        var logMsg = "Automatic backup in progress";
        Logger.LogInfo(logMsg);
        Send(new ServerStatePacket(ServerState.Other, logMsg));
        String backupDir;
        for (var i = ConfigRoot.AutoBackup.MaxBackups; i > 0; i--) {
            backupDir = $"{ConfigRoot.AutoBackup.Directory}/Backup{i}";
            if(Directory.Exists(backupDir))
                if (i == ConfigRoot.AutoBackup.MaxBackups)
                    Directory.Delete(backupDir, true);
                else 
                    Directory.Move(backupDir, $"{ConfigRoot.AutoBackup.Directory}/Backup{i + 1}");
        }
        backupDir = $"{ConfigRoot.AutoBackup.Directory}/Backup1";
        Directory.CreateDirectory(backupDir);
        
        Landscape.Backup(backupDir);
        
        Send(new ServerStatePacket(ServerState.Running));
        Logger.LogInfo("Automatic backup finished.");
    }
}
