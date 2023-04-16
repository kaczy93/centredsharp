using System.Net;
using System.Net.Sockets;
using System.Text;
using Cedserver;
using Shared;

namespace Server; 

public static class CEDServer {
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    public static readonly uint ProtocolVersion = (uint)(6 + (Config.CentrEdPlus ? 0x1002 : 0));
    public static Landscape Landscape { get; private set; }
    public static TcpListener TCPServer { get; private set; }
    public static List<NetState> Clients { get; private set;  }
    public static bool Quit { get; set; }
    
    private static bool _valid;

    public static DateTime StartTime;

    private static DateTime _lastFlush;
    private static DateTime _lastBackup;


    public static void Init(string[] args) {
        LogInfo("Initialization started");
        Config.Init(args);
        StartTime = DateTime.Now;
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new Landscape(Config.Map.MapPath, Config.Map.Statics, Config.Map.StaIdx, Config.Tiledata,
            Config.Radarcol, Config.Map.Width, Config.Map.Height, ref _valid);
        TCPServer = new TcpListener(IPAddress.Any, Config.Port);
        TCPServer.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Quit = false;
        _lastFlush = DateTime.Now;
        _lastBackup = DateTime.Now;
        Clients = new List<NetState>();
        if(_valid) 
            LogInfo("Initialization done");
        else {
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static async void Listen() {
        while (true) {
            var ns = new NetState(await TCPServer.AcceptTcpClientAsync());
            Clients.Add(ns);
            SendPacket(ns, new ConnectionHandling.ProtocolVersionPacket(ProtocolVersion));
            new Task(() => Receive(ns)).Start();
        }
    }

    private static async void Receive(NetState ns) {
        byte[] buffer = new byte[ns.ReceiveStream.Capacity];
        try {
            while (true) {
                int bytesRead = await ns.TcpClient.GetStream().ReadAsync(buffer);
                if (bytesRead > 0) {
                    ns.ReceiveStream.Write(buffer, 0, bytesRead);
                    ProcessBuffer(ns);
                    buffer = new byte[ns.ReceiveStream.Capacity - ns.ReceiveStream.Length];
                    ns.ReceiveStream.Position = ns.ReceiveStream.Length;
                }
            }
        }
        catch (Exception e) {
            ns.LogError("Exception during receive");
            Console.WriteLine(e);
        }
    }

    private static void ProcessBuffer(NetState ns) {
        try {
            ns.ReceiveStream.Position = 0;
            while (ns.ReceiveStream.Length >= 1 && ns.TcpClient.Connected) {
                using var reader = new BinaryReader(ns.ReceiveStream, Encoding.UTF8, true);
                var packetId = reader.ReadByte();
                var packetHandler = PacketHandlers.GetHandler(packetId);
                if (packetHandler != null) {
                    ns.LastAction = DateTime.Now;
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (ns.ReceiveStream.Length > 5) {
                            size = reader.ReadUInt32();
                        }
                        else {
                            break; //wait for more data
                        }
                    }

                    if (ns.ReceiveStream.Length >= size) {
                        using var packetReader = new BinaryReader(ns.ReceiveStream.Dequeue((int)size));
                        //buffer.Lock()
                        packetHandler.OnReceive(packetReader, ns);
                        //buffer.Unlock()/
                        
                    }
                    else {
                        break; //wait for more data
                    }
                }
                else {
                    ns.LogError($"Dropping client due to unknown packet: {packetId}");
                    Disconnect(ns);
                }
            }

            ns.LastAction = DateTime.Now;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            ns.LogError("Error processing buffer of client");
        }
    }

    private static void CheckNetStates() {
        List<NetState> toRemove = new List<NetState>();
        foreach (var ns in Clients) {
            if (ns.TcpClient.Connected) {
                if (DateTime.Now - TimeSpan.FromMinutes(2) > ns.LastAction) {
                    ns.LogInfo($"Timeout: {(ns.Account != null ? ns.Account.Name : string.Empty)}");
                    Disconnect(ns);
                }
            }
            else {
                OnDisconnect(ns);
                toRemove.Add(ns);
            }
        }
        foreach (var netState in toRemove) {
            Clients.Remove(netState);
            netState.TcpClient.Dispose();
        }
    }

    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public static void Run() {
        if (!_valid) return;
        TCPServer.Start();
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

    public static void SendPacket(NetState? ns, Packet packet) {
        if (ns != null) {
            packet.Write(ns.TcpClient.GetStream());
        }
        else { //broadcast
            foreach (var netState in Clients) {
                if (netState.TcpClient.Connected) {
                    SendPacket(netState, packet);
                }
            }
        }
    }

    private static void OnDisconnect(NetState? ns) {
        if (ns == null) return;
        ns.LogInfo("Disconnect");
        
        if (ns.Account != null)
            SendPacket(null, new ClientHandling.ClientDisconnectedPacket(ns.Account.Name));
    }

    public static void Disconnect(NetState ns) {
        if (ns.TcpClient.Connected) {
            ns.TcpClient.Client.Disconnect(true);
        }
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
