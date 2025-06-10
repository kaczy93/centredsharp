using System.IO;
using CentrED.Network;

namespace CentrED.Client;

public static class AckHandling
{
    public static void OnMapAckPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnMapAckPacket");
        ns.Parent.AwaitingAck = false;
    }
}
