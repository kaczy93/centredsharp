using System.Net;
using System.Net.Sockets;
using Cedserver;

namespace Server; 

public static class CEDServer {
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    public static readonly uint ProtocolVersion = (uint)(6 + (Config.CentrEdPlus ? 0x1002 : 0));
    public static Landscape Landscape { get; private set; } = null!;
    public static Socket Listener { get; private set; } = null!;
    public static List<NetState> Clients { get; } = new();

    public static DateTime StartTime = DateTime.Now;
    private static DateTime _lastFlush = DateTime.Now;
    private static DateTime _lastBackup = DateTime.Now;
    
    public static bool Quit { get; set; }

    private static bool _valid;

    public static void Init(string[] args) {
        LogInfo("Initialization started");
        Config.Init(args);
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new Landscape(Config.Map.MapPath, Config.Map.Statics, Config.Map.StaIdx, Config.Tiledata,
            Config.Radarcol, Config.Map.Width, Config.Map.Height, out _valid);
        Listener = Bind(new IPEndPoint(IPAddress.Any, Config.Port));
        Quit = false;
        if(_valid) 
            LogInfo("Initialization done");
        else {
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static Socket Bind(IPEndPoint endPoint) {
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

    private static async void Listen() {
        while (true) {
            var ns = new NetState(await Listener.AcceptAsync());
            Clients.Add(ns);
            ns.Send(new ProtocolVersionPacket(ProtocolVersion));
            new Task(() => ns.Receive()).Start();
        }
    }
    
    private static void CheckNetStates() {
        List<NetState> toRemove = new List<NetState>();
        foreach (var ns in Clients) {
            if (ns.IsConnected) {
                if (DateTime.Now - TimeSpan.FromMinutes(2) > ns.LastAction) {
                    ns.LogInfo($"Timeout: {(ns.Account != null ? ns.Account.Name : string.Empty)}");
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

    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public static void Run() {
        if (!_valid) return;
        Listener.Listen(8);
        new Task(Listen).Start();
        do {
            CheckNetStates();
            if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush) {
                Landscape.Flush();
                Config.Flush();
                _lastFlush = DateTime.Now;
            }
            if (Config.Autobackup.Enabled && DateTime.Now - Config.Autobackup.Interval > _lastBackup) {
                Landscape.Backup();
                _lastBackup = DateTime.Now;
            }
            Thread.Sleep(1);
        } while (!Quit);
        Config.Flush();
    }

    public static void Send(Packet packet) {
        foreach (var ns in Clients) {
            if (ns.IsConnected) {
                ns.Send(packet);
            }
        }
    }

    private static void OnDisconnect(NetState ns) {
        if (ns.Account != null)
            Send(new ClientDisconnectedPacket(ns.Account.Name));
    }
    
    public static void LogInfo(string log) {
        Log("INFO", log);
    }

    public static void LogError(string log) {
        Log("ERROR", log);
    }

    public static void LogDebug(string log) {
        if (DEBUG) Log("DEBUG", log);
    }

    private static void Log(string level, string log) {
        Console.WriteLine($"[{level}] {DateTime.Now} {log}");
    }
}
