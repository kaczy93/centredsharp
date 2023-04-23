using System.Net;
using System.Net.Sockets;
using CentrED.Utility;

namespace CentrED.Client; 

public class CentrEDClient {
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    private Socket _socket;
    private MemoryStream _receiveStream { get; }
    private BinaryReader _reader { get; }
    
    public bool CentrEdPlus { get; set; }
    
    public bool Initialized { get; }
    public string Username { get; }
    public string Password { get; }
    
    //CentrEDClient and Server.NetState are almost the same, we could share most of the code
    public CentrEDClient(string hostname, int port, string username, string password) {
        Username = username;
        Password = password;
        var ipAddress = Dns.GetHostAddresses(hostname)[0];
        var ipEndPoint = new IPEndPoint(ipAddress, port);
        _socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(ipEndPoint);
        new Task(() => Receive()).Start();

        while (!Initialized) {
            Thread.Sleep(1);
        }
    }
    
    private async void Receive() {
        var buffer = new byte[_receiveStream.Capacity];
        try {
            while (IsConnected) {
                int bytesRead = await _socket.ReceiveAsync(buffer);
                if (bytesRead > 0) {
                    _receiveStream.Write(buffer, 0, bytesRead);
                    ProcessBuffer();
                    buffer = new byte[_receiveStream.Capacity - _receiveStream.Length];
                    _receiveStream.Position = _receiveStream.Length;
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
            _receiveStream.Position = 0;
            while (_receiveStream.Length >= 1 && Socket.Connected) {
                var packetId = _reader.ReadByte();
                var packetHandler = PacketHandlers.GetHandler(packetId);
                if (packetHandler != null) {
                    // LastAction = DateTime.Now;
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (_receiveStream.Length <= 5) {
                            break; //wait for more data
                        }
                        size = _reader.ReadUInt32();
                    }
                    if (_receiveStream.Length >= size) {
                        using var packetReader = new BinaryReader(_receiveStream.Dequeue((int)size));
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
            // LastAction = DateTime.Now;
        }
        catch (Exception e) {
            LogError("ProcessBuffer error");
            Console.WriteLine(e);
            Dispose();
        }
    }
    
    public bool IsConnected
    {
        get
        {
            try {
                if (!_socket.Connected) return false;
                
                if (_socket.Poll(0, SelectMode.SelectRead)) {
                    var buff = new byte[1];
                    return _socket.Receive(buff, SocketFlags.Peek) != 0;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public void Dispose() {
        if (!_socket.Connected) return;
        LogInfo("Disconnecting");

        try {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException e) {
            LogError(e.ToString());
        }
        try {
            _socket.Close();
        }
        catch (SocketException e) {
            LogError(e.ToString());
        }
    }
    
    public void LogInfo(string log) {
        Log("INFO", log);
    }

    public void LogError(string log) {
        Log("ERROR", log);
    }

    public void LogDebug(string log) {
        if (DEBUG) Log("DEBUG", log);
    }

    private void Log(string level, string log) {
        Console.WriteLine($"[{level}] {DateTime.Now} {log}");
    }
}