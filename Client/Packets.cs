using CentrED.Client.Map;
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
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    
    public DrawMapPacket(LandTile tile) : this(tile.X, tile.Y, tile.Z, tile.RealId)
    {
    }

    public DrawMapPacket(LandTile tile, ushort newId, sbyte newZ) : this(tile.X, tile.Y, newZ, newId)
    {
    }

    public DrawMapPacket(LandTile tile, sbyte newZ) : this(tile.X, tile.Y, newZ, tile.RealId)
    {
    }

    public DrawMapPacket(ushort x, ushort y, sbyte z, ushort tileId) : base(0x06, 8)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
    }
}

public class InsertStaticPacket : Packet
{
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    
    public InsertStaticPacket(StaticTile tile) : this(tile.X, tile.Y, tile.Z, tile.Id, tile.Hue)
    {
    }

    public InsertStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue) : base(0x07, 10)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(z);
        Writer.Write(tileId);
        Writer.Write(hue);
    }
}

public class DeleteStaticPacket : Packet
{
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    
    public DeleteStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue) : base(0x08, 10)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
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
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    public sbyte NewZ { get; }
    
    public ElevateStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, sbyte newZ) : base(0x09, 11)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
        NewZ = newZ;
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
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    public ushort NewX { get; }
    public ushort NewY { get; }
    
    public MoveStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, ushort newX, ushort newY) : base
        (0x0A, 14)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
        NewX = newX;
        NewY = newY;
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
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    public ushort Hue { get; }
    public ushort NewHue { get; }
    
    public HueStaticPacket(ushort x, ushort y, sbyte z, ushort tileId, ushort hue, ushort newHue) : base(0x0B, 12)
    {
        X = x;
        Y = y;
        Z = z;
        TileId = tileId;
        Hue = hue;
        NewHue = newHue;
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

public class LargeScaleOperationPacket : Packet
{
    public LargeScaleOperationPacket(AreaInfo[] areas, ILargeScaleOperation lso) : base(0x0E, 0)
    {
        Writer.Write((byte)Math.Min(255, areas.Length));
        foreach (var areaInfo in areas)
        {
            areaInfo.Write(Writer);
        }

        WriteLso(lso, typeof(LSOCopyMove));
        WriteLso(lso, typeof(LSOSetAltitude));
        WriteLso(lso, typeof(LSODrawLand));
        WriteLso(lso, typeof(LSODeleteStatics));
        WriteLso(lso, typeof(LSOAddStatics));
        WriteLso(lso, typeof(LSODrawCoastLine));
    }

    private void WriteLso(ILargeScaleOperation lso, Type type)
    {
        if (lso.GetType() == type)
        {
            Writer.Write(true);
            lso.Write(Writer);
        }
        else
        {
            Writer.Write(false);
        }
    }
}

public class NoOpPacket : Packet
{
    public NoOpPacket() : base(0xFF, 1)
    {
    }
}

public abstract class AdminPacket : Packet
{
    public AdminPacket(byte packetId) : base(0x03, 0)
    {
        Writer.Write(packetId);
    }
}

public class ServerFlushPacket : AdminPacket
{
    public ServerFlushPacket() : base(0x01)
    {
    }
}

public class ServerStopPacket : AdminPacket
{
    public ServerStopPacket(string reason) : base(0x02)
    {
        Writer.WriteStringNull(reason);
    }
}

public class ModifyUserPacket : AdminPacket
{
    public ModifyUserPacket(string username, string password, AccessLevel accessLevel, List<string> regions) : base(0x05)
    {
        Writer.WriteStringNull(username);
        Writer.WriteStringNull(password);
        Writer.Write((byte)accessLevel);
        Writer.Write((byte)regions.Count);
        foreach (var region in regions)
        {
            Writer.WriteStringNull(region);
        }
    }
}

public class DeleteUserPacket : AdminPacket
{
    public DeleteUserPacket(string username) : base(0x06)
    {
        Writer.WriteStringNull(username);
    }
}

public class ListUsersPacket : AdminPacket
{
    public ListUsersPacket() : base(0x07)
    {
    }
}

public class ModifyRegionPacket : AdminPacket
{
    public ModifyRegionPacket(string regionName, List<Rect> areas) : base(0x08)
    {
        Writer.WriteStringNull(regionName);
        Writer.Write((byte)areas.Count);
        foreach (var area in areas)
        {
            area.Write(Writer);
        }
    }
}

public class DeleteRegionPacket : AdminPacket
{
    public DeleteRegionPacket(string regionName) : base(0x09)
    {
        Writer.WriteStringNull(regionName);
    }
}

public class ListRegionsPacket : AdminPacket
{
    public ListRegionsPacket() : base(0x0A)
    {
    }
}