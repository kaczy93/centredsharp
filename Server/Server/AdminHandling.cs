using CentrED.Utility;
using static CentrED.Server.PacketHandlers;

namespace CentrED.Server; 

public class AdminHandling {
    private static PacketHandler?[] AdminHandlers { get; }

    static AdminHandling() {
        AdminHandlers = new PacketHandler?[0x100];

        AdminHandlers[0x01] = new PacketHandler(0, OnFlushPacket);
        AdminHandlers[0x02] = new PacketHandler(0, OnQuitPacket);
        AdminHandlers[0x05] = new PacketHandler(0, OnModifyUserPacket);
        AdminHandlers[0x06] = new PacketHandler(0, OnDeleteUserPacket);
        AdminHandlers[0x07] = new PacketHandler(0, OnListUsersPacket);
        AdminHandlers[0x08] = new PacketHandler(0, OnModifyRegionPacket);
        AdminHandlers[0x09] = new PacketHandler(0, OnDeleteRegionPacket);
        AdminHandlers[0x0A] = new PacketHandler(0, OnListRegionsPacket);
    }
    
    public static void OnAdminHandlerPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnAdminHandlerPacket");
        if (!ValidateAccess(ns, AccessLevel.Developer)) return;
        var id = reader.ReadByte();
        if (id != 0x01 && !ValidateAccess(ns, AccessLevel.Administrator)) return;
        var packetHandler = AdminHandlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnFlushPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnFlushPacket");
        CEDServer.Landscape.Flush();
        Config.Flush();
    }

    private static void OnQuitPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnQuitPacket");
        CEDServer.Quit = true;
    }
    
    
    private static void OnModifyUserPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnModifyUserPacket");
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var accessLevel = (AccessLevel)reader.ReadByte();
        var regionCount = reader.ReadByte();

        var account = Config.Accounts.Find(x => x.Name == username);
        if (account != null) {
            if (password != "") {
                account.UpdatePassword(password);
            }

            account.AccessLevel = accessLevel;
            account.Regions.Clear();
            for (int i = 0; i < regionCount; i++) {
                account.Regions.Add(reader.ReadStringNull());
            }

            Config.Invalidate();

            foreach (var netState in CEDServer.Clients) {
                if (netState.Account == account) {
                    ns.Send(new AccessChangedPacket(account));
                } 
            }
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
                Config.Accounts.Add(account);
                Config.Invalidate();
                ns.Send(new ModifyUserResponsePacket(ModifyUserStatus.Added, account));
            }
        }
    }

    private static void OnDeleteUserPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnDeleteUserPacket");
        var username = reader.ReadStringNull();
        var account = Config.Accounts.Find(u => u.Name == username);
        if (account != null && account != ns.Account) {
            foreach (var netState in CEDServer.Clients) {
                if (netState.Account == account) {
                    netState.Dispose();
                }
            }
            Config.Accounts.Remove(account);
            Config.Invalidate();
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.Deleted, username));
        }
        else {
            ns.Send(new DeleteUserResponsePacket(DeleteUserStatus.NotFound, username));
        }
    }

    private static void OnListUsersPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnListUsersPacket");
        ns.Send(new CompressedPacket(new UserListPacket()));
    }

    private static void OnModifyRegionPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnModifyRegionPacket");
        var regionName = reader.ReadStringNull();

        var region = Config.Regions.Find(x => x.Name == regionName);
        ModifyRegionStatus status;
        if (region == null) {
            region = new Region(regionName);
            Config.Regions.Add(region);
            status = ModifyRegionStatus.Added;
        }
        else {
            region.Area.Clear();
            status = ModifyRegionStatus.Modified;
        }

        var areaCount = reader.ReadByte();
        for (int i = 0; i < areaCount; i++) {
            var x1 = reader.ReadUInt16();
            var y1 = reader.ReadUInt16();
            var x2 = reader.ReadUInt16();
            var y2 = reader.ReadUInt16();
            region.Area.Add(new Rect(x1,y1,x2,y2));
        }

        Config.Invalidate();
        AdminBroadcast(AccessLevel.Administrator, new ModifyRegionResponsePacket(status, region));

        if (status == ModifyRegionStatus.Modified) {
            foreach (var netState in CEDServer.Clients) {
                var account = netState.Account;
                
                if (account.Regions.Contains(regionName)) {
                    netState.Send(new AccessChangedPacket(account));
                }
            }
        }
    }

    private static void OnDeleteRegionPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnDeleteRegionPacket");
        var regionName = reader.ReadStringNull();
        var status = DeleteRegionStatus.NotFound;
        var region = Config.Regions.Find(r => r.Name == regionName);
        if (region != null) {
            Config.Regions.Remove(region);
            Config.Invalidate();
            status = DeleteRegionStatus.Deleted;
        }
        
        AdminBroadcast(AccessLevel.Administrator, new DeleteRegionResponsePacket(status, regionName));
    }

    private static void OnListRegionsPacket(BinaryReader reader, NetState ns) {
        ns.LogDebug("OnListRegionsPacket");
        ns.Send(new CompressedPacket(new RegionListPacket()));
    }

    private static void AdminBroadcast(AccessLevel accessLevel, Packet packet) {
        CEDServer.LogDebug("AdminBroadcast");
        foreach (var ns in CEDServer.Clients) {
            if (ns.Account.AccessLevel >= accessLevel) {
                ns.Send(packet);
            }
        }
    }
}