using Cedserver;
using Shared;

namespace Server; 

public class RadarMap {
    
    //TODO: Optimize radarmap initialization, 10s is way too long
    public RadarMap(Landscape landscape, BinaryReader mapReader, BinaryReader staidxReader, BinaryReader staticsReader, string radarcolPath) {
        using var radarcol = File.Open(radarcolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _radarColors = new ushort[radarcol.Length / sizeof(ushort)];
        var buffer = new byte[radarcol.Length];
        radarcol.Read(buffer, 0, (int)radarcol.Length);
        Buffer.BlockCopy(buffer, 0, _radarColors, 0, buffer.Length);
        
        _width = landscape.Width;
        _height = landscape.Height;
        _radarMap = new ushort[_width * _height];

        for (ushort x = 0; x < _width; x++) {
            for (ushort y = 0; y < _height; y++) {
                var block = landscape.GetBlockNumber(x, y);
                mapReader.BaseStream.Seek(landscape.GetMapOffset(x, y) + 4, SeekOrigin.Begin);
                var mapCell = new MapCell(null, mapReader);
                _radarMap[block] = _radarColors[mapCell.TileId];

                staidxReader.BaseStream.Seek(landscape.GetStaidxOffset(x, y), SeekOrigin.Begin);
                var index = new GenericIndex(staidxReader);
                var staticsBlock = new StaticBlock(staticsReader, index, x, y);
                
                var highestZ = mapCell.Altitude;
                foreach (var staticItem in staticsBlock.Items) {
                    if (staticItem.LocalX == 0 && staticItem.LocalY == 0 && staticItem.Z >= highestZ) {
                        highestZ = staticItem.Z;
                        _radarMap[block] = _radarColors[staticItem.TileId + 0x4000];
                    }
                }
            }
        }
        PacketHandlers.RegisterPacketHandler(0x0D, 2, OnRadarHandlingPacket);
    }

    private ushort _width;
    private ushort _height;
    private ushort[] _radarColors;
    private ushort[] _radarMap;
    private List<Packet>? _packets;

    private void OnRadarHandlingPacket(BinaryReader buffer, NetState ns) {
        ns.LogDebug("OnRadarHandlingPacket");
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.View)) return;
        switch(buffer.ReadByte()) {
            case 0x01: 
                CEDServer.SendPacket(ns, new RadarChecksumPacket(_radarMap));
                break;
            case 0x02: CEDServer.SendPacket(ns, new CompressedPacket(new RadarMapPacket(_radarMap)));
                break;
        }
    }

    public void Update(ushort x, ushort y, ushort tileId) {
        var block = x * _height + y;
        var color = _radarColors[tileId];
        if (_radarMap[block] != color) {
            _radarMap[block] = color;
            var packet = new UpdateRadarPacket(x, y, color);
            if (_packets != null) {
                _packets.Add(packet);
            }
            else {
                CEDServer.SendPacket(null, packet);
            }
        }
    }

    public void BeginUpdate() {
        if (_packets != null) throw new InvalidOperationException("RadarMap update is already in progress");
        
        _packets = new List<Packet>();
    }

    public void EndUpdate() {
        if (_packets == null) throw new InvalidOperationException("RadarMap update isn't in progress");
        
        var completePacket = new CompressedPacket(new RadarMapPacket(_radarMap));
        if(completePacket.Writer.BaseStream.Length <= _packets.Count / 4 * 5)
        {
            CEDServer.SendPacket(null, completePacket);
        }
        else {
            foreach (var packet in _packets) {
                CEDServer.SendPacket(null, packet);
            }
        }
    }
}

public class RadarChecksumPacket : Packet {
    public RadarChecksumPacket(ushort[] radarMap) : base(0x0D, 0) {
        Writer.Write((byte)0x01);
        Writer.Write(Crypto.Crc32Checksum(radarMap));
    }
}

public class RadarMapPacket : Packet {
    public RadarMapPacket(ushort[] radarMap) : base(0x0D, 0) {
        Writer.Write((byte)0x02);
        byte[] buffer = new byte[Buffer.ByteLength(radarMap)];
        Buffer.BlockCopy(radarMap, 0, buffer, 0, buffer.Length);
        Writer.Write(buffer);
    }
}

public class UpdateRadarPacket : Packet {
    public UpdateRadarPacket(ushort x, ushort y, ushort color) : base(0x0D, 0) {
        Writer.Write((byte)0x03);
        Writer.Write(x);
        Writer.Write(y);
        Writer.Write(color);
    }
}