using System.IO.Compression;
using CentrED.Utility;

namespace CentrED.Network; 

public class CompressedPacket : Packet {
    public CompressedPacket(Packet packet) : base(0x01, 0) {
        var compBuffer = new MemoryStream();
        var compStream = new ZLibStream(compBuffer, CompressionLevel.Optimal, true); //SmallestSize level seems to be slow
        compStream.Write(packet.Compile(out _));
        compStream.Close();
        Writer.Write((uint)packet.Stream.Length);
        compBuffer.Seek(0, SeekOrigin.Begin);
        compBuffer.CopyBytesTo(Stream, (int)compBuffer.Length);
    }
}
