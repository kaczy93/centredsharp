using System.IO.Compression;

namespace CentrED.Network;

public class CompressedPacket : Packet
{
    public CompressedPacket(Packet packet) : base(0x01, 0)
    {
        var compressedData = new MemoryStream();
        using var zLibStream = new ZLibStream
            (compressedData, CompressionLevel.Optimal); //SmallestSize level seems to be slow
        var bytes = packet.Compile(out var length);
        zLibStream.Write(bytes);
        zLibStream.Flush();
        zLibStream.Close();
        Writer.Write((uint)packet.Stream.Length);
        Writer.Write(compressedData.GetBuffer());
    }
}