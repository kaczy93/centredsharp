using System.Buffers;
using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Server.Map;

public class RadarMap
{
    public RadarMap
    (
        ServerLandscape landscape,
        BinaryReader mapReader,
        BinaryReader staidxReader,
        BinaryReader staticsReader,
        string radarcolPath
    )
    {
        using var radarcol = File.Open(radarcolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _radarColors = new ushort[radarcol.Length / sizeof(ushort)];
        var buffer = new byte[radarcol.Length];
        radarcol.Read(buffer, 0, (int)radarcol.Length);
        Buffer.BlockCopy(buffer, 0, _radarColors, 0, buffer.Length);

        _width = landscape.Width;
        _height = landscape.Height;
        _radarMap = new ushort[_width * _height];
        for (ushort x = 0; x < _width; x++)
        {
            for (ushort y = 0; y < _height; y++)
            {
                var block = landscape.GetBlockNumber(x, y);
                mapReader.BaseStream.Seek(landscape.GetMapOffset(x, y) + 4, SeekOrigin.Begin);
                var landTile = new LandTile(mapReader, 0, 0);
                _radarMap[block] = _radarColors[landTile.Id];

                staidxReader.BaseStream.Seek(landscape.GetStaidxOffset(x, y), SeekOrigin.Begin);
                var index = new GenericIndex(staidxReader);
                var staticsBlock = new StaticBlock(landscape, x, y, staticsReader, index);

                var highestZ = landTile.Z;
                foreach (var staticTile in staticsBlock.GetTiles(0, 0))
                {
                    if (staticTile.Z >= highestZ)
                    {
                        highestZ = staticTile.Z;
                        var id = staticTile.Id + 0x4000;
                        if (id > _radarColors.Length)
                        {
                            Console.WriteLine($"Invalid static tile {staticTile.Id} at block {x},{y}");
                            id = 0x4000;
                        }
                        _radarMap[block] = _radarColors[id];
                    }
                }
            }
        }
    }
    
    private ushort _width;
    private ushort _height;
    private ushort[] _radarColors;
    private ushort[] _radarMap;
    private List<Packet>? _packets;

    internal void OnRadarHandlingPacket(SpanReader reader, NetState<CEDServer> ns)
    {
        ns.LogDebug("OnRadarHandlingPacket");
        if (!ns.ValidateAccess(AccessLevel.View))
            return;
        var subpacket = reader.ReadByte();
        switch (subpacket)
        {
            case 0x01: ns.Send(new RadarChecksumPacket(_radarMap)); break;
            case 0x02: ns.SendCompressed(new RadarMapPacket(_radarMap)); break;
            default: throw new ArgumentException($"Invalid RadarMap SubPacket {subpacket}");
        }
    }

    public void Update(NetState<CEDServer> ns, ushort x, ushort y, ushort tileId)
    {
        var block = x * _height + y;
        var color = _radarColors[tileId];
        if (_radarMap[block] != color)
        {
            _radarMap[block] = color;
            var packet = new UpdateRadarPacket(x, y, color);
            if (_packets != null)
            {
                _packets.Add(packet);
            }
            else
            {
                ns.Parent.Send(packet);
            }
        }
    }

    public void BeginUpdate()
    {
        if (_packets != null)
            throw new InvalidOperationException("RadarMap update is already in progress");

        _packets = new List<Packet>();
    }

    public void EndUpdate(NetState<CEDServer> ns)
    {
        if (_packets == null)
            throw new InvalidOperationException("RadarMap update isn't in progress");

        if (_packets.Count > 1024)
        {
            ns.SendCompressed(new RadarMapPacket(_radarMap));
        }
        else
        {
            foreach (var packet in _packets)
            {
                ns.Send(packet);
            }
        }
        _packets = null;
    }
}

public class RadarChecksumPacket : Packet
{
    public RadarChecksumPacket(ushort[] radarMap) : base(0x0D, 0)
    {
        Writer.Write((byte)0x01);
        Writer.Write(Crypto.Crc32Checksum(radarMap));
    }
}

public class RadarMapPacket : Packet
{
    public RadarMapPacket(ushort[] radarMap) : base(0x0D, 0)
    {
        Writer.Write((byte)0x02);
        byte[] buffer = new byte[Buffer.ByteLength(radarMap)];
        Buffer.BlockCopy(radarMap, 0, buffer, 0, buffer.Length);
        Writer.Write(buffer);
    }
}

public class UpdateRadarPacket : Packet
{
    public UpdateRadarPacket(ushort x, ushort y, ushort color) : base(0x0D, 0)
    {
        Writer.Write((byte)0x03);
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(color);
    }
}