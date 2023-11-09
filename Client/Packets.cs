using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Client;

public class LoginRequestPacket : Packet
{
    public LoginRequestPacket(string username, string password) : base(0x02, 0)
    {
        Writer.Write((byte)0x03);
        Writer.WriteStringNull(username);
        Writer.WriteStringNull(password);
    }
}

public class QuitPacket : Packet
{
    public QuitPacket() : base(0x02, 0)
    {
        Writer.Write((byte)0x05);
    }
}

public class ServerFlushPacket : Packet
{
    public ServerFlushPacket() : base(0x03, 0)
    {
        Writer.Write((byte)0x01);
    }
}

public class ServerStopPacket : Packet
{
    public ServerStopPacket(string reason) : base(0x03, 0)
    {
        Writer.Write((byte)0x02);
        Writer.WriteStringNull(reason);
    }
}

public class RequestBlocksPacket : Packet
{
    public RequestBlocksPacket(BlockCoords blockCoord) : base(0x04, 0)
    {
        blockCoord.Write(Writer);
    }

    public RequestBlocksPacket(List<BlockCoords> blockCoords) : base(0x04, 0)
    {
        foreach (var blockCoord in blockCoords)
        {
            blockCoord.Write(Writer);
        }
    }
}

public class FreeBlockPacket : Packet
{
    public FreeBlockPacket(ushort x, ushort y) : base(0x05, 5)
    {
        Writer.Write(x);
        Writer.Write(y);
    }
}

public class DrawMapPacket : Packet
{
    public DrawMapPacket(LandTile tile) : this(tile.X, tile.Y, tile.Z, tile.Id)
    {
    }

    public DrawMapPacket(LandTile tile, ushort newId) : this(tile.X, tile.Y, tile.Z, newId)
    {
    }

    public DrawMapPacket(LandTile tile, sbyte newZ) : this(tile.X, tile.Y, newZ, tile.Id)
    {
    }

    public DrawMapPacket(ushort x, ushort y, sbyte z, ushort tileId) : base(0x06, 8)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
    }
}

public class InsertStaticPacket : Packet
{
    public InsertStaticPacket(StaticTile tile) : this(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue)
    {
    }

    public InsertStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue) : base(0x07, 10)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
    }
}

public class DeleteStaticPacket : Packet
{
    public DeleteStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue) : base(0x08, 10)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
    }

    public DeleteStaticPacket(StaticTile tile) : this(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue)
    {
    }
}

public class ElevateStaticPacket : Packet
{
    public ElevateStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, sbyte newZ) : base(0x09, 11)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
        Writer.Write(newZ);
    }

    public ElevateStaticPacket(StaticTile tile, sbyte newZ) : this(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue, newZ)
    {
    }
}

public class MoveStaticPacket : Packet
{
    public MoveStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, ushort newX, ushort newY) : base
        (0x0A, 14)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
        Writer.Write(newX);
        Writer.Write(newY);
    }

    public MoveStaticPacket(StaticTile tile, ushort newX, ushort newY) : this
        (tile.X, tile.Y, tile.Z, tile.Id, tile.Hue, newX, newY)
    {
    }
}

public class HueStaticPacket : Packet
{
    public HueStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, ushort newHue) : base(0x0B, 12)
    {
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
        Writer.Write(newHue);
    }

    public HueStaticPacket(StaticTile tile, ushort newHue) : this(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue, newHue)
    {
    }
}

public class UpdateClientPosPacket : Packet
{
    public UpdateClientPosPacket(ushort x, ushort y) : base(0x0C, 0)
    {
        Writer.Write((byte)0x04);
        Writer.Write(x);
        Writer.Write(y);
    }
}

public class ChatMessagePacket : Packet
{
    public ChatMessagePacket(string message) : base(0x0C, 0)
    {
        Writer.Write((byte)0x05);
        Writer.WriteStringNull(message);
    }
}

public class GotoClientPosPacket : Packet
{
    public GotoClientPosPacket(string username) : base(0x0C, 0)
    {
        Writer.Write((byte)0x06);
        Writer.WriteStringNull(username);
    }
}

public class ChangePasswordPacket : Packet
{
    public ChangePasswordPacket(string oldPassword, string newPassword) : base(0x0C, 0)
    {
        Writer.Write((byte)0x08);
        Writer.WriteStringNull(oldPassword);
        Writer.WriteStringNull(newPassword);
    }
}

public class RequestRadarChecksumPacket : Packet
{
    public RequestRadarChecksumPacket() : base(0x0D, 2)
    {
        Writer.Write((byte)0x01);
    }
}

public class RequestRadarMapPacket : Packet
{
    public RequestRadarMapPacket() : base(0x0D, 2)
    {
        Writer.Write((byte)0x02);
    }
}

public class NoOpPacket : Packet
{
    public NoOpPacket() : base(0xFF, 1)
    {
    }
}