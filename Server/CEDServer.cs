using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CentrED.Network;
using CentrED.Server.Config;
using CentrED.Server.Map;
using CentrED.Utility;

namespace CentrED.Server;

public class CEDServer : ILogging, IDisposable
{
    public const int MaxConnections = 1024;
    private const int SendPipeSize = 1024 * 256;
    
    private readonly Logger _logger = new();
    private ProtocolVersion ProtocolVersion;
    private Socket Listener { get; } = null!;
    public ConfigRoot Config { get; }
    public ServerLandscape Landscape { get; }
    public HashSet<NetState<CEDServer>> Clients { get; } = new(8);
    private readonly Dictionary<long, HashSet<NetState<CEDServer>>> _blockSubscriptions = new();

    private readonly ConcurrentQueue<NetState<CEDServer>> _connectedQueue = new();
    private readonly Queue<NetState<CEDServer>> _toDispose = new();
    
    private readonly ConcurrentQueue<string> _consoleCommandQueue = new();

    public DateTime StartTime = DateTime.Now;
    private DateTime _lastFlush = DateTime.Now;
    private DateTime _lastBackup = DateTime.Now;

    public bool Quit { get; set; }
    public bool Running { get; private set; }
    
    public bool CPUIdle { get; private set; } = true;
    private NetState<CEDServer>? _CPUIdleOwner;

    public CEDServer(ConfigRoot config, TextWriter? logOutput = default)
    {
        if (logOutput == null)
            logOutput = Console.Out;
        _logger.Out = logOutput;
        
        LogInfo("Initialization started");
        Config = config;
        ProtocolVersion = Config.CentrEdPlus ? ProtocolVersion.CentrEDPlus : ProtocolVersion.CentrED;
        LogInfo("Running as " + (Config.CentrEdPlus ? "CentrED+ 0.7.9" : "CentrED 0.6.3"));
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new ServerLandscape(config, _logger);
        Listener = Bind(new IPEndPoint(IPAddress.Any, Config.Port));
        LogInfo("Initialization done");
    }

    public NetState<CEDServer>? GetClient(string name)
    {
        return Clients.FirstOrDefault(ns => ns.Username == name);
    }

    public Account? GetAccount(string name)
    {
        return Config.Accounts.Find(a => a.Name == name);
    }

    public Account? GetAccount(NetState<CEDServer> ns)
    {
        return Config.Accounts.Find(a => a.Name == ns.Username);
    }

    public Region? GetRegion(string name)
    {
        return Config.Regions.Find(a => a.Name == name);
    }

    private Socket Bind(IPEndPoint endPoint)
    {
        var s = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            LingerState = new LingerOption(false, 0),
            ExclusiveAddressUse = false,
            NoDelay = true,
            SendBufferSize = SendPipeSize,
            ReceiveBufferSize = 64 * 1024,
        };

