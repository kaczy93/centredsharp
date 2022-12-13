using Cedserver;
using Shared;

namespace Server; 

public class RadarMap {
    
    //TODO: Optimize radarmap initialization, 10s is way too long
    public RadarMap(Stream map, Stream statics, Stream staidx, ushort width, ushort height, string radarcolPath) {
        var radarcol = File.Open(radarcolPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _radarColors = new ushort[radarcol.Length / sizeof(ushort)];
        var buffer = new byte[radarcol.Length];
        radarcol.Read(buffer, 0, (int)radarcol.Length);
        Buffer.BlockCopy(buffer, 0, _radarColors, 0, buffer.Length);
        _width = width;
        _height = height;
        var count = width * height;
        _radarMap = new ushort[count];
        map.Position = 4;
        staidx.Position = 0;
        
        for (int i = 0; i < count; i++) {
            //Probably we can read whole records at once using pointers and Marshal
            //Original code uses separate data structure for radarcol initialization
            var mapCell = new MapCell(null, map);
            map.Seek(193, SeekOrigin.Current);
            _radarMap[i] = _radarColors[mapCell.TileId];
            var index = new GenericIndex(staidx);
            if (index.Lookup != -1 && index.Size > 0) {
                statics.Position = index.Lookup;
                StaticItem[] staticItems = new StaticItem[index.Size / 7];
                for (int j = 0; j < staticItems.Length; j++) {
                    staticItems[j] = new StaticItem(null, statics);
                }

                var highestZ = mapCell.Altitude;
                foreach (var staticItem in staticItems) {
                    if (staticItem.LocalX == 0 && staticItem.LocalY == 0 && staticItem.Z >= highestZ) {
                        highestZ = staticItem.Z;
                        _radarMap[i] = _radarColors[staticItem.TileId + 0x4000];
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
    private List<Packet>? _packets; //List of what?
    private uint _packetSize;

    //This packet handling is diffrent than others ¯\_(ツ)_/¯
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
        var block = x * _height * y;
        var color = _radarColors[tileId];
        if (_radarMap[block] != color) {
            _radarMap[block] = color;
            var packet = new UpdateRadarPacket(x, y, color);
            if (_packets != null) {
                _packets.Add(packet);
                _packetSize++;
            }
            else {
                CEDServer.SendPacket(null, packet);
            }
        }
    }

    public void BeginUpdate() {
        if (_packets != null) return;
        _packets = new List<Packet>();
        _packetSize = 0;
    }

    public void EndUpdate() {
        if (_packets != null) return;
        var completePacket = new CompressedPacket(new RadarMapPacket(_radarMap));
        if(completePacket.Writer.BaseStream.Length <= _packetSize / 4 * 5)
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