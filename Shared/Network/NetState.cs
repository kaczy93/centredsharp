using System.Net.Sockets;
using CentrED.Utility;

namespace CentrED.Network;

public class NetState<T> : IDisposable where T : ILogging
{
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
    public bool Active => LastAction > DateTime.Now - TimeSpan.FromMinutes(2);
    private const int RECV_BUFFER_CAPACITY = 65536;

    public NetState(T parent, Socket socket, PacketHandler<T>?[] packetHandlers)
    {
        Parent = parent;
        _socket = socket;
        PacketHandlers = packetHandlers;

        _recvStream = new MemoryStream(socket.ReceiveBufferSize);
        _recvBuffer = new byte[RECV_BUFFER_CAPACITY];
        _recvReader = new BinaryReader(_recvStream);

        _sendStream = new MemoryStream(socket.SendBufferSize);

        Username = "";
        LastAction = DateTime.Now;
    }

    public bool Receive()
    {
        try
        {
            if (PollAndPeek())
            {
                if (_socket.Available > 0)
                {
                    var bytesRead = _socket.Receive(_recvBuffer, SocketFlags.None);
                    if (bytesRead > 0)
                    {
                        _recvStream.Seek(0, SeekOrigin.End);
                        _recvStream.Write(_recvBuffer, 0, bytesRead);
                        ProcessBuffer();
                    }
                }
            }
            else
            {
                Disconnect();
            }
        }
        catch (Exception e)
        {
            LogError("Receive error");
            Console.WriteLine(e);
            Disconnect();
        }

        return Running;
    }

    private void ProcessBuffer()
    {
        try
        {
            _recvStream.Position = 0;
            while (_recvStream.Length >= 1)
            {
                var packetId = _recvReader.ReadByte();
                var packetHandler = PacketHandlers[packetId];
                if (packetHandler != null)
                {
                    var size = packetHandler.Length;
                    if (size == 0)
                    {
                        if (_recvStream.Length <= 5)
                        {
                            break; //wait for more data
                        }
                        size = _recvReader.ReadUInt32();
                    }
                    if (_recvStream.Length >= size)
                    {
                        var buffer = _recvStream.Dequeue((int)_recvStream.Position, (int)(size - _recvStream.Position));
                        using var packetReader = new BinaryReader(new MemoryStream(buffer));
                        packetHandler.OnReceive(packetReader, this);
                    }
                    else
                    {
                        break; //wait for more data
                    }
                }
                else
                {
                    LogError($"Unknown packet: {packetId}");
                    Disconnect();
                }
            }
            LastAction = DateTime.Now;
        }
        catch (Exception e)
        {
            LogError("ProcessBuffer error");
            Console.WriteLine(e);
            Disconnect();
        }
    }

    public void Send(Packet packet)
    {
        try
        {
            _sendStream.Write(packet.Compile(out _));
        }
        catch (Exception e)
        {
            LogError("Send Error");
            Console.WriteLine(e);
            Disconnect();
        }

        FlushPending = true;
    }

    public bool Flush()
    {
        try
        {
            _sendStream.Position = 0;
            if (_sendStream.Length > 0)
            {
                var buffer = new byte[_sendStream.Length];
                var bytesCount = _sendStream.Read(buffer);
                var bytesSent = _socket.Send(buffer, 0, bytesCount, SocketFlags.None);
                _sendStream.Dequeue(0, bytesSent);
                if (_sendStream.Length == 0)
                {
                    FlushPending = false;
                }
            }
            LastAction = DateTime.Now;
        }
        catch (Exception e)
        {
            LogError("Flush Error");
            Console.WriteLine(e);
            Disconnect();
        }

        return Running;
    }

    private bool PollAndPeek()
    {
        try
        {
            if (!_socket.Connected)
                return false;
            if (!_socket.Poll(0, SelectMode.SelectRead))
                return true;

            var buff = new byte[1];
            return _socket.Receive(buff, SocketFlags.Peek) != 0;
        }
        catch
        {
            return false;
        }
    }

    public void Disconnect()
    {
        Running = false;
    }

    public void Dispose()
    {
        Disconnect();
        if (!_socket.Connected)
            return;
        LogInfo("Disconnecting");
        Username = "";

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException e)
        {
            LogError(e.ToString());
        }

        try
        {
            _socket.Close();
        }
        catch (SocketException e)
        {
            LogError(e.ToString());
        }
    }

    public void LogInfo(string log)
    {
        Parent._logger.LogInfo(LogMessage(log));
    }

    public void LogError(string log)
    {
        Parent._logger.LogError(LogMessage(log));
    }

    public void LogDebug(string log)
    {
        Parent._logger.LogDebug(LogMessage(log));
    }

    private string LogMessage(string log)
    {
        string endpoint;
        try
        {
            endpoint = _socket.RemoteEndPoint!.ToString()!;
        }
        catch (Exception)
        {
            endpoint = "";
        }
        return $"{Username}@{endpoint} {log}";
    }
}