        try
        {
            s.Bind(endPoint);
            s.Listen(32);
            LogInfo($"Listening on {s.LocalEndPoint}");
            return s;
        }
        catch (Exception e)
        {
            if (e is SocketException se)
            {
                // WSAEADDRINUSE
                if (se.ErrorCode == 10048)
                {
                    LogError($"Listener Failed: {endPoint.Address}:{endPoint.Port} (In Use)");
                }
                // WSAEADDRNOTAVAIL
                else if (se.ErrorCode == 10049)
                {
                    LogError($"Listener Failed: {endPoint.Address}:{endPoint.Port} (Unavailable)");
                }
                else
                {
                    LogError("Listener Exception:");
                    Console.WriteLine(e);
                }
            }

            return null!;
        }
    }

    private void RegisterPacketHandlers(NetState<CEDServer> ns)
    {
        ns.RegisterPacketHandler(0x01, 0, Zlib.OnCompressedPacket);
        ns.RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        ns.RegisterPacketHandler(0x03, 0, AdminHandling.OnAdminHandlerPacket);
        ns.RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        ns.RegisterPacketHandler(0xFF, 1, ConnectionHandling.OnNoOpPacket);
    }

    private async void Listen()
    {
        try
        {
            while (Running)
            {
                var socket = await Listener.AcceptAsync();
                if (Clients.Count >= MaxConnections)
                {
                    LogError("Too many connections");
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    continue;
                }
                
                var ns = new NetState<CEDServer>(this, socket, sendPipeSize: SendPipeSize)
                {
                    ProtocolVersion = ProtocolVersion
                };
                RegisterPacketHandlers(ns);
                Landscape.RegisterPacketHandlers(ns);
                _connectedQueue.Enqueue(ns);
            }
        }
        catch (Exception e)
        {
            LogError("Server stopped");
            LogError(e.ToString());
        }
        finally
        {
            Quit = true;
        }
    }

    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public void Run()
    {
        Running = true;
        new Task(Listen).Start();
        try
        {
            do
            {
                ProcessConnectedQueue();
                ProcessNetStates();

                AutoSave();
                AutoBackup();
                ProcessCommands();

                if(CPUIdle)
                    Thread.Sleep(1);
            } while (!Quit);
        }
        finally
        {
            Listener.Close();
            foreach (var ns in Clients)
            {
                ns.Dispose();
            }
            Running = false;
        }
    }

    private void ProcessConnectedQueue()
    {
        while (_connectedQueue.TryDequeue(out var ns))
        {
            Clients.Add(ns);
            ns.LogInfo($"Connected. [{Clients.Count} Online]");
            ns.Send(new ProtocolVersionPacket((uint)ProtocolVersion));
            ns.Flush();
        }
    }

    private void ProcessNetStates()
    {
        foreach (var ns in Clients)
        {
            if (!ns.Receive() || !ns.Active)
            {
                _toDispose.Enqueue(ns);
            }
            if (!ns.Flush())
            {
                _toDispose.Enqueue(ns);
            }
        }
        
        while (_toDispose.TryDequeue(out var ns))
        {
            Clients.Remove(ns);
            if (ns.Username != "")
            {
                Send(new ClientDisconnectedPacket(ns));
            }
            if (CPUIdle && _CPUIdleOwner == ns)
            {
                SetCPUIdle(null, true);
            }
            ns.Dispose();
        }
    }

    private void AutoSave()
    {
        if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush)
        {
            Save();
        }
    }

    public void Save()
    {
        Landscape.Flush();
        Config.Flush();
        _lastFlush = DateTime.Now;
    }

    private void AutoBackup()
    {
        if (Config.AutoBackup.Enabled && DateTime.Now - Config.AutoBackup.Interval > _lastBackup)
        {
            Backup();
            _lastBackup = DateTime.Now;
        }
    }

    public void Flush()
    {
        foreach (var ns in Clients)
        {
            if (!ns.FlushPending) continue;
            
            if (!ns.Flush())
            {
                _toDispose.Enqueue(ns);
            }
        }
    }

    public void Send(Packet packet)
    {
        foreach (var ns in Clients)
        {
            ns.Send(packet);
        }
    }
    
    public HashSet<NetState<CEDServer>> GetBlockSubscriptions(ushort x, ushort y)
    {
        Landscape.AssertBlockCoords(x, y);
        var key = Landscape.GetBlockNumber(x, y);

        if (!_blockSubscriptions.TryGetValue(key, out var subscriptions))
        {
            subscriptions = [];
            _blockSubscriptions.Add(key, subscriptions);
        }

        subscriptions.RemoveWhere(ns => !ns.Running);
        return subscriptions;
    }

    private void Backup()
    {
        Landscape.Flush();
        var logMsg = "Automatic backup in progress";
        LogInfo(logMsg);
        Send(new ServerStatePacket(ServerState.Other, logMsg));
        String backupDir;
        for (var i = Config.AutoBackup.MaxBackups; i > 0; i--)
        {
            backupDir = $"{Config.AutoBackup.Directory}/Backup{i}";
            if (Directory.Exists(backupDir))
                if (i == Config.AutoBackup.MaxBackups)
                    Directory.Delete(backupDir, true);
                else
                    Directory.Move(backupDir, $"{Config.AutoBackup.Directory}/Backup{i + 1}");
        }
        backupDir = $"{Config.AutoBackup.Directory}/Backup1";

        Landscape.Backup(backupDir);

        Send(new ServerStatePacket(ServerState.Running));
        LogInfo("Automatic backup finished.");
    }

    public void SetCPUIdle(NetState<CEDServer>? ns, bool enabled)
    {
        if (CPUIdle == enabled)
            return;
        
        Console.WriteLine($"CPU Idle: {enabled}");
        CPUIdle = enabled;
        if (enabled)
            _CPUIdleOwner = ns;
        else
        {
            _CPUIdleOwner = null;
        }
    }

    public void PushCommand(string command)
    {
        _consoleCommandQueue.Enqueue(command);
    }

    private void ProcessCommands()
    {
        while (_consoleCommandQueue.TryDequeue(out var command))
        {
            try
            {
                var parts = command.Split(' ', 2);
                switch (parts)
                {
                    case ["save"]:
                        Console.Write("Saving...");
                        Landscape.Flush();
                        Console.WriteLine("Done");
                        break;
                    case ["save", string dir]:
                        Console.Write($"Saving to {dir}...");
                        Landscape.Backup(dir);
                        Console.WriteLine("Done");
                        break;
                    default: PrintHelp(); break;
                }
                ;
            }
            catch (Exception e)
            {
                LogError($"Error processing command: {command}");
                LogError(e.ToString());
            }
        }
    }

    private void PrintHelp()
    {
        Console.WriteLine("Supported commands:");
        Console.WriteLine("save");
        Console.WriteLine("save <dir>"); 
    }

    public void Dispose()
    {
        Listener.Dispose();
        Landscape.Dispose();
    }
    
    public void LogInfo(string message)
    {
        _logger.LogInfo(message);
    }

    public void LogWarn(string message)
    {
       _logger.LogWarn(message);
    }

    public void LogError(string message)
    {
        _logger.LogError(message);
    }

    public void LogDebug(string message)
    {
#if DEBUG
        _logger.LogDebug(message);
#endif
    }
}