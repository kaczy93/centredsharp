namespace CentrED.Network; 

public class PacketHandler<T>  {
    public delegate void PacketProcessor(BinaryReader buffer, NetState<T> ns);
    
    public uint Length { get; }
    
    public PacketProcessor OnReceive { get; }

    public PacketHandler(uint length, PacketProcessor packetProcessor) {
        Length = length;
        OnReceive = packetProcessor;
    }
}