namespace CentrED.Network;

public class Packet
{
    public Stream Stream { get; }
    public BinaryWriter Writer { get; }
    private byte PacketId { get; }
    private uint Length { get; }

    public Packet(byte packetId, uint length)
    {
        Stream = new MemoryStream();
        Writer = new BinaryWriter(Stream);
        PacketId = packetId;
        Length = length;
        Writer.Write(packetId);
        if (Length == 0)
            Writer.Write(Length);
    }

    public byte[] Compile(out int length)
    {
        if (Length == 0)
        {
            Writer.Seek(1, SeekOrigin.Begin);
            Writer.Write((uint)Stream.Length);
        }
        Writer.Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[Stream.Length];
        length = Stream.Read(buffer);
        return buffer;
    }
}