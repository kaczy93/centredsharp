using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public class ClientHandling
{
    private static PacketHandler<CentrEDClient>?[] Handlers { get; }

    static ClientHandling()
    {
        Handlers = new PacketHandler<CentrEDClient>?[0x100];

        Handlers[0x01] = new PacketHandler<CentrEDClient>(0, OnClientConnectedPacket);
        Handlers[0x02] = new PacketHandler<CentrEDClient>(0, OnClientDisconnectedPacket);
        Handlers[0x03] = new PacketHandler<CentrEDClient>(0, OnClientListPacket);
        Handlers[0x04] = new PacketHandler<CentrEDClient>(0, OnSetPosPacket);
        Handlers[0x05] = new PacketHandler<CentrEDClient>(0, OnChatMessagePacket);
        Handlers[0x07] = new PacketHandler<CentrEDClient>(0, OnAccessChangedPacket);
        Handlers[0x08] = new PacketHandler<CentrEDClient>(0, OnPasswordChangeStatusPacket);
    }

    public static void OnClientHandlerPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var packetHandler = Handlers[reader.ReadByte()];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnClientConnectedPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var username = reader.ReadStringNull();
        if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus)
        {
            reader.ReadByte(); //Access level
        }
        ns.Parent.Clients.Add(username);
        if (username != ns.Username)
            ns.Parent.OnClientConnected(username);
    }

    private static void OnClientDisconnectedPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var username = reader.ReadStringNull();
        ns.Parent.Clients.Remove(username);
        if (username != ns.Username)
            ns.Parent.OnClientDisconnected(username);
    }

    private static void OnClientListPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        ns.Parent.Clients.Clear();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            ns.Parent.Clients.Add(reader.ReadStringNull());
            if (ns.ProtocolVersion == ProtocolVersion.CentrEDPlus)
            {
                reader.ReadByte();   //Access level
                reader.ReadUInt32(); //login time since uptime
            }
        }
    }

    private static void OnSetPosPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        ns.Parent.SetPos(x, y);
    }

    private static void OnChatMessagePacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var sender = reader.ReadStringNull();
        var message = reader.ReadStringNull();
        ns.Parent.OnChatMessage(sender, message);
    }

    private static void OnAccessChangedPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var accessLevel = (AccessLevel)reader.ReadByte();
        ReadAccountRestrictions(reader);
        if (accessLevel != ns.Parent.AccessLevel)
        {
            ns.Parent.AccessLevel = accessLevel;
            if (accessLevel == AccessLevel.None)
            {
                //TODO: Maybe move this to client?
                ns.LogInfo("Your account has been locked");
                ns.Disconnect();
            }
        }
    }

    private static void OnPasswordChangeStatusPacket(BinaryReader reader, NetState<CentrEDClient> ns)
    {
        var status = (PasswordChangeStatus)reader.ReadByte();
        switch (status)
        {
            case PasswordChangeStatus.Success:
                ns.LogInfo("You password has been changed");
                break;
            case PasswordChangeStatus.OldPwInvalid:
                ns.LogInfo("Old password is wrong.");
                break;
            case PasswordChangeStatus.NewPwInvalid:
                ns.LogInfo("New password is not allowed.");
                break;
            case PasswordChangeStatus.Identical:
                ns.LogInfo("New password matched the old password.");
                break;
        }
        ;
    }

    public static List<Rect> ReadAccountRestrictions(BinaryReader reader)
    {
        var rectCount = reader.ReadUInt16();
        var result = new List<Rect>(rectCount);
        for (var i = 0; i < rectCount; i++)
        {
            result.Add(new Rect(reader));
        }
        return result;
    }
}