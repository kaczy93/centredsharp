namespace CentrED.Network;

public class PacketHandler<T> where T : ILogging
{
    public delegate void PacketProcessor(BinaryReader reader, NetState<T> ns);

    public uint Length { get; }

    public PacketProcessor OnReceive { get; }

    public PacketHandler(uint length, PacketProcessor packetProcessor)
    {
        Length = length;
        OnReceive = packetProcessor;
    }
}