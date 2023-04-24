using System.IO.Compression;
using CentrED.Utility;

namespace CentrED.Network; 

public static class PacketHandlers {
    public static void OnCompressedPacket<T>(BinaryReader buffer, NetState<T> ns) {
        ns.LogDebug("OnCompressedPacket");
        var targetSize = (int)buffer.ReadUInt32();
        var uncompBuffer = new GZipStream(buffer.BaseStream, CompressionMode.Decompress);
        var uncompStream = new MemoryStream();
        uncompBuffer.CopyBytesTo(uncompStream, targetSize);
        uncompStream.Position = 0;
        var packetId = uncompStream.ReadByte();
        var handler = ns.PacketHandlers[packetId];
        if (handler != null) {
            if (handler.Length == 0) 
                uncompStream.Position += 4;
            handler.OnReceive(new BinaryReader(uncompStream), ns);
        }
        else {
            ns.LogError($"Dropping client due to unknown packet: {packetId}");
            ns.Dispose();
        }
    }
}