namespace Cedserver; 

public class Packet {
    private readonly BinaryWriter _stream;

    public BinaryWriter Stream {
        get {
            if (Length == 0) {
                _stream.BaseStream.Position = 1;
                _stream.Write((uint)_stream.BaseStream.Length);
            }

            _stream.BaseStream.Position = 0;
            return _stream;
        }
    }

    public byte PacketId { get; }
    public uint Length { get; }

    public Packet(byte packetId, uint length) {
        _stream = new BinaryWriter(new MemoryStream());
        PacketId = packetId;
        Length = length;
        Stream.Write(packetId);
        if (Length == 0)
            Stream.Write(Length);
    }
}