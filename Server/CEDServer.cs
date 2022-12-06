//Server/UCEDServer.pas

using System.Net;
using System.Net.Sockets;
using Cedserver;

namespace Server; 

//TCedServer
public static class CEDServer {
    public static Landscape Landscape { get; }
    public static TcpListener TCPServer { get; }
    public static List<NetState> Clients { get; }
    public static bool Quit { get; set; }

    private static DateTime _lastFlush;
    private static bool _valid;

    static CEDServer() {
        Console.WriteLine($"[{DateTime.Now}] Initialization started");
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new Landscape(Config.Map.MapPath, Config.Map.Statics, Config.Map.StaIdx, Config.Tiledata,
            Config.Radarcol, Config.Map.Width, Config.Map.Height, ref _valid);
        TCPServer = new TcpListener(IPAddress.Any, Config.Port);
        TCPServer.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Quit = false;
        _lastFlush = DateTime.Now;
        Console.WriteLine($"[{DateTime.Now}] Initialization done");
    }

    private static void OnAccept(IAsyncResult ar) {
        TcpListener? listener = ar.AsyncState as TcpListener;
        if (listener == null)
            return;
        try {
            var ns = new NetState(listener.EndAcceptTcpClient(ar));
            Clients.Add(ns);
            ns.BeginRead(OnReceive);
        }
        finally {
            listener.BeginAcceptTcpClient(OnAccept, listener);
        }
    }
    

    private static void OnReceive(IAsyncResult ar) {
        NetState ns = ar.AsyncState as NetState;
        if (ns == null) return;

        try {
            int bytesRead = ns.EndRead(ar);
            if(bytesRead > 0)
                ns.ReceiveStream.Write(ns.ReadBuffer,0, bytesRead);
            ProcessBuffer(ns);
        }
        finally {
            if (ns != null) {
                ns.BeginRead(OnReceive);
            }
        }
    }

    private static void OnDisconnect(NetState? ns) {
        if (ns == null) return;
        Console.WriteLine($"[{DateTime.Now}] Disconnect: {ns.TcpClient.Client.RemoteEndPoint}");
        SendPacket(null, new ClientHandling.ClientDisconnectedPacket(ns.Account.Name));
    }
    
    private static void ProcessBuffer(NetState ns) {
        try {
            var buffer = ns.ReceiveStream;
            buffer.Position = 0;
            while (buffer.Length >= 1 && ns.TcpClient.Connected) {
                using var reader = new BinaryReader(buffer);
                var packetId = reader.ReadByte();
                var packetHandler = PacketHandlers.GetHandler(packetId);
                if (packetHandler != null) {
                    ns.LastAction = DateTime.Now;
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (buffer.Length > 5) {
                            size = reader.ReadUInt32();
                        }
                        else {
                            break; //wait for more data
                        }
                    }

                    if (buffer.Length >= size) {
                        //buffer.Lock()
                        packetHandler.OnReceive(reader, ns);
                        //buffer.Unlock()/
                        //buffer.Dequeue(size)
                    }
                    else {
                        break; //wait for more data
                    }
                }
                else {
                    Console.WriteLine($"[{DateTime.Now}] Dropping client due to unknown packet: {ns.TcpClient.Client.RemoteEndPoint}");
                    Disconnect(ns);
                }
            }

            ns.LastAction = DateTime.Now;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            Console.WriteLine($"[{DateTime.Now}] Error processing buffer of client {ns.TcpClient.Client.RemoteEndPoint}");
        }
    }

    private static void CheckNetStates() {
        foreach (var ns in Clients) {
            if (ns == null) return;
            if (ns.TcpClient.Connected) {
                if (DateTime.Now - TimeSpan.FromMinutes(2) > ns.LastAction) {
                    Console.WriteLine($"[{DateTime.Now}] Timeout: {(ns.Account != null ? ns.Account.Name : string.Empty)} {ns.TcpClient.Client.RemoteEndPoint}");
                    Disconnect(ns);
                }
            }
            else {
                OnDisconnect(ns);
            }
        }
    }

    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        Console.WriteLine($"[{DateTime.Now}] Killed");
        Quit = true;
        e.Cancel = true;
    }

    public static void Run() {
        TCPServer.Start();
        TCPServer.BeginAcceptTcpClient(OnAccept, TCPServer);
        do {
            CheckNetStates();
            if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush) {
                Landscape.Flush();
                Config.Flush();
                _lastFlush = DateTime.Now;
            }
            Thread.Sleep(1);
        } while (!Quit);
    }
    
    public static void Disconnect(NetState ns) {
        if (ns.TcpClient.Connected) {
            ns.TcpClient.Close();
            Clients.Remove(ns);
        }
    }

    public static void SendPacket(NetState? ns, Packet packet) {
        if (ns != null) {
            packet.Stream.BaseStream.CopyTo( ns.TcpClient.GetStream());
        }
        else { //broadcast
            foreach (var netState in Clients) {
                if (netState.TcpClient.Connected) {
                    SendPacket(netState, packet);
                }
            }
        }
    }
}
