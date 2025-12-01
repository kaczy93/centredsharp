using System.Buffers;
using CentrED.Network;

namespace CentrED.Client;

public delegate void RadarChecksum(uint checksum);
public delegate void RadarData(ReadOnlySpan<ushort> data);
public delegate void RadarUpdate(ushort x, ushort y, ushort color);

public class RadarMap
{
    private static PacketHandler<CentrEDClient>?[] Handlers { get; }

    static RadarMap()
    {
        Handlers = new PacketHandler<CentrEDClient>?[0x100];

        Handlers[0x01] = new PacketHandler<CentrEDClient>(0, OnRadarChecksumPacket);
        Handlers[0x02] = new PacketHandler<CentrEDClient>(0, OnRadarMapPacket);
        Handlers[0x03] = new PacketHandler<CentrEDClient>(0, OnUpdateRadarPacket);
    }

    public static void OnRadarHandlerPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        ns.LogDebug("Client OnRadarHandlerPacket");
        var id = reader.ReadByte();
        var packetHandler = Handlers[id];
        packetHandler?.OnReceive(reader, ns);
    }

    private static void OnRadarChecksumPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        var checksum = reader.ReadUInt32();
        ns.Parent.OnRadarChecksum(checksum);
    }

    private static unsafe void OnRadarMapPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        var length = ns.Parent.Width * ns.Parent.Height;
        var byteLength = length * 2;
        var data = new ushort[length];
        fixed (byte* bufferPtr = &reader.Buffer[reader.Position])
        fixed (ushort* dataPtr = &data[0])
        {
            Buffer.MemoryCopy(bufferPtr, dataPtr, byteLength, byteLength);
        }
        ns.Parent.OnRadarData(data);
    }

    private static void OnUpdateRadarPacket(SpanReader reader, NetState<CentrEDClient> ns)
    {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        var color = reader.ReadUInt16();
        ns.Parent.OnRadarUpdate(x, y, color);
    }
}