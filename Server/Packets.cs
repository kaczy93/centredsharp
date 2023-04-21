using System.IO.Compression;
using Server;
using Shared;

namespace Cedserver;

public record BlockCoords(ushort X, ushort Y) {
    public BlockCoords(BinaryReader reader) : this(0, 0) {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(X);
        writer.Write(Y);
    }
};

class CompressedPacket : Packet {

    public CompressedPacket(Packet packet) : base(0x01, 0) {
        var compBuffer = new MemoryStream();
        var compStream = new ZLibStream(compBuffer, CompressionLevel.Optimal, true); //SmallestSize level seems to be slow
        compStream.Write(packet.Compile(out _));
        compStream.Close();
        Writer.Write((uint)packet.Stream.Length);
        compBuffer.Seek(0, SeekOrigin.Begin);
        compBuffer.CopyBytesTo(Stream, (int)compBuffer.Length);
    }
}

class BlockPacket : Packet {
    public BlockPacket(List<BlockCoords> coords, NetState? ns) : base(0x04, 0) {
        foreach (var coord in coords) {
            var mapBlock = CEDServer.Landscape.GetLandBlock(coord.X, coord.Y);
            var staticsBlock = CEDServer.Landscape.GetStaticBlock(coord.X, coord.Y);

            coord.Write(Writer);
            mapBlock.Write(Writer);
            Writer.Write((ushort)staticsBlock.Tiles.Count);
            staticsBlock.Write(Writer);
            if (ns == null) continue;
            var subscriptions = CEDServer.Landscape.GetBlockSubscriptions(coord.X, coord.Y);
            subscriptions.Add(ns);
        }
    }
}

class DrawMapPacket : Packet {
    public DrawMapPacket(LandTile landTile) : base(0x06, 8) {
        Writer.Write(landTile.X);
        Writer.Write(landTile.Y);
        Writer.Write(landTile.Z);
        Writer.Write(landTile.TileId);
    }
}

class InsertStaticPacket : Packet {
    public InsertStaticPacket(StaticTile staticTile) : base(0x07, 10) {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.TileId);
        Writer.Write(staticTile.Hue);
    }
}

class DeleteStaticPacket : Packet {
    public DeleteStaticPacket(StaticTile staticTile) : base(0x08, 10) {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.TileId);
        Writer.Write(staticTile.Hue);
    }
}

class ElevateStaticPacket : Packet {
    public ElevateStaticPacket(StaticTile staticTile, sbyte newZ) : base(0x09, 11) {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.TileId);
        Writer.Write(staticTile.Hue);
        Writer.Write(newZ);
    }
}

class MoveStaticPacket : Packet {
    public MoveStaticPacket(StaticTile staticTile, ushort newX, ushort newY) : base(0x0A, 14) {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.TileId);
        Writer.Write(staticTile.Hue);
        Writer.Write(newX);
        Writer.Write(newY);
    }
}

class HueStaticPacket : Packet {
    public HueStaticPacket(StaticTile staticTile, ushort newHue) : base(0x0B, 12) {
        Writer.Write(staticTile.X);
        Writer.Write(staticTile.Y);
        Writer.Write(staticTile.Z);
        Writer.Write(staticTile.TileId);
        Writer.Write(staticTile.Hue);
        Writer.Write(newHue);
    }
}

public class ProtocolVersionPacket : Packet {
    public ProtocolVersionPacket(uint version) : base(0x02, 0) {
        Writer.Write((byte)0x01);
        Writer.Write(version);    
    }
}

public class LoginResponsePacket : Packet {
    public LoginResponsePacket(LoginState state, Account? account = null) : base(0x02, 0) {
        Writer.Write((byte)0x03);
        Writer.Write((byte)state);
        if (state == LoginState.Ok && account != null) {
            account.LastLogon = DateTime.Now;
            Writer.Write((byte)account.AccessLevel);
            if(Config.CentrEdPlus)
                Writer.Write((uint)Math.Abs((DateTime.Now - CEDServer.StartTime).TotalSeconds));
            Writer.Write(Config.Map.Width);
            Writer.Write(Config.Map.Height);
            if (Config.CentrEdPlus) {
                uint flags = 0xF0000000;
                if (CEDServer.Landscape.TileDataProvider.Version == TileDataVersion.HighSeas)
                    flags |= 0x8;
                if (CEDServer.Landscape.IsUop)
                    flags |= 0x10;

                Writer.Write(flags);
            }

            ClientHandling.WriteAccountRestrictions(Writer, account);
        }
    }
}
    
public class ServerStatePacket : Packet {
    public ServerStatePacket(ServerState state, string message = "") : base(0x02, 0) {
        Writer.Write((byte)0x04);
        Writer.Write((byte)state);
        if(state == ServerState.Other)
            Writer.WriteStringNull(message); 
    }
}
public class ClientConnectedPacket : Packet {
    public ClientConnectedPacket(Account account) : base(0x0C, 0) {
        Writer.Write((byte)0x01);
        Writer.WriteStringNull(account.Name);
        if (Config.CentrEdPlus) {
            Writer.Write((byte)account.AccessLevel);
        }
    }
}

public class ClientDisconnectedPacket : Packet {
    public ClientDisconnectedPacket(string username) : base(0x0C, 0) {
        Writer.Write((byte)0x02);
        Writer.WriteStringNull(username);
    }
}

public class ClientListPacket : Packet {
    public ClientListPacket(NetState avoid) : base(0x0C, 0) {
        Writer.Write((byte)0x03);
        foreach (var ns in CEDServer.Clients) {
            if (ns != avoid) {
                Writer.WriteStringNull(ns.Account.Name);
                if (Config.CentrEdPlus) {
                    Writer.Write((byte)ns.Account.AccessLevel);
                    Writer.Write((uint)Math.Abs((ns.Account.LastLogon - CEDServer.StartTime).TotalSeconds));
                }
            }
        }
    }
}

public class SetClientPosPacket : Packet {
    public SetClientPosPacket(LastPos pos) : base(0x0C, 0) {
        Writer.Write((byte)0x04);
        Writer.Write((ushort)Math.Clamp(pos.X, 0, CEDServer.Landscape.CellWidth - 1));
        Writer.Write((ushort)Math.Clamp(pos.Y, 0, CEDServer.Landscape.CellHeight - 1));
    }
}

public class ChatMessagePacket : Packet {
    public ChatMessagePacket(string sender, string message) : base(0x0C, 0) {
        Writer.Write((byte)0x05);
        Writer.WriteStringNull(sender);
        Writer.WriteStringNull(message);
    }
}

public class AccessChangedPacket : Packet {
    public AccessChangedPacket(Account account) : base(0x0C, 0) {
        Writer.Write((byte)0x07);
        Writer.Write((byte)account.AccessLevel);
        ClientHandling.WriteAccountRestrictions(Writer, account);
    }
}

public class PasswordChangeStatusPacket : Packet {
    public PasswordChangeStatusPacket(PasswordChangeStatus status) : base(0x0C, 0) {
        Writer.Write((byte)0x08);
        Writer.Write((byte)status);
    }
}

public class ModifyUserResponsePacket : Packet {
    public ModifyUserResponsePacket(ModifyUserStatus status, Account? account) : base(0x03, 0) {
        Writer.Write((byte)0x05);
        Writer.Write((byte)status);
        
        if (account == null) return;
        
        Writer.WriteStringNull(account.Name);
        if (status == ModifyUserStatus.Added || status == ModifyUserStatus.Modified) {
            Writer.Write((byte)account.AccessLevel);
            Writer.Write(account.Regions.Count);
            foreach (var regionName in account.Regions) {
                Writer.WriteStringNull(regionName);
            }
        }
    }
}

public class DeleteUserResponsePacket : Packet {
    public DeleteUserResponsePacket(DeleteUserStatus status, string username) : base(0x03, 0) {
        Writer.Write((byte)0x06);
        Writer.Write((byte)status);
        Writer.WriteStringNull(username);
    }
}

public class UserListPacket : Packet {
    public UserListPacket() : base(0x03, 0) {
        Writer.Write((byte)0x07);
        Writer.Write((ushort)Config.Accounts.Count);
        foreach (var account in Config.Accounts) {
            Writer.WriteStringNull(account.Name);
            Writer.Write((byte)account.AccessLevel);
            Writer.Write((byte)account.Regions.Count);
            foreach (var region in account.Regions) {
                Writer.WriteStringNull(region);
            }
        }
    }
}

public class ModifyRegionResponsePacket : Packet {
    public ModifyRegionResponsePacket(ModifyRegionStatus status, Region region) : base(0x03, 0) {
        Writer.Write((byte)0x08);
        Writer.Write((byte)status);
        Writer.WriteStringNull(region.Name);
        if (status == ModifyRegionStatus.Added || status == ModifyRegionStatus.Modified) {
            Writer.Write(region.Area.Count);
            foreach (var rect in region.Area) {
                Writer.Write(rect.X1);
                Writer.Write(rect.Y1);
                Writer.Write(rect.X2);
                Writer.Write(rect.Y2);
            }
        }
    }
}

public class DeleteRegionResponsePacket : Packet {
    public DeleteRegionResponsePacket(DeleteRegionStatus status, string regionName) : base(0x03, 0) {
        Writer.Write((byte)0x09);
        Writer.Write((byte)status);
        Writer.WriteStringNull(regionName);
    }
}

public class RegionListPacket : Packet {
    public RegionListPacket() : base(0x03, 0)  {
        Writer.Write((byte)0x0A);
        Writer.Write((byte)Config.Regions.Count);
        foreach (var region in Config.Regions) {
            Writer.WriteStringNull(region.Name);
            Writer.Write((byte)region.Area.Count);
            foreach (var rect in region.Area) {
                Writer.Write(rect.X1);
                Writer.Write(rect.Y1);
                Writer.Write(rect.X2);
                Writer.Write(rect.Y2);
            }
        }
    }
}
