using CentrED.Network;
using CentrED.Server.Config;
using CentrED.Utility;

namespace CentrED.Server;

class BlockPacket : Packet
{
    public BlockPacket(IEnumerable<BlockCoords> coords, NetState<CEDServer> ns, bool subscribe) : base(0x04, 0)
    {
        foreach (var coord in coords)
        {
            var mapBlock = ns.Parent.Landscape.GetLandBlock(coord.X, coord.Y);
            var staticsBlock = ns.Parent.Landscape.GetStaticBlock(coord.X, coord.Y);

            coord.Write(Writer);
            mapBlock.Write(Writer);
            Writer.Write((ushort)staticsBlock.TotalTilesCount);
            staticsBlock.SortTiles(ref ns.Parent.Landscape.TileDataProvider.StaticTiles);
            staticsBlock.Write(Writer);
            if (!subscribe)
                continue;
            var subscriptions = ns.Parent.Landscape.GetBlockSubscriptions(coord.X, coord.Y);
            subscriptions.Add(ns);
        }
    }
}

class DrawMapPacket : Packet
{
    public DrawMapPacket(LandTile landTile) : base(0x06, 8)
    {
        Writer.Write(landTile.X);
        Writer.Write(landTile.Y);
        Writer.Write(landTile.Z);
        Writer.Write(landTile.Id);
    }
}

class InsertStaticPacket : Packet
{
    public InsertStaticPacket(StaticTile staticTile) : base(0x07, 10)
    {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.Id);
        Writer.Write(staticTile.Hue);
    }
}

class DeleteStaticPacket : Packet
{
    public DeleteStaticPacket(StaticTile staticTile) : base(0x08, 10)
    {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.Id);
        Writer.Write(staticTile.Hue);
    }
}

class ElevateStaticPacket : Packet
{
    public ElevateStaticPacket(StaticTile staticTile, sbyte newZ) : base(0x09, 11)
    {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.Id);
        Writer.Write(staticTile.Hue);
        Writer.Write(newZ);
    }
}

class MoveStaticPacket : Packet
{
    public MoveStaticPacket(StaticTile staticTile, ushort newX, ushort newY) : base(0x0A, 14)
    {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.Id);
        Writer.Write(staticTile.Hue);
        Writer.Write(newX);
        Writer.Write(newY);
    }
}

class HueStaticPacket : Packet
{
    public HueStaticPacket(StaticTile staticTile, ushort newHue) : base(0x0B, 12)
    {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.Id);
        Writer.Write(staticTile.Hue);
        Writer.Write(newHue);
    }
}

public class ProtocolVersionPacket : Packet
{
    public ProtocolVersionPacket(uint version) : base(0x02, 0)
    {
        Writer.Write((byte)0x01);
        Writer.Write(version);
    }
}

public class LoginResponsePacket : Packet
{
    public LoginResponsePacket(LoginState state, NetState<CEDServer>? ns = null) : base(0x02, 0)
    {
        Writer.Write((byte)0x03);
        Writer.Write((byte)state);
        if (state == LoginState.Ok && ns != null)
        {
            ns.Account().LastLogon = DateTime.Now;
            Writer.Write((byte)ns.AccessLevel());
            if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus)
                Writer.Write((uint)Math.Abs((DateTime.Now - ns.Parent.StartTime).TotalSeconds));
            Writer.Write(ns.Parent.Landscape.Width);
            Writer.Write(ns.Parent.Landscape.Height);
            if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus)
            {
                uint flags = 0xF0000000;
                if (ns.Parent.Landscape.TileDataProvider.Version == TileDataVersion.HighSeas)
                    flags |= 0x8;
                if (ns.Parent.Landscape.IsUop)
                    flags |= 0x10;

                Writer.Write(flags);
            }

            ClientHandling.WriteAccountRestrictions(Writer, ns);
        }
    }
}

public class ServerStatePacket : Packet
{
    public ServerStatePacket(ServerState state, string message = "") : base(0x02, 0)
    {
        Writer.Write((byte)0x04);
        Writer.Write((byte)state);
        if (state == ServerState.Other)
            Writer.WriteStringNull(message);
    }
}

public class ClientConnectedPacket : Packet
{
    public ClientConnectedPacket(NetState<CEDServer> ns) : base(0x0C, 0)
    {
        Writer.Write((byte)0x01);
        Writer.WriteStringNull(ns.Username);
        if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus)
        {
            Writer.Write((byte)ns.AccessLevel());
        }
    }
}

public class ClientDisconnectedPacket : Packet
{
    public ClientDisconnectedPacket(NetState<CEDServer> ns) : base(0x0C, 0)
    {
        Writer.Write((byte)0x02);
        Writer.WriteStringNull(ns.Username);
    }
}

