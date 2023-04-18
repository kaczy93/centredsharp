using System.Net.Sockets;
using System.Text;
using Cedserver;
using Shared;

namespace Server; 

public class NetState {
    private Socket Socket { get; set; }
    private MemoryStream ReceiveStream { get; }
    private BinaryReader Reader { get; }
    public Account Account { get; set; }
    public DateTime LastAction { get; set; }
    
    public NetState(Socket socket) {
        Socket = socket;
        ReceiveStream = new MemoryStream(4096);
        Reader = new BinaryReader(ReceiveStream, Encoding.UTF8);
        Account = null!; //Account will be null only when something goes wrong during login
        LastAction = DateTime.Now;
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
            LogError("Exception during receive");
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
                var packetHandler = PacketHandlers.GetHandler(packetId);
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
                    LogError($"Dropping client due to unknown packet: {packetId}");
                    Dispose();
                }
            }
            LastAction = DateTime.Now;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            LogError("Error processing buffer of client");
        }
    }

    public void Dispose() {
        if (!Socket.Connected) return;
        LogInfo("Disconnecting");

        try {
            Socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException e) {
            CEDServer.LogError(e.ToString());
        }
        try {
            Socket.Close();
        }
        catch (SocketException e) {
            CEDServer.LogError(e.ToString());
        }

        Socket = null!;
    }
    
    public void Send(Packet packet) {
        Socket.Send(packet.Data);
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
        Log("INFO", log);
    }

    public void LogError(string log) {
        Log("ERROR", log);
    }

    public void LogDebug(string log) {
        if (CEDServer.DEBUG) Log("DEBUG", log);
    }
    private void Log(string level, string log) {
        Console.WriteLine($"[{level}] {DateTime.Now}@{Socket.RemoteEndPoint?.ToString() ?? ""} {log}");
    }
}