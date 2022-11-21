using Server;
using Shared;
using static Cedserver.ClientHandling;
using static Server.PacketHandlers;

namespace Cedserver; 

public class AdminHandling {
    public static PacketHandler[] AdminHandlers { get; }

    static AdminHandling() {
        AdminHandlers = new PacketHandler[0x100];

        AdminHandlers[0x01] = new PacketHandler(0, OnFlushPacket);
        AdminHandlers[0x02] = new PacketHandler(0, OnQuitPacket);
        AdminHandlers[0x05] = new PacketHandler(0, OnModifyUserPacket);
        AdminHandlers[0x06] = new PacketHandler(0, OnDeleteUserPacket);
        AdminHandlers[0x07] = new PacketHandler(0, OnListUsersPacket);
        AdminHandlers[0x08] = new PacketHandler(0, OnModifyRegionPacket);
        AdminHandlers[0x09] = new PacketHandler(0, OnDeleteRegionPacket);
        AdminHandlers[0x0A] = new PacketHandler(0, OnListRegionsPacket);
    }
    
    public class ModifyUserResponsePacket : Packet {
        public ModifyUserResponsePacket(ModifyUserStatus status, Account account) : base(0x03, 0) {
            Stream.Write((byte)0x05);
            Stream.Write((byte)status);
            Stream.WriteStringNull(account.Name);
            if (status == ModifyUserStatus.Added || status == ModifyUserStatus.Modified) {
                Stream.Write((byte)account.AccessLevel);
                Stream.Write(account.Regions.Count);
                foreach (var regionName in account.Regions) {
                    Stream.WriteStringNull(regionName);
                }
            }
            //TODO: Check for client side modifications
        }
    }
    
    public class DeleteUserResponsePacket : Packet {
        public DeleteUserResponsePacket(DeleteUserStatus status, string username) : base(0x03, 0) {
            Stream.Write((byte)0x06);
            Stream.Write((byte)status);
            Stream.WriteStringNull(username);
        }
    }
    
    public class UserListPacket : Packet {
        public UserListPacket() : base(0x03, 0) {
            Stream.Write((byte)0x07);
            Stream.Write((ushort)Config.Accounts.Count);
            foreach (var account in Config.Accounts) {
                Stream.WriteStringNull(account.Name);
                Stream.Write((byte)account.AccessLevel);
                Stream.Write((byte)account.Regions.Count);
                foreach (var region in account.Regions) {
                    Stream.WriteStringNull(region);
                }
            }
        }
    }
    
    public class ModifyRegionResponsePacket : Packet {
        public ModifyRegionResponsePacket(ModifyRegionStatus status, Region region) : base(0x03, 0) {
            Stream.Write((byte)0x08);
            Stream.Write((byte)status);
            Stream.WriteStringNull(region.Name);
            if (status == ModifyRegionStatus.Added || status == ModifyRegionStatus.Modified) {
                Stream.Write(region.Area.Count);
                foreach (var rect in region.Area) {
                    Stream.Write(rect.X1);
                    Stream.Write(rect.Y1);
                    Stream.Write(rect.X2);
                    Stream.Write(rect.Y2);
                }
            }
        }
    }
    
    public class DeleteRegionResponsePacket : Packet {
        public DeleteRegionResponsePacket(DeleteRegionStatus status, string regionName) : base(0x03, 0) {
            Stream.Write((byte)0x09);
            Stream.Write((byte)status);
            Stream.WriteStringNull(regionName);
        }
    }
    
    public class RegionListPacket : Packet {
        public RegionListPacket() : base(0x03, 0)  {
            Stream.Write((byte)0x0A);
            Stream.Write((byte)Config.Regions.Count);
            foreach (var region in Config.Regions) {
                Stream.WriteStringNull(region.Name);
                Stream.Write((byte)region.Area.Count);
                foreach (var rect in region.Area) {
                    Stream.Write(rect.X1);
                    Stream.Write(rect.Y1);
                    Stream.Write(rect.X2);
                    Stream.Write(rect.Y2);
                }
            }
        }
    }

    public static void OnAdminHandlerPacket(BinaryReader reader, NetState ns) {
        if (ValidateAccess(ns, AccessLevel.Administrator)) return;
        var packetHandler = AdminHandlers[reader.ReadByte()];
        if (packetHandler != null) {
            packetHandler.OnReceive(reader, ns);
        }
    }

    public static void OnFlushPacket(BinaryReader reader, NetState ns) {
        CEDServer.Landscape.Flush();
        Config.Flush();
    }

    public static void OnQuitPacket(BinaryReader reader, NetState ns) {
        CEDServer.Quit = true;
    }
    
    
    public static void OnModifyUserPacket(BinaryReader reader, NetState ns) {
        var username = reader.ReadStringNull();
        var password = reader.ReadStringNull();
        var accessLevel = (AccessLevel)reader.ReadByte();
        var regionCount = reader.ReadByte();

        var account = Cedserver.Config.Accounts.Find(x => x.Name == username);
        if (account != null) {
            if (password != "") {
                account.UpdatePassword(password);
            }

            account.AccessLevel = accessLevel;
            account.Regions.Clear();
            for (int i = 0; i < regionCount; i++) {
                account.Regions.Add(reader.ReadStringNull());
            }

            account.Invalidate();

            foreach (var netState in CEDServer.Clients) {
                if (netState != null && netState.Account == account) {
                    CEDServer.SendPacket(ns, new AccessChangedPacket(account));
                } 
            }
            CEDServer.SendPacket(ns, new ModifyUserResponsePacket(ModifyUserStatus.Modified, account));
        }
        else {
            if (username == "") {
                CEDServer.SendPacket(ns, new ModifyUserResponsePacket(ModifyUserStatus.InvalidUsername, account));
            }
            else {
                var regions = new List<string>();
                for (int i = 0; i < regionCount; i++) {
                    regions.Add(reader.ReadStringNull());
                }

                account = new Account(Cedserver.Config.Accounts, username, password, accessLevel, regions);
                Config.Accounts.Add(account);
                Config.Accounts.Invalidate();
                CEDServer.SendPacket(ns, new ModifyUserResponsePacket(ModifyUserStatus.Added, account));
            }
        }
    }

    public static void OnDeleteUserPacket(BinaryReader reader, NetState ns) {
        var username = reader.ReadStringNull();
        var account = Cedserver.Config.Accounts.Find(u => u.Name == username);
        if (account != null && account != ns.Account) {
            foreach (var netState in CEDServer.Clients) {
                if (netState != null && netState.Account == account) {
                    CEDServer.Disconnect(ns);
                    netState.Account = null;
                }
            }
            Config.Accounts.Remove(account);
            Config.Invalidate();
            CEDServer.SendPacket(ns, new DeleteUserResponsePacket(DeleteUserStatus.Deleted, username));
        }
        else {
            CEDServer.SendPacket(ns, new DeleteUserResponsePacket(DeleteUserStatus.NotFound, username));
        }
    }

    public static void OnListUsersPacket(BinaryReader reader, NetState ns) {
        CEDServer.SendPacket(ns, new CompressedPacket(new UserListPacket()));
    }

    public static void OnModifyRegionPacket(BinaryReader reader, NetState ns) {
        var regionName = reader.ReadStringNull();

        var region = Config.Regions.Find(x => x.Name == regionName);
        ModifyRegionStatus status;
        if (region == null) {
            region = new Region(Config.Regions, regionName);
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

        Config.Regions.Invalidate();
        AdminBroadcast(AccessLevel.Administrator, new ModifyRegionResponsePacket(status, region));

        if (status == ModifyRegionStatus.Modified) {
            foreach (var netState in CEDServer.Clients) {
                if (netState != null) {
                    var account = netState.Account;
                    
                    if (account.Regions.Contains(regionName)) {
                        CEDServer.SendPacket(netState, new AccessChangedPacket(account));
                    }
                }
            }
        }
    }

    public static void OnDeleteRegionPacket(BinaryReader reader, NetState ns) {
        var regionName = reader.ReadStringNull();
        var status = DeleteRegionStatus.NotFound;
        var region =  Config.Regions.Find(r => r.Name == regionName)
        if (region != null) {
            Config.Regions.Remove(region);
            Config.Regions.Invalidate();
            status = DeleteRegionStatus.Deleted;
        }
        
        AdminBroadcast(AccessLevel.Administrator, new DeleteRegionResponsePacket(status, regionName));
    }

    public static void OnListRegionsPacket(BinaryReader reader, NetState ns) {
        CEDServer.SendPacket(ns, new CompressedPacket(new RegionListPacket()));
    }

    //TODO: Maybe we need some extra logic around here
    public static void AdminBroadcast(AccessLevel accessLevel, Packet packet) {
        foreach (var ns in CEDServer.Clients) {
            if (ns != null && ns.Account.AccessLevel >= accessLevel) {
                CEDServer.SendPacket(ns, packet);
            }
        }
    }
}