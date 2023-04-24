using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Server; 

public class CEDServer {
    private ProtocolVersion ProtocolVersion;
    private Socket Listener { get; } = null!;
    
    public Config Config { get; }
    public Landscape Landscape { get; }
    public List<NetState<CEDServer>> Clients { get; } = new();

    public DateTime StartTime = DateTime.Now;
    private DateTime _lastFlush = DateTime.Now;
    private DateTime _lastBackup = DateTime.Now;
    
    public bool Quit { get; set; }

    private bool _valid;

    public CEDServer(string[] args) {
        Logger.LogInfo("Initialization started");
        Config = Config.Init(args);
        Logger.LogInfo("Running as " + (Config.CentrEdPlus ? "CentrED+ 0.7.9" : "CentrED 0.6.3"));
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new Landscape(Config.Map.MapPath, Config.Map.Statics, Config.Map.StaIdx, Config.Tiledata,
            Config.Radarcol, Config.Map.Width, Config.Map.Height, out _valid);
        Listener = Bind(new IPEndPoint(IPAddress.Any, Config.Port));
        Quit = false;
        if(_valid) 
            Logger.LogInfo("Initialization done");
        else {
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public NetState<CEDServer>? GetClient(string name) {
        return Clients.Find(ns => ns.Username == name);
    }
    
    public Account? GetAccount(string name) {
        return Config.Accounts.Find(a => a.Name == name);
    }
    
    public Account? GetAccount(NetState<CEDServer> ns) {
        return Config.Accounts.Find(a => a.Name == ns.Username);
    }
    
    public Region? GetRegion(string name) {
        return Config.Regions.Find(a => a.Name == name);
    }

    private Socket Bind(IPEndPoint endPoint) {
        var s = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            s.NoDelay = true;
            s.LingerState!.Enabled = false;
            s.ExclusiveAddressUse = false;

            s.Bind(endPoint);
            return s;
        }
        catch (Exception e)
        {
            if (e is SocketException se)
            {
                if (se.ErrorCode == 10048)
                {
                    // WSAEADDRINUSE
                    Console.WriteLine("Listener Failed: {0}:{1} (In Use)", endPoint.Address, endPoint.Port);
                }
                else if (se.ErrorCode == 10049)
                {
                    Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", endPoint.Address, endPoint.Port);
                }
                else
                {
                    Console.WriteLine("Listener Exception:");
                    Console.WriteLine(e);
                }
            }

            return null!;
        }
    }

    private async void Listen() {
        while (true) {
            var ns = new NetState<CEDServer>(this, await Listener.AcceptAsync(), PacketHandlers.Handlers);
            ns.ProtocolVersion = ProtocolVersion;
            Clients.Add(ns);
            ns.Send(new ProtocolVersionPacket((uint)ProtocolVersion));
            new Task(() => ns.Receive()).Start();
        }
    }
    
    private void CheckNetStates() {
        var toRemove = new List<NetState<CEDServer>>();
        foreach (var ns in Clients) {
            if (ns.IsConnected) {
                if (DateTime.Now - TimeSpan.FromMinutes(2) > ns.LastAction) {
                    ns.LogInfo($"Timeout: {ns.Username}");
                    ns.Dispose();
                }
            }
            else {
                OnDisconnect(ns);
                toRemove.Add(ns);
            }
        }
        foreach (var netState in toRemove) {
            Clients.Remove(netState);
        }
    }

    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        Logger.LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public void Run() {
        if (!_valid) return;
        Listener.Listen(8);
        new Task(Listen).Start();
        try {
            do {
                CheckNetStates();
                if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush) {
                    Landscape.Flush();
                    Config.Flush();
                    _lastFlush = DateTime.Now;
                }

                if (Config.AutoBackup.Enabled && DateTime.Now - Config.AutoBackup.Interval > _lastBackup) {
                    Backup();
                    _lastBackup = DateTime.Now;
                }

                Thread.Sleep(1);
            } while (!Quit);

        }
        finally {
            foreach (var ns in Clients) {
                ns.Dispose();
            }
        }
        Config.Flush();
    }

    public void Send(Packet packet) {
        foreach (var ns in Clients) {
            if (ns.IsConnected) {
                ns.Send(packet);
            }
        }
    }

    private void OnDisconnect(NetState<CEDServer> ns) {
        if (ns.Username != "")
            Send(new ClientDisconnectedPacket(ns.Username));
    }

    private void Backup() {
        Landscape.Flush();
        var logMsg = "Automatic backup in progress";
        Logger.LogInfo(logMsg);
        Send(new ServerStatePacket(ServerState.Other, logMsg));
        String backupDir;
        for (var i = Config.AutoBackup.MaxBackups; i > 0; i--) {
            backupDir = $"{Config.AutoBackup.Directory}/Backup{i}";
            if(Directory.Exists(backupDir))
                if (i == Config.AutoBackup.MaxBackups)
                    Directory.Delete(backupDir, true);
                else 
                    Directory.Move(backupDir, $"{Config.AutoBackup.Directory}/Backup{i + 1}");
        }
        backupDir = $"{Config.AutoBackup.Directory}/Backup1";
        Directory.CreateDirectory(backupDir);
        
        Landscape.Backup(backupDir);
        
        Send(new ServerStatePacket(ServerState.Running));
        Logger.LogInfo("Automatic backup finished.");
    }
}
