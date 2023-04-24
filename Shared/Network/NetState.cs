using System.Net.Sockets;
using System.Text;
using CentrED.Utility;

namespace CentrED.Server; 

public class NetState {
    private Socket Socket { get; }
    private PacketHandler?[] PacketHandlers { get; }
    private MemoryStream ReceiveStream { get; }
    private BinaryReader Reader { get; }
    public String Username { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public DateTime LastAction { get; set; }
    public DateTime LoginTime { get; }
    
    public NetState(Socket socket, PacketHandler?[] packetHandlers) {
        Socket = socket;
        PacketHandlers = packetHandlers;
        ReceiveStream = new MemoryStream(4096);
        Reader = new BinaryReader(ReceiveStream, Encoding.UTF8);
        Username = "";
        AccessLevel = AccessLevel.None;
        LastAction = DateTime.Now;
        LoginTime = DateTime.Now;
    }
    
    public async void Receive() {
        byte[] buffer = new byte[ReceiveStream.Capacity];
        try {
            while (IsConnected) {
                int bytesRead = await Socket.ReceiveAsync(buffer);
                if (bytesRead > 0) {
                    ReceiveStream.Write(buffer, 0, bytesRead);
                    ProcessBuffer();
                    buffer = new byte[ReceiveStream.Capacity - ReceiveStream.Length];
                    ReceiveStream.Position = ReceiveStream.Length;
                }
            }
        }
        catch (Exception e) {
            LogError("Receive error");
            Console.WriteLine(e);
        }
        finally {
            Dispose();
        }
    }
    
    private void ProcessBuffer() {
        try {
            ReceiveStream.Position = 0;
            while (ReceiveStream.Length >= 1 && Socket.Connected) {
                var packetId = Reader.ReadByte();
                var packetHandler = PacketHandlers[packetId];
                if (packetHandler != null) {
                    LastAction = DateTime.Now;
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (ReceiveStream.Length <= 5) {
                            break; //wait for more data
                        }
                        size = Reader.ReadUInt32();
                    }
                    if (ReceiveStream.Length >= size) {
                        using var packetReader = new BinaryReader(ReceiveStream.Dequeue((int)size));
                        packetHandler.OnReceive(packetReader, this);
                    }
                    else {
                        break; //wait for more data
                    }
                }
                else {
                    LogError($"Unknown packet: {packetId}");
                    Dispose();
                }
            }
            LastAction = DateTime.Now;
        }
        catch (Exception e) {
            LogError("ProcessBuffer error");
            Console.WriteLine(e);
            Dispose();
        }
    }

    public void Dispose() {
        if (!Socket.Connected) return;
        LogInfo("Disconnecting");

        try {
            Socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException e) {
            LogError(e.ToString());
        }
        try {
            Socket.Close();
        }
        catch (SocketException e) {
            LogError(e.ToString());
        }
    }
    
    public void Send(Packet packet) {
        try
        {
            Socket.Send(packet.Compile(out _));
        }
        catch (Exception e)
        {
            LogError("Send Error");
            Console.WriteLine(e);
            Dispose();
        }
    }
    
    public bool IsConnected
    {
        get
        {
            try {
                if (!Socket.Connected) return false;
                
                if (Socket.Poll(0, SelectMode.SelectRead)) {
                    var buff = new byte[1];
                    return Socket.Receive(buff, SocketFlags.Peek) != 0;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public void LogInfo(string log) {
        Logger.LogInfo(LogMessage(log));
    }

    public void LogError(string log) {
        Logger.LogError(LogMessage(log));
    }

    public void LogDebug(string log) {
        Logger.LogDebug(LogMessage(log));
    }

    private string LogMessage(string log) {
        return $"{Username}@{Socket.RemoteEndPoint?.ToString() ?? ""} {log}";
    }
}