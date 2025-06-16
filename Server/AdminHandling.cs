using System.Buffers;
using CentrED.Network;
using CentrED.Server.Config;
using static CentrED.Server.PacketHandlers;

namespace CentrED.Server;

public class AdminHandling
{
    private static PacketHandler<CEDServer>?[] Handlers { get; }

    static AdminHandling()
    {
        Handlers = new PacketHandler<CEDServer>?[0x100];

        Handlers[0x01] = new PacketHandler<CEDServer>(0, OnFlushPacket);
        Handlers[0x02] = new PacketHandler<CEDServer>(0, OnShutdownPacket);
        Handlers[0x05] = new PacketHandler<CEDServer>(0, OnModifyUserPacket);
        Handlers[0x06] = new PacketHandler<CEDServer>(0, OnDeleteUserPacket);
        Handlers[0x07] = new PacketHandler<CEDServer>(0, OnListUsersPacket);
        Handlers[0x08] = new PacketHandler<CEDServer>(0, OnModifyRegionPacket);
        Handlers[0x09] = new PacketHandler<CEDServer>(0, OnDeleteRegionPacket);
        Handlers[0x0A] = new PacketHandler<CEDServer>(0, OnListRegionsPacket);
        Handlers[0x10] = new PacketHandler<CEDServer>(0, OnServerCpuIdlePacket);
    }

    public static void OnAdminHandlerPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnAdminHandlerPacket");
        if (!ValidateAccess(ns, AccessLevel.Developer))
            return;
        var id = reader.ReadByte();
        if (id != 0x01 && id != 0x10 && !ValidateAccess(ns, AccessLevel.Administrator))
            return;
        var packetHandler = Handlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnFlushPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnFlushPacket");
        ns.Parent.Landscape.Flush();
        ns.Parent.Config.Flush();
    }

    private static void OnShutdownPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnShutdownPacket");
        ns.Parent.Quit = true;
    }

    private static void OnModifyUserPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnModifyUserPacket");
        var username = reader.ReadString();
        var password = reader.ReadString();
        var accessLevel = (AccessLevel)reader.ReadByte();
        var regionCount = reader.ReadByte();

        var account = ns.Parent.GetAccount(username);
        if (account != null)
        {
            if (password != "")
            {
                account.UpdatePassword(password);
            }

            account.AccessLevel = accessLevel;
            account.Regions.Clear();
            for (int i = 0; i < regionCount; i++)
            {
                account.Regions.Add(reader.ReadString());
            }

            ns.Parent.Config.Invalidate();

            ns.Parent.GetClient(account.Name)?.Send(new AccessChangedPacket(ns));
            ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.Modified, account));
        }
        else
        {
            if (username == "")
            {
                ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.InvalidUsername, account));
            }
            else
            {
                var regions = new List<string>();
                for (int i = 0; i < regionCount; i++)
                {
                    regions.Add(reader.ReadString());
                }

                account = new Account(username, password, accessLevel, regions);
                ns.Parent.Config.Accounts.Add(account);
                ns.Parent.Config.Invalidate();
                ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.Added, account));
            }
        }
    }

    private static void OnDeleteUserPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnDeleteUserPacket");
        var username = reader.ReadString();
        var account = ns.Parent.GetAccount(username);
        if (account != null && account.Name != ns.Username)
        {
            ns.Parent.GetClient(account.Name)?.Disconnect();
            ns.Parent.Config.Accounts.Remove(account);
            ns.Parent.Config.Invalidate();
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.Deleted, username));
        }
        else
        {
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.NotFound, username));
        }
    }

    private static void OnListUsersPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnListUsersPacket");
        ns.SendCompressed(new UserListPacket(ns));
    }

    private static void OnModifyRegionPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnModifyRegionPacket");
        var regionName = reader.ReadString();

        var region = ns.Parent.GetRegion(regionName);
        ModifyRegionStatus status;
        if (region == null)
        {
            region = new Region(regionName);
            ns.Parent.Config.Regions.Add(region);
            status = ModifyRegionStatus.Added;
        }
        else
        {
            region.Area.Clear();
            status = ModifyRegionStatus.Modified;
        }

        var areaCount = reader.ReadByte();
        for (int i = 0; i < areaCount; i++)
        {
            region.Area.Add(reader.ReadRect());
        }

        ns.Parent.Config.Invalidate();
        AdminBroadcast(ns, AccessLevel.Administrator, new ModifyRegionResponsePacket(status, region));

        if (status == ModifyRegionStatus.Modified)
        {
            foreach (var netState in ns.Parent.Clients)
            {
                var account = ns.Parent.GetAccount(netState.Username)!;

                if (account.Regions.Contains(regionName))
                {
                    netState.Send(new AccessChangedPacket(ns));
                }
            }
        }
    }

    private static void OnDeleteRegionPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnDeleteRegionPacket");
        var regionName = reader.ReadString();
        var status = DeleteRegionStatus.NotFound;
        var region = ns.Parent.GetRegion(regionName);
        if (region != null)
        {
            ns.Parent.Config.Regions.Remove(region);
            ns.Parent.Config.Invalidate();
            status = DeleteRegionStatus.Deleted;
        }

        AdminBroadcast(ns, AccessLevel.Administrator, new DeleteRegionResponsePacket(status, regionName));
    }

    private static void OnServerCpuIdlePacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnServerTurboPacket");
        var enabled = reader.ReadBoolean();
        ns.Parent.SetCPUIdle(ns, enabled);
    }

    private static void OnListRegionsPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnListRegionsPacket");
        ns.SendCompressed(new RegionListPacket(ns));
    }

    private static void AdminBroadcast(NetState<CEDServer> ns, AccessLevel accessLevel, Packet packet)
    {
        ns.LogDebug("AdminBroadcast");
        foreach (var netState in ns.Parent.Clients)
        {
            if (ns.Parent.GetAccount(netState.Username)!.AccessLevel >= accessLevel)
            {
                netState.Send(packet);
            }
        }
    }
}