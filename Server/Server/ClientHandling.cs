using CentrED.Network;
using CentrED.Utility;
using static CentrED.Server.PacketHandlers;

namespace CentrED.Server; 

public class ClientHandling {
    private static PacketHandler<CEDServer>?[] ClientHandlers { get; }

    static ClientHandling() {
        ClientHandlers = new PacketHandler<CEDServer>?[0x100];

        ClientHandlers[0x04] = new PacketHandler<CEDServer>(0, OnUpdateClientPosPacket);
        ClientHandlers[0x05] = new PacketHandler<CEDServer>(0, OnChatMessagePacket);
        ClientHandlers[0x06] = new PacketHandler<CEDServer>(0, OnGotoClientPosPacket);
        ClientHandlers[0x08] = new PacketHandler<CEDServer>(0, OnChangePasswordPacket);
    }
    
    public static void OnClientHandlerPacket(BinaryReader reader, NetState<CEDServer> ns) {
        if (!ValidateAccess(ns, AccessLevel.View)) return;
        var packetHandler = ClientHandlers[reader.ReadByte()];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnUpdateClientPosPacket(BinaryReader reader, NetState<CEDServer> ns) {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        ns.Parent.GetAccount(ns.Username)!.LastPos = new LastPos(x, y);
        ns.Parent.Config.Invalidate();
    }

    private static void OnChatMessagePacket(BinaryReader reader, NetState<CEDServer> ns) {
        ns.Parent.Send(new CompressedPacket(new ChatMessagePacket(ns.Username, reader.ReadStringNull())));
    }

    private static void OnGotoClientPosPacket(BinaryReader reader, NetState<CEDServer> ns) {
        var name = reader.ReadStringNull();
        var account = ns.Parent.GetAccount(name);
        if (account != null) {
            ns.Send(new SetClientPosPacket(ns));
        }
    }

    private static void OnChangePasswordPacket(BinaryReader reader, NetState<CEDServer> ns) {
        var oldPwd = reader.ReadStringNull();
        var newPwd = reader.ReadStringNull();
        var account = ns.Parent.GetAccount(ns.Username);
        if (account == null) return;
        
        PasswordChangeStatus status;
        if (!account.CheckPassword(oldPwd)) {
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
            account.UpdatePassword(newPwd);
        }
        ns.Parent.Config.Invalidate();
        ns.Send(new PasswordChangeStatusPacket(status));
    }

    public static void WriteAccountRestrictions(BinaryWriter writer, NetState<CEDServer> ns) {
        var account = ns.Parent.GetAccount(ns)!;
        if (account.AccessLevel >= AccessLevel.Administrator) {
            writer.Write((ushort)0); //Client expects areaCount all the time
            return;
        }

        var rects = new List<Rect>();
        foreach (var regionName in account.Regions) {
            var region = ns.Parent.GetRegion(regionName);
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