using System.Drawing;
using Server;
using Shared;
using static Server.PacketHandlers;

namespace Cedserver; 

public class ClientHandling {

    public static PacketHandler[] ClientHandlers { get; }

    static ClientHandling() {
        ClientHandlers = new PacketHandler[0x100];

        ClientHandlers[0x04] = new PacketHandler(0, OnUpdateClientPosPacket);
        ClientHandlers[0x05] = new PacketHandler(0, OnChatMessagePacket);
        ClientHandlers[0x06] = new PacketHandler(0, OnGotoClientPosPacket);
        ClientHandlers[0x08] = new PacketHandler(0, OnChangePasswordPacket);
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
            WriteAccountRestrictions(Writer, account);
        }
    }

    public class PasswordChangeStatusPacket : Packet {
        public PasswordChangeStatusPacket(PasswordChangeStatus status) : base(0x0C, 0) {
            Writer.Write((byte)0x08);
            Writer.Write((byte)status);
        }
    }

    public static void OnClientHandlerPacket(BinaryReader reader, NetState ns) {
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var packetHandler = ClientHandlers[reader.ReadByte()];
        if (packetHandler != null) {
            packetHandler.OnReceive(reader, ns);
        }
    }

    public static void OnUpdateClientPosPacket(BinaryReader reader, NetState ns) {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        ns.Account.LastPos = new LastPos(x, y);
        Config.Invalidate();
    }

    public static void OnChatMessagePacket(BinaryReader reader, NetState ns) {
        CEDServer.SendPacket(null, new CompressedPacket(new ChatMessagePacket(ns.Account.Name, reader.ReadStringNull())));
    }

    public static void OnGotoClientPosPacket(BinaryReader reader, NetState ns) {
        var account = Config.Accounts.Find(a => a.Name == reader.ReadStringNull());
        if (account != null) {
            CEDServer.SendPacket(ns, new SetClientPosPacket(account.LastPos));
        }
    }

    public static void OnChangePasswordPacket(BinaryReader reader, NetState ns) {
        var oldPwd = reader.ReadStringNull();
        var newPwd = reader.ReadStringNull();
        PasswordChangeStatus status;
        if (!ns.Account.CheckPassword(oldPwd)) {
            status = PasswordChangeStatus.OldPwInvalid;
        }
        else if (oldPwd == newPwd) {
            status = PasswordChangeStatus.Identical;
        }
        else if (newPwd.Length < 4) {
            status = PasswordChangeStatus.NewPwInvalid;
        }
        else {
            status = PasswordChangeStatus.Success;
            ns.Account.UpdatePassword(newPwd);
        }
        CEDServer.SendPacket(ns, new PasswordChangeStatusPacket(status));
    }

    public static void WriteAccountRestrictions(BinaryWriter writer, Account account) {
        if (account.AccessLevel >= AccessLevel.Administrator) {
            writer.Write((ushort)0); //Client expects areaCount all the time
            return;
        }

        var rects = new List<Rect>();
        foreach (var regionName in account.Regions) {
            var region = Config.Regions.Find(r => r.Name == regionName);
            if (region != null) {
                rects.AddRange(region.Area);
            }
        }
        
        writer.Write((ushort)rects.Count);
        foreach (var rect in rects) {
            writer.Write(rect.X1);
            writer.Write(rect.Y1);
            writer.Write(rect.X2);
            writer.Write(rect.Y2);

        }
    }
}