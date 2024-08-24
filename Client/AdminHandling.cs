using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public static class AdminHandling
{
    public delegate void UserDeleted(string username);
    public delegate void UserModified(string username, ModifyUserStatus status);
    public delegate void RegionDeleted(string name);
    public delegate void RegionModified(string name, ModifyRegionStatus status);
    
    private static PacketHandler<CentrEDClient>?[] Handlers { get; }

    static AdminHandling()
    {
        Handlers = new PacketHandler<CentrEDClient>?[0x100];

        Handlers[0x05] = new PacketHandler<CentrEDClient>(0, OnModifyUserResponsePacket);
        Handlers[0x06] = new PacketHandler<CentrEDClient>(0, OnDeleteUserResponsePacket);
        Handlers[0x07] = new PacketHandler<CentrEDClient>(0, OnListUsersResponsePacket);
        Handlers[0x08] = new PacketHandler<CentrEDClient>(0, OnModifyRegionResponsePacket);
        Handlers[0x09] = new PacketHandler<CentrEDClient>(0, OnDeleteRegionResponsePacket);
        Handlers[0x0A] = new PacketHandler<CentrEDClient>(0, OnListRegionsResponsePacket);
    }

    public static void OnAdminHandlerPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnConnectionHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = Handlers[id];
        packetHandler?.OnReceive(reader, ns);
    }
    
    private static void OnModifyUserResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnModifyUserResponsePacket");
        var status = (ModifyUserStatus)reader.ReadByte();
        if (status == ModifyUserStatus.InvalidUsername)
            return;
        
        var username = reader.ReadStringNull();
        var accessLevel = (AccessLevel)reader.ReadByte();
        var regionCount = reader.ReadByte();
        var regions = new List<string>(regionCount);
        for (var i = 0; i < regionCount; i++)
        {
            var regionName = reader.ReadStringNull();
            regions.Add(regionName);
        }
        var user = new User(username, accessLevel, regions);
        if (status == ModifyUserStatus.Added)
        {
            ns.Parent.Admin.Users.Add(user);
        }
        if(status == ModifyUserStatus.Modified)
        {
            var index = ns.Parent.Admin.Users.FindIndex(u => u.Username == username);
            ns.Parent.Admin.Users[index] = user;
        }
        ns.Parent.OnUserModified(username, status);
    }
    
    private static void OnDeleteUserResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnDeleteUserResponsePacket");
        var status = (DeleteUserStatus)reader.ReadByte();
        var username = reader.ReadStringNull();
        if (status != DeleteUserStatus.Deleted)
            return;

        ns.Parent.Admin.Users.RemoveAll(u => u.Username == username);
        ns.Parent.OnUserDeleted(username);
    }
    
    private static void OnListUsersResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnListUsersResponsePacket");
        ns.Parent.Admin.Users.Clear();
        var userCount = reader.ReadUInt16();
        ns.Parent.Admin.Users.Capacity = userCount;
        for (var i = 0; i < userCount; i++)
        {
            var username = reader.ReadStringNull();
            var accessLevel = (AccessLevel)reader.ReadByte();
            var regionCount = reader.ReadByte();
            var regions = new List<string>(regionCount);
            for (var j = 0; j < regionCount; j++)
            {
                regions.Add(reader.ReadStringNull());
            }
            ns.Parent.Admin.Users.Add(new User(username, accessLevel, regions));
        }
    }
    
    private static void OnModifyRegionResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnModifyRegionResponsePacket");
        var status = (ModifyRegionStatus)reader.ReadByte();
        var regionName = reader.ReadStringNull();
        var areaCount = reader.ReadByte();
        var areas = new List<Rect>(areaCount);
        for (var i = 0; i < areaCount; i++)
        {
            var newArea = new Rect(reader);
            areas.Add(newArea);
        }
        var region = new Region(regionName, areas);
        if(status == ModifyRegionStatus.Added)
        {
            ns.Parent.Admin.Regions.Add(region);
        }
        if(status == ModifyRegionStatus.Modified)
        {
            var index = ns.Parent.Admin.Regions.FindIndex(r => r.Name == regionName);
            ns.Parent.Admin.Regions[index] = region;
        }
        ns.Parent.OnRegionModified(regionName, status);
    }
    
    private static void OnDeleteRegionResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnDeleteRegionResponsePacket");
        var status = (DeleteRegionStatus)reader.ReadByte();
        var regionName = reader.ReadStringNull();
        if (status == DeleteRegionStatus.NotFound)
            return;
        
        var index = ns.Parent.Admin.Regions.FindIndex(r => r.Name == regionName);
        ns.Parent.Admin.Regions.RemoveAt(index);
        ns.Parent.OnRegionDeleted(regionName);
    }
    
    private static void OnListRegionsResponsePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnListRegionsResponsePacket");
        var regionCount = reader.ReadByte();
        ns.Parent.Admin.Regions = new List<Region>(regionCount);
        for (var i = 0; i < regionCount; i++)
        {
            var regionName = reader.ReadStringNull();
            var areaCount = reader.ReadByte();
            var areas = new List<Rect>(areaCount);
            for (var j = 0; j < areaCount; j++)
            {
                areas.Add(new Rect(reader));
            }
            var region = new Region(regionName, areas);
            ns.Parent.Admin.Regions.Add(region);
        }
    }
}