using CentrED.Network;
using static CentrED.Network.PacketHandlers;

namespace CentrED.Server;

public static class PacketHandlers
{
    public static PacketHandler<CEDServer>?[] Handlers { get; }

    static PacketHandlers()
    {
        Handlers = new PacketHandler<CEDServer>?[0x100];

        RegisterPacketHandler(0x01, 0, OnCompressedPacket);
        RegisterPacketHandler(0x02, 0, ConnectionHandling.OnConnectionHandlerPacket);
        RegisterPacketHandler(0x03, 0, AdminHandling.OnAdminHandlerPacket);
        RegisterPacketHandler(0x04, 0, OnRequestBlocksPacket);
        RegisterPacketHandler(0x05, 5, OnFreeBlockPacket);
        //0x06-0x0B handled by landscape 
        RegisterPacketHandler(0x0C, 0, ClientHandling.OnClientHandlerPacket);
        //0x0D handled by radarmap
        //0x0E handled by landscape 
        RegisterPacketHandler(0xFF, 1, OnNoOpPacket);
    }

    public static void RegisterPacketHandler
        (int packetId, uint length, PacketHandler<CEDServer>.PacketProcessor packetProcessor)
    {
        Handlers[packetId] = new PacketHandler<CEDServer>(length, packetProcessor);
    }

    public static bool ValidateAccess(NetState<CEDServer> ns, AccessLevel accessLevel)
    {
        return ns.AccessLevel() >= accessLevel;
    }

    public static bool ValidateAccess(NetState<CEDServer> ns, AccessLevel accessLevel, uint x, uint y)
    {
        if (!ValidateAccess(ns, accessLevel))
            return false;
        var account = ns.Parent.GetAccount(ns.Username)!;
        if (account.Regions.Count == 0 || ns.AccessLevel() >= AccessLevel.Administrator)
            return true;

        foreach (var regionName in account.Regions)
        {
            var region = ns.Parent.GetRegion(regionName);
            if (region != null && region.Area.Any(a => a.Contains(x, y)))
                return true;
        }
        return false;
    }

    private static void OnRequestBlocksPacket(BinaryReader buffer, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnRequestBlocksPacket");
        if (!ValidateAccess(ns, AccessLevel.View))
            return;
        var blocksCount = (buffer.BaseStream.Length - buffer.BaseStream.Position) / 4; // x and y, both 2 bytes
        var blocks = new BlockCoords[blocksCount];
        for (var i = 0; i < blocksCount; i++)
        {
            blocks[i] = new BlockCoords(buffer);
            ns.LogDebug($"Requested x={blocks[i].X} y={blocks[i].Y}");
        }

        ns.Send(new CompressedPacket(new BlockPacket(new List<BlockCoords>(blocks), ns, true)));
    }

    private static void OnFreeBlockPacket(BinaryReader buffer, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnFreeBlockPacket");
        if (!ValidateAccess(ns, AccessLevel.View))
            return;
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        var subscriptions = ns.Parent.Landscape.GetBlockSubscriptions(x, y);
        subscriptions.Remove(ns);
    }

    private static void OnNoOpPacket(BinaryReader buffer, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnNoOpPacket");
    }
}