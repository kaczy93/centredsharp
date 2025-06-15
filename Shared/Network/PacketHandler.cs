using System.Buffers;

namespace CentrED.Network;

public class PacketHandler<T> where T : ILogging
{
    public delegate void PacketProcessor(SpanReader reader, NetState<T> ns);

    public uint Length { get; }

    private PacketProcessor _OnReceive { get; }

    public PacketHandler(uint length, PacketProcessor packetProcessor)
    {
        Length = length;
        _OnReceive = packetProcessor;
    }
    
    public void OnReceive(SpanReader reader, NetState<T> ns)
    {
        _OnReceive(reader, ns);
    }
}