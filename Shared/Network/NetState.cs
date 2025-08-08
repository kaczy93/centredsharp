using System.Buffers;
using System.Net.Sockets;

namespace CentrED.Network;

public class NetState<T> : IDisposable, ILogging where T : ILogging
{
    private readonly Socket _socket;
    protected internal PacketHandler<T>?[] PacketHandlers { get; }

    protected Pipe RecvPipe;
    protected Pipe SendPipe;

    public ProtocolVersion ProtocolVersion { get; set; }
    public T Parent { get; }
    public String Username { get; set; }
    public DateTime LastAction { get; set; }
    public bool Running { get; private set; } = true;
    public bool FlushPending => SendPipe.Reader.AvailableToRead().Length > 0;
    public bool Active => LastAction > DateTime.UtcNow - TimeSpan.FromMinutes(2);
    
    private const uint DefaultPipeSize = 1024 * 64;

    public NetState(T parent, Socket socket, uint recvPipeSize = DefaultPipeSize, uint sendPipeSize = DefaultPipeSize)
    {
        Parent = parent;
        _socket = socket;
        PacketHandlers = new PacketHandler<T>[0x100];
        
        RecvPipe = new Pipe(recvPipeSize);
        SendPipe = new Pipe(sendPipeSize);

        Username = "";
        LastAction = DateTime.UtcNow;
    }
    
    public void RegisterPacketHandler(byte packetId, uint length, PacketHandler<T>.PacketProcessor handler)
    {
        if (PacketHandlers[packetId] != null)
            throw new Exception($"Packet {packetId} already registered");
        PacketHandlers[packetId] = new PacketHandler<T>(length, handler);
    }

    public bool Receive()
    {
        try
        {
            if (PollAndPeek())
            {
                if (_socket.Available > 0)
                {
                    var recvWriter = RecvPipe.Writer;
                    if (recvWriter.IsClosed)
                        return false;
                    
                    var buffer = recvWriter.AvailableToWrite();
                    if(buffer.Length == 0)
                        return true;
                    
                    var bytesRead = _socket.Receive(buffer, SocketFlags.None);
                    if (bytesRead > 0)
                    {
                        recvWriter.Advance((uint)bytesRead);
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

    protected void ProcessBuffer()
    {
        try
        {
            while (Running)
            {
                var reader = RecvPipe.Reader;
                var buffer = reader.AvailableToRead();
                if (buffer.Length <= 0)
                {
                    break;
                }
                var bufferReader = new SpanReader(buffer);
                var packetId = bufferReader.ReadByte();
                var packetHandler = PacketHandlers[packetId];
                if (packetHandler != null)
                {
                    var packetLength = packetHandler.Length;
                    if (packetLength == 0)
                    {
                        if (bufferReader.Remaining < 5)
                        {
                            break; //wait for more data
                        }
                        packetLength = bufferReader.ReadUInt32();
                    }
                    if (bufferReader.Length >= packetLength)
                    {
                        var data = buffer.Slice(bufferReader.Position, (int)(packetLength - bufferReader.Position));
                        var packetReader = new SpanReader(data);
                        packetHandler.OnReceive(packetReader, this);
                        reader.Advance(packetLength);
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
            LastAction = DateTime.UtcNow;
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
        Send(packet.Compile());
    }
    
    public void Send(ReadOnlySpan<byte> data)
    {
        try
        {
            var sendWriter = SendPipe.Writer;
            var buffer = sendWriter.AvailableToWrite();
            if (buffer.Length < data.Length)
            {
                Flush();
                buffer = sendWriter.AvailableToWrite();
            }
            data.CopyTo(buffer);
            sendWriter.Advance((uint)data.Length);
        }
        catch (Exception e)
        {
            LogError("Send Error");
            Console.WriteLine(e);
            Disconnect();
        }
    }

    public bool Flush()
    {
        try
        {
            var reader = SendPipe.Reader;
            var buffer = reader.AvailableToRead();
            if (buffer.Length > 0)
            {
                var bytesSent = _socket.Send(buffer, SocketFlags.None);
                reader.Advance((uint)bytesSent);
            }
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
        RecvPipe.Dispose();
        SendPipe.Dispose();
    }

    public void LogInfo(string message)
    {
        Parent.LogInfo(Format(message));
    }

    public void LogWarn(string message)
    {
        Parent.LogWarn(Format(message));
    }

    public void LogError(string message)
    {
        Parent.LogError(Format(message));
    }

    public void LogDebug(string message)
    {
#if DEBUG
        Parent.LogDebug(Format(message));
#endif
    }

    private string Format(string message)
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
        return $"{Username}@{endpoint} {message}";
    }
}
