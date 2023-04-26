using System.Net.Sockets;
using CentrED.Utility;

namespace CentrED.Network; 

public class NetState<T> {
    private readonly Socket _socket;
    internal PacketHandler<T>?[] PacketHandlers { get; }

    private byte[] _recvBuffer;
    private MemoryStream _recvStream;
    private MemoryStream _sendStream;

    private BinaryReader _recvReader;
    public ProtocolVersion ProtocolVersion { get; set; }
    public T Parent { get; }
    public String Username { get; set; }
    public DateTime LastAction { get; set; }
    public bool Running { get; private set; } = true;
    public bool FlushPending { get; private set; } = false;
    
    public NetState(T parent, Socket socket, PacketHandler<T>?[] packetHandlers) {
        Parent = parent;
        _socket = socket;
        PacketHandlers = packetHandlers;
        
        _recvStream = new MemoryStream(socket.ReceiveBufferSize);
        _recvBuffer = new byte[_recvStream.Capacity];
        _recvReader = new BinaryReader(_recvStream);
        
        _sendStream = new MemoryStream(socket.SendBufferSize);
        
        Username = "";
        LastAction = DateTime.Now;
    }
    
    public bool Receive() {
        if (!Running) return false;
        
        try {
            if (_socket.Poll(0, SelectMode.SelectRead)) {
                var bytesRead = _socket.Receive(_recvBuffer, SocketFlags.None);
                if (bytesRead > 0) {
                    _recvStream.Write(_recvBuffer, 0, bytesRead);
                    _recvBuffer = new byte[_recvStream.Capacity];
                    ProcessBuffer();
                }
            }
        }
        catch (Exception e) {
            LogError("Receive error");
            Console.WriteLine(e);
            Dispose();
            return false;
        }

        return true;
    }
    
    private void ProcessBuffer() {
        try {
            _recvStream.Position = 0;
            while (_recvStream.Length >= 1) {
                var packetId = _recvReader.ReadByte();
                var packetHandler = PacketHandlers[packetId];
                if (packetHandler != null) {
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (_recvStream.Length <= 5) {
                            break; //wait for more data
                        }
                        size = _recvReader.ReadUInt32();
                    }
                    if (_recvStream.Length >= size) {
                        var buffer = _recvStream.Dequeue((int)_recvStream.Position, (int)(size - _recvStream.Position));
                        using var packetReader = new BinaryReader(new MemoryStream(buffer));
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
    
    public void Send(Packet packet) {
        try
        {
            _sendStream.Write(packet.Compile(out _));
        }
        catch (Exception e)
        {
            LogError("Send Error");
            Console.WriteLine(e);
            Dispose();
        }

        if (!FlushPending) {
            FlushPending = true;
        }
    }

    public bool Flush() {
        if (!Running) return false;
        
        try {
            _sendStream.Position = 0;
            var buffer = new byte[_sendStream.Length];
            var bytesCount = _sendStream.Read(buffer);
            var bytesSent = _socket.Send(buffer, 0, bytesCount, SocketFlags.None);
            _sendStream.Dequeue(0, bytesSent);
            if (_sendStream.Length == 0) {
                FlushPending = false;
            }
        }
        catch (Exception e)
        {
            LogError("Flush Error");
            Console.WriteLine(e);
            Dispose();
            return false;
        }

        return true;
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
        Running = false;

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
        Logger.LogInfo(LogMessage(log));
    }

    public void LogError(string log) {
        Logger.LogError(LogMessage(log));
    }

    public void LogDebug(string log) {
        Logger.LogDebug(LogMessage(log));
    }

    private string LogMessage(string log) {
        string endpoint;
        try {
            endpoint = _socket.RemoteEndPoint!.ToString()!;
        }
        catch (Exception) {
            endpoint = "";
        }
        return $"{Username}@{endpoint} {log}";
    }
}