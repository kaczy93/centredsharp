﻿using CentrED.Network;
using CentrED.Utility;

namespace CentrED.Server; 

public class RadarMap {
    
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
                var landTile = new LandTile(mapReader);
                _radarMap[block] = _radarColors[landTile.Id];

                staidxReader.BaseStream.Seek(landscape.GetStaidxOffset(x, y), SeekOrigin.Begin);
                var index = new GenericIndex(staidxReader);
                var staticsBlock = new StaticBlock(staticsReader, index, x, y);
                
                var highestZ = landTile.Z;
                foreach (var staticTile in staticsBlock.Tiles) {
                    if (staticTile.LocalX == 0 && staticTile.LocalY == 0 && staticTile.Z >= highestZ) {
                        highestZ = staticTile.Z;
                        _radarMap[block] = _radarColors[staticTile.Id + 0x4000];
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

    private void OnRadarHandlingPacket(BinaryReader buffer, NetState<CEDServer> ns) {
        ns.LogDebug("OnRadarHandlingPacket");
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.View)) return;
        switch(buffer.ReadByte()) {
            case 0x01: 
                ns.Send(new RadarChecksumPacket(_radarMap));
                break;
            case 0x02: 
                ns.Send(new CompressedPacket(new RadarMapPacket(_radarMap)));
                break;
        }
    }

    public void Update(NetState<CEDServer> ns, ushort x, ushort y, ushort tileId) {
        var block = x * _height + y;
        var color = _radarColors[tileId];
        if (_radarMap[block] != color) {
            _radarMap[block] = color;
            var packet = new UpdateRadarPacket(x, y, color);
            if (_packets != null) {
                _packets.Add(packet);
            }
            else {
                ns.Parent.Send(packet);
            }
        }
    }

    public void BeginUpdate() {
        if (_packets != null) throw new InvalidOperationException("RadarMap update is already in progress");
        
        _packets = new List<Packet>();
    }

    public void EndUpdate(NetState<CEDServer> ns) {
        if (_packets == null) throw new InvalidOperationException("RadarMap update isn't in progress");
        
        var completePacket = new CompressedPacket(new RadarMapPacket(_radarMap));
        if(completePacket.Writer.BaseStream.Length <= _packets.Count / 4 * 5)
        {
            ns.Parent.Send(completePacket);
        }
        else {
            foreach (var packet in _packets) {
                ns.Parent.Send(packet);
            }
        }
        _packets = null;
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