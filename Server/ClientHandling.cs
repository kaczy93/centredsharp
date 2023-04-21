using CentrED.Utility;
using static CentrED.Server.PacketHandlers;

namespace CentrED.Server; 

public class ClientHandling {
    private static PacketHandler?[] ClientHandlers { get; }

    static ClientHandling() {
        ClientHandlers = new PacketHandler?[0x100];

        ClientHandlers[0x04] = new PacketHandler(0, OnUpdateClientPosPacket);
        ClientHandlers[0x05] = new PacketHandler(0, OnChatMessagePacket);
        ClientHandlers[0x06] = new PacketHandler(0, OnGotoClientPosPacket);
        ClientHandlers[0x08] = new PacketHandler(0, OnChangePasswordPacket);
    }
    
    public static void OnClientHandlerPacket(BinaryReader reader, NetState ns) {
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var packetHandler = ClientHandlers[reader.ReadByte()];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnUpdateClientPosPacket(BinaryReader reader, NetState ns) {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        ns.Account.LastPos = new LastPos(x, y);
        Config.Invalidate();
    }

    private static void OnChatMessagePacket(BinaryReader reader, NetState ns) {
        CEDServer.Send(new CompressedPacket(new ChatMessagePacket(ns.Account.Name, reader.ReadStringNull())));
    }

    private static void OnGotoClientPosPacket(BinaryReader reader, NetState ns) {
        var name = reader.ReadStringNull();
        var account = Config.Accounts.Find(a => a.Name == name);
        if (account != null) {
            ns.Send(new SetClientPosPacket(account.LastPos));
        }
    }

    private static void OnChangePasswordPacket(BinaryReader reader, NetState ns) {
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
        ns.Send(new PasswordChangeStatusPacket(status));
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