using System.Buffers;
using System.IO.Compression;
using Server.Buffers;

namespace CentrED.Network;

public static class Zlib
{
    public static void SendCompressed<T>(this NetState<T> ns, Packet packet) where T : ILogging
    {
        SendCompressed(ns, packet.Compile());
    }
    
    public static void SendCompressed<T>(this NetState<T> ns, ReadOnlySpan<byte> data) where T : ILogging
    {
        using var compressedStream = new MemoryStream();
        using var zLibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal);
        zLibStream.Write(data);
        zLibStream.Flush();

        var packetLength = (int)compressedStream.Length + 9;
        var buffer = STArrayPool<byte>.Shared.Rent(packetLength);
        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x01);          // PacketId
        writer.Write((uint)packetLength);  // PacketLength
        writer.Write((uint)data.Length);   // DeCompressedLength;
        
        compressedStream.Seek(0, SeekOrigin.Begin);
        var compressedLength = compressedStream.Read(writer.RawBuffer[writer.Position..]);
    
        ns.Send(buffer[..packetLength]);
        STArrayPool<byte>.Shared.Return(buffer);
    }
    
    public static unsafe void OnCompressedPacket<T>(SpanReader reader, NetState<T> ns) where T : ILogging
    {
        ns.LogDebug("OnCompressedPacket");
        var decompressedSize = (int)reader.ReadUInt32();
        var compressedData = reader.Buffer[reader.Position..];
        var decompressedData = STArrayPool<byte>.Shared.Rent(decompressedSize);
        fixed (byte* compressedPtr = &compressedData[0])
        {
            using var inputStream = new UnmanagedMemoryStream(compressedPtr, compressedData.Length);
            using var zLibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
            zLibStream.ReadExactly(decompressedData, 0, decompressedSize);
        }
        var rawReader = new SpanReader(decompressedData.AsSpan()[..decompressedSize]);
        var packetId = rawReader.ReadByte();
        var handler = ns.PacketHandlers[packetId];
        if (handler != null)
        {
            if (handler.Length == 0)
            {
                rawReader.ReadUInt32(); // skip packet length
            }
            handler.OnReceive(rawReader, ns);
        }
        else
        {
            ns.LogError($"Dropping client due to unknown packet: {packetId}");
            ns.Disconnect();
        }
        STArrayPool<byte>.Shared.Return(decompressedData);
    }
}