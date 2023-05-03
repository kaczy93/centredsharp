using System.IO.Compression;
using CentrED.Utility;

namespace CentrED.Network; 

public class CompressedPacket : Packet {
    public CompressedPacket(Packet packet) : base(0x01, 0) {
        var compressedData = new MemoryStream();
        using var zLibStream = new ZLibStream(compressedData, CompressionLevel.Optimal); //SmallestSize level seems to be slow
        zLibStream.Write(packet.Compile(out _));
        zLibStream.Dispose();
        Writer.Write((uint)packet.Stream.Length);
        Writer.Write(compressedData.GetBuffer());
    }
}
