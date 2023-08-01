using CentrED.Network;
using CentrED.Utility;
using static CentrED.Server.PacketHandlers;

namespace CentrED.Server; 

public class AdminHandling {
    private static PacketHandler<CEDServer>?[] Handlers { get; }

    static AdminHandling() {
        Handlers = new PacketHandler<CEDServer>?[0x100];

        Handlers[0x01] = new PacketHandler<CEDServer>(0, OnFlushPacket);
        Handlers[0x02] = new PacketHandler<CEDServer>(0, OnQuitPacket);
        Handlers[0x05] = new PacketHandler<CEDServer>(0, OnModifyUserPacket);
        Handlers[0x06] = new PacketHandler<CEDServer>(0, OnDeleteUserPacket);
        Handlers[0x07] = new PacketHandler<CEDServer>(0, OnListUsersPacket);
        Handlers[0x08] = new PacketHandler<CEDServer>(0, OnModifyRegionPacket);
        Handlers[0x09] = new PacketHandler<CEDServer>(0, OnDeleteRegionPacket);
        Handlers[0x0A] = new PacketHandler<CEDServer>(0, OnListRegionsPacket);
    }
    
    public static void OnAdminHandlerPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnAdminHandlerPacket");
        if (!ValidateAccess(ns, AccessLevel.Developer)) return;
        var id = reader.ReadByte();
        if (id != 0x01 && !ValidateAccess(ns, AccessLevel.Administrator)) return;
        var packetHandler = Handlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnFlushPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnFlushPacket");
        ns.Parent.Landscape.Flush();
        ns.Parent.Config.Flush();
    }

    private static void OnQuitPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnQuitPacket");
        ns.Parent.Quit = true;
    }
    
    
    private static void OnModifyUserPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnModifyUserPacket");
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var accessLevel = (AccessLevel)reader.ReadByte();
        var regionCount = reader.ReadByte();

        var account = ns.Parent.GetAccount(username);
        if (account != null) {
            if (password != "") {
                account.UpdatePassword(password);
            }

            account.AccessLevel = accessLevel;
            account.Regions.Clear();
            for (int i = 0; i < regionCount; i++) {
                account.Regions.Add(reader.ReadStringNull());
            }
            
            ns.Parent.Config.Invalidate();

            ns.Parent.GetClient(account.Name)?.Send(new AccessChangedPacket(ns));
            ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.Modified, account));
        }
        else {
            if (username == "") {
                ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.InvalidUsername, account));
            }
            else {
                var regions = new List<string>();
                for (int i = 0; i < regionCount; i++) {
                    regions.Add(reader.ReadStringNull());
                }

                account = new Account(username, password, accessLevel, regions);
                ns.Parent.Config.Accounts.Add(account);
                ns.Parent.Config.Invalidate();
                ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.Added, account));
            }
        }
    }

    private static void OnDeleteUserPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnDeleteUserPacket");
        var username = reader.ReadStringNull();
        var account = ns.Parent.GetAccount(username);
        if (account != null && account.Name != ns.Username) {
            ns.Parent.GetClient(account.Name)?.Disconnect();
            ns.Parent.Config.Accounts.Remove(account);
            ns.Parent.Config.Invalidate();
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.Deleted, username));
        }
        else {
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.NotFound, username));
        }
    }

    private static void OnListUsersPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnListUsersPacket");
        ns.Send(new CompressedPacket(new UserListPacket(ns)));
    }

    private static void OnModifyRegionPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnModifyRegionPacket");
        var regionName = reader.ReadStringNull();

        var region = ns.Parent.GetRegion(regionName);
        ModifyRegionStatus status;
        if (region == null) {
            region = new Region(regionName);
            ns.Parent.Config.Regions.Add(region);
            status = ModifyRegionStatus.Added;
        }
        else {
            region.Area.Clear();
            status = ModifyRegionStatus.Modified;
        }

        var areaCount = reader.ReadByte();
        for (int i = 0; i < areaCount; i++) {
            region.Area.Add(new Rect(reader));
        }

        ns.Parent.Config.Invalidate();
        AdminBroadcast(ns, AccessLevel.Administrator, new ModifyRegionResponsePacket(status, region));

        if (status == ModifyRegionStatus.Modified) {
            foreach (var netState in ns.Parent.Clients) {
                var account = ns.Parent.GetAccount(netState.Username)!;
                
                if (account.Regions.Contains(regionName)) {
                    netState.Send(new AccessChangedPacket(ns));
                }
            }
        }
    }

    private static void OnDeleteRegionPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnDeleteRegionPacket");
        var regionName = reader.ReadStringNull();
        var status = DeleteRegionStatus.NotFound;
        var region = ns.Parent.GetRegion(regionName);
        if (region != null) {
            ns.Parent.Config.Regions.Remove(region);
            ns.Parent.Config.Invalidate();
            status = DeleteRegionStatus.Deleted;
        }
        
        AdminBroadcast(ns, AccessLevel.Administrator, new DeleteRegionResponsePacket(status, regionName));
    }

    private static void OnListRegionsPacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.LogDebug("Server OnListRegionsPacket");
        ns.Send(new CompressedPacket(new RegionListPacket(ns)));
    }

    private static void AdminBroadcast(NetState<CEDServer> ns, AccessLevel accessLevel, Packet packet) {
        Logger.LogDebug("AdminBroadcast");
        foreach (var netState in ns.Parent.Clients) {
            if (ns.Parent.GetAccount(netState.Username)!.AccessLevel >= accessLevel) {
                netState.Send(packet);
            }
        }
    }
}