public class ClientListPacket : Packet
{
    public ClientListPacket(NetState<CEDServer> avoid) : base(0x0C, 0)
    {
        Writer.Write((byte)0x03);
        foreach (var ns in avoid.Parent.Clients)
        {
            if (ns.Username != "" && ns != avoid)
            {
                Writer.WriteStringNull(ns.Username);
                if (avoid.Parent.Config.CentrEdPlus)
                {
                    Writer.Write((byte)ns.AccessLevel());
                    Writer.Write((uint)Math.Abs((ns.LastLogon() - avoid.Parent.StartTime).TotalSeconds));
                }
            }
        }
    }
}

public class SetClientPosPacket : Packet
{
    public SetClientPosPacket(NetState<CEDServer> ns) : base(0x0C, 0)
    {
        Writer.Write((byte)0x04);
        Writer.Write((ushort)Math.Clamp(ns.Account().LastPos.X, 0, ns.Parent.Landscape.CellWidth - 1));
        Writer.Write((ushort)Math.Clamp(ns.Account().LastPos.Y, 0, ns.Parent.Landscape.CellHeight - 1));
    }
}

public class ChatMessagePacket : Packet
{
    public ChatMessagePacket(string sender, string message) : base(0x0C, 0)
    {
        Writer.Write((byte)0x05);
        Writer.WriteStringNull(sender);
        Writer.WriteStringNull(message);
    }
}

public class AccessChangedPacket : Packet
{
    public AccessChangedPacket(NetState<CEDServer> ns) : base(0x0C, 0)
    {
        Writer.Write((byte)0x07);
        Writer.Write((byte)ns.AccessLevel());
        ClientHandling.WriteAccountRestrictions(Writer, ns);
    }
}

public class PasswordChangeStatusPacket : Packet
{
    public PasswordChangeStatusPacket(PasswordChangeStatus status) : base(0x0C, 0)
    {
        Writer.Write((byte)0x08);
        Writer.Write((byte)status);
    }
}

public class ModifyUserResponsePacket : Packet
{
    public ModifyUserResponsePacket(ModifyUserStatus status, Account? account) : base(0x03, 0)
    {
        Writer.Write((byte)0x05);
        Writer.Write((byte)status);

        if (account == null)
            return;

        Writer.WriteStringNull(account.Name);
        if (status == ModifyUserStatus.Added || status == ModifyUserStatus.Modified)
        {
            Writer.Write((byte)account.AccessLevel);
            Writer.Write(account.Regions.Count);
            foreach (var regionName in account.Regions)
            {
                Writer.WriteStringNull(regionName);
            }
        }
    }
}

public class DeleteUserResponsePacket : Packet
{
    public DeleteUserResponsePacket(DeleteUserStatus status, string username) : base(0x03, 0)
    {
        Writer.Write((byte)0x06);
        Writer.Write((byte)status);
        Writer.WriteStringNull(username);
    }
}

public class UserListPacket : Packet
{
    public UserListPacket(NetState<CEDServer> ns) : base(0x03, 0)
    {
        var accounts = ns.Parent.Config.Accounts;
        Writer.Write((byte)0x07);
        Writer.Write((ushort)accounts.Count);
        foreach (var account in accounts)
        {
            Writer.WriteStringNull(account.Name);
            Writer.Write((byte)account.AccessLevel);
            Writer.Write((byte)account.Regions.Count);
            foreach (var region in account.Regions)
            {
                Writer.WriteStringNull(region);
            }
        }
    }
}

public class ModifyRegionResponsePacket : Packet
{
    public ModifyRegionResponsePacket(ModifyRegionStatus status, Region region) : base(0x03, 0)
    {
        Writer.Write((byte)0x08);
        Writer.Write((byte)status);
        Writer.WriteStringNull(region.Name);
        if (status is ModifyRegionStatus.Added or ModifyRegionStatus.Modified)
        {
            Writer.Write(region.Area.Count);
            foreach (var rect in region.Area)
            {
                rect.Write(Writer);
            }
        }
    }
}

public class DeleteRegionResponsePacket : Packet
{
    public DeleteRegionResponsePacket(DeleteRegionStatus status, string regionName) : base(0x03, 0)
    {
        Writer.Write((byte)0x09);
        Writer.Write((byte)status);
        Writer.WriteStringNull(regionName);
    }
}

public class RegionListPacket : Packet
{
    public RegionListPacket(NetState<CEDServer> ns) : base(0x03, 0)
    {
        var regions = ns.Parent.Config.Regions;
        Writer.Write((byte)0x0A);
        Writer.Write((byte)regions.Count);
        foreach (var region in regions)
        {
            Writer.WriteStringNull(region.Name);
            Writer.Write((byte)region.Area.Count);
            foreach (var rect in region.Area)
            {
                Writer.Write(rect.X1);
                Writer.Write(rect.Y1);
                Writer.Write(rect.X2);
                Writer.Write(rect.Y2);
            }
        }
    }
}