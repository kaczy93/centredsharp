using System.IO.Compression;
using Server;
using Shared;

namespace Cedserver;

record BlockCoords(ushort X, ushort Y) {
    public BlockCoords(BinaryReader reader) : this(0, 0) {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(X);
        writer.Write(Y);
    }
};

class CompressedPacket : Packet {
    public CompressedPacket(Packet packet) : base(0x01, 0) {
        var sourceStream = packet.Stream;
        var compBuffer = new MemoryStream();
        var compStream = new GZipStream(compBuffer, CompressionLevel.SmallestSize);
        sourceStream.BaseStream.CopyTo(compStream);
        Stream.Write((uint)sourceStream.BaseStream.Length);
        compBuffer.CopyTo(Stream.BaseStream);
    }
}

class BlockPacket : Packet {
    public BlockPacket(BlockCoords[] coords, NetState ns) : base(0x04, 0) {
        foreach (var coord in coords) {
            var mapBlock = CEDServer.Landscape.GetMapBlock(coord.X, coord.Y);
            if (mapBlock == null) continue;
            var staticsBlock = CEDServer.Landscape.GetStaticBlock(coord.X, coord.Y);
            if (staticsBlock == null) continue;

            coord.Write(Stream);
            mapBlock.Write(Stream);
            Stream.Write(staticsBlock.Items.Count);
            staticsBlock.Write(Stream);
            if (ns != null) { //TODO: Confirm if this subscription code is correct
                var subscriptions = CEDServer.Landscape.GetBlockSubscriptions(coord.X, coord.Y);
                subscriptions.Remove(ns);
                subscriptions.Add(ns);//Specifically AddLast
                if (ns.Subscriptions.IndexOf(subscriptions) == -1) {
                    ns.Subscriptions.Add(subscriptions);
                }
            }
        }
    }
}

class DrawMapPacket : Packet {
    public DrawMapPacket(MapCell mapCell) : base(0x06, 8) {
        Stream.Write(mapCell.X);
        Stream.Write(mapCell.Y);
        Stream.Write(mapCell.Altitude);
        Stream.Write(mapCell.TileId);
    }
}

class InsertStaticPacket : Packet {
    public InsertStaticPacket(StaticItem staticItem) : base(0x07, 10) {
        Stream.Write(staticItem.X);
        Stream.Write(staticItem.Y);
        Stream.Write(staticItem.Z);
        Stream.Write(staticItem.TileId);
        Stream.Write(staticItem.Hue);
    }
}

class DeleteStaticPacket : Packet {
    public DeleteStaticPacket(StaticItem staticItem) : base(0x08, 10) {
        Stream.Write(staticItem.X);
        Stream.Write(staticItem.Y);
        Stream.Write(staticItem.Z);
        Stream.Write(staticItem.TileId);
        Stream.Write(staticItem.Hue);
    }
}

class ElevateStaticPacket : Packet {
    public ElevateStaticPacket(StaticItem staticItem, sbyte newZ) : base(0x09, 11) {
        Stream.Write(staticItem.X);
        Stream.Write(staticItem.Y);
        Stream.Write(staticItem.Z);
        Stream.Write(staticItem.TileId);
        Stream.Write(staticItem.Hue);
        Stream.Write(newZ);
    }
}

class MoveStaticPacket : Packet {
    public MoveStaticPacket(StaticItem staticItem, ushort newX, ushort newY) : base(0x0A, 14) {
        Stream.Write(staticItem.X);
        Stream.Write(staticItem.Y);
        Stream.Write(staticItem.Z);
        Stream.Write(staticItem.TileId);
        Stream.Write(staticItem.Hue);
        Stream.Write(newX);
        Stream.Write(newY);
    }
}

class HueStaticPacket : Packet {
    public HueStaticPacket(StaticItem staticItem, ushort newHue) : base(0x0B, 12) {
        Stream.Write(staticItem.X);
        Stream.Write(staticItem.Y);
        Stream.Write(staticItem.Z);
        Stream.Write(staticItem.TileId);
        Stream.Write(staticItem.Hue);
        Stream.Write(newHue);
    }
}