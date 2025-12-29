using System.Buffers;
using CentrED.Network;
using CentrED.Server.Config;

namespace CentrED.Server;

public class ClientHandling
{
    private static PacketHandler<CEDServer>?[] Handlers { get; }

    static ClientHandling()
    {
        Handlers = new PacketHandler<CEDServer>?[0x100];

        Handlers[0x04] = new PacketHandler<CEDServer>(0, OnUpdateClientPosPacket);
        Handlers[0x05] = new PacketHandler<CEDServer>(0, OnChatMessagePacket);
        Handlers[0x06] = new PacketHandler<CEDServer>(0, OnGotoClientPosPacket);
        Handlers[0x08] = new PacketHandler<CEDServer>(0, OnChangePasswordPacket);
    }

    public static void OnClientHandlerPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnClientHandlerPacket");
        if (!ns.ValidateAccess(AccessLevel.View))
            return;
        var packetHandler = Handlers[reader.ReadByte()];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnUpdateClientPosPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnUpdateClientPosPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        ns.Parent.GetAccount(ns.Username)!.LastPos = new LastPos(x, y);
        ns.Parent.Config.Invalidate();
    }

    private static void OnChatMessagePacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnChatMessagePacket");
        ns.Parent.Broadcast(new ChatMessagePacket(ns.Username, reader.ReadString()));
    }

    private static void OnGotoClientPosPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnGotoClientPosPacket");
        var name = reader.ReadString();
        var client = ns.Parent.GetClient(name);
        if (client != null)
        {
            ns.Send(new SetClientPosPacket(client));
        }
    }

    private static void OnChangePasswordPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("Server OnChangePasswordPacket");
        var oldPwd = reader.ReadString();
        var newPwd = reader.ReadString();
        var account = ns.Parent.GetAccount(ns.Username);
        if (account == null)
            return;

        PasswordChangeStatus status;
        if (!account.CheckPassword(oldPwd))
        {
            status = PasswordChangeStatus.OldPwInvalid;
        }
        else if (oldPwd == newPwd)
        {
            status = PasswordChangeStatus.Identical;
        }
        else if (newPwd.Length < 4)
        {
            status = PasswordChangeStatus.NewPwInvalid;
        }
        else
        {
            status = PasswordChangeStatus.Success;
            account.UpdatePassword(newPwd);
        }
        ns.Parent.Config.Invalidate();
        ns.Send(new PasswordChangeStatusPacket(status));
    }

    public static void WriteAccountRestrictions(BinaryWriter writer, NetState<CEDServer> ns)
    {
        var account = ns.Parent.GetAccount(ns)!;
        if (account.AccessLevel >= AccessLevel.Administrator)
        {
            writer.Write((ushort)0); //Client expects areaCount all the time
            return;
        }

        var rects = new List<RectU16>();
        foreach (var regionName in account.Regions)
        {
            var region = ns.Parent.GetRegion(regionName);
            if (region != null)
            {
                rects.AddRange(region.Area);
            }
        }

        writer.Write((ushort)rects.Count);
        foreach (var rect in rects)
        {
            rect.Write(writer);
        }
    }
}