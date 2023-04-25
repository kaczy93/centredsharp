using System.IO.Compression;
using CentrED.Utility;

namespace CentrED.Network; 

public static class PacketHandlers {
    public static void OnCompressedPacket<T>(BinaryReader buffer, NetState<T> ns) {
        ns.LogDebug("OnCompressedPacket");
        var targetSize = (int)buffer.ReadUInt32();
        var zLibStream = new ZLibStream(buffer.BaseStream, CompressionMode.Decompress);
        var rawData = new MemoryStream();
        zLibStream.CopyBytesTo(rawData, targetSize);
        rawData.Position = 0;
        using var reader = new BinaryReader(rawData);
        var packetId = reader.ReadByte();
        var handler = ns.PacketHandlers[packetId];
        if (handler != null) {
            var size = handler.Length;
            if (size == 0) {
                size = reader.ReadUInt32();
            }
            handler.OnReceive(reader, ns);
        }
        else {
            ns.LogError($"Dropping client due to unknown packet: {packetId}");
            ns.Dispose();
        }
    }
}