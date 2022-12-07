using Server;

namespace Cedserver; 

public class Packet {

    public Stream Stream { get; }
    public BinaryWriter Writer { get; }
    public byte PacketId { get; }
    public uint Length { get; }

    public Packet(byte packetId, uint length) {
        Stream = new MemoryStream();
        Writer = new BinaryWriter(Stream);
        PacketId = packetId;
        Length = length;
        Writer.Write(packetId);
        Writer.Write(Length);
    }

    public virtual int Write(Stream targetStream) {
        if(CEDServer.DEBUG) Console.WriteLine($"Writing packet {PacketId} {GetType().Name}");
        if (Length == 0) {
            Writer.Seek(1, SeekOrigin.Begin);
            Writer.Write((uint)Writer.BaseStream.Length);
        }
        Writer.Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[Stream.Length];
        var packetBytes = Stream.Read(buffer);
        targetStream.Write(buffer);
        return packetBytes;
    }
}