namespace Server; 

public class PacketHandler {
    public delegate void PacketProcessor(BinaryReader buffer, NetState netState);
    
    public uint Length { get; }
    
    public PacketProcessor OnReceive { get; }

    public PacketHandler(uint length, PacketProcessor packetProcessor) {
        Length = length;
        OnReceive = packetProcessor;
    }
}