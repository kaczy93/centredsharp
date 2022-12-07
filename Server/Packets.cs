using System.IO.Compression;
using Server;
using Shared;

namespace Cedserver;

public record BlockCoords(ushort X, ushort Y) {
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

    private Packet basePacket;
    public CompressedPacket(Packet packet) : base(0x01, 0) {//Need to fix compression
        basePacket = packet;
        // var compBuffer = new MemoryStream();
        // var compStream = new ZLibStream(compBuffer, CompressionMode.Compress);
        // packet.Write(compStream);
        // Writer.Write((uint)Stream.Length);
        // compBuffer.Seek(0, SeekOrigin.Begin);
        // compBuffer.CopyTo(Stream);
    }

    public override int Write(Stream targetStream) {
        return basePacket.Write(targetStream);
    }
}

class BlockPacket : Packet {
    public BlockPacket(List<BlockCoords> coords, NetState ns) : base(0x04, 0) {
        foreach (var coord in coords) {
            var mapBlock = CEDServer.Landscape.GetMapBlock(coord.X, coord.Y);
            if (mapBlock == null) continue;
            var staticsBlock = CEDServer.Landscape.GetStaticBlock(coord.X, coord.Y);
            if (staticsBlock == null) continue;

            coord.Write(Writer);
            mapBlock.Write(Writer);
            Writer.Write((ushort)staticsBlock.Items.Count);
            staticsBlock.Write(Writer);
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
        Writer.Write(mapCell.X);
        Writer.Write(mapCell.Y);
        Writer.Write(mapCell.Altitude);
        Writer.Write(mapCell.TileId);
    }
}

class InsertStaticPacket : Packet {
    public InsertStaticPacket(StaticItem staticItem) : base(0x07, 10) {
        Writer.Write(staticItem.X);
        Writer.Write(staticItem.Y);
        Writer.Write(staticItem.Z);
        Writer.Write(staticItem.TileId);
        Writer.Write(staticItem.Hue);
    }
}

class DeleteStaticPacket : Packet {
    public DeleteStaticPacket(StaticItem staticItem) : base(0x08, 10) {
        Writer.Write(staticItem.X);
        Writer.Write(staticItem.Y);
        Writer.Write(staticItem.Z);
        Writer.Write(staticItem.TileId);
        Writer.Write(staticItem.Hue);
    }
}

class ElevateStaticPacket : Packet {
    public ElevateStaticPacket(StaticItem staticItem, sbyte newZ) : base(0x09, 11) {
        Writer.Write(staticItem.X);
        Writer.Write(staticItem.Y);
        Writer.Write(staticItem.Z);
        Writer.Write(staticItem.TileId);
        Writer.Write(staticItem.Hue);
        Writer.Write(newZ);
    }
}

class MoveStaticPacket : Packet {
    public MoveStaticPacket(StaticItem staticItem, ushort newX, ushort newY) : base(0x0A, 14) {
        Writer.Write(staticItem.X);
        Writer.Write(staticItem.Y);
        Writer.Write(staticItem.Z);
        Writer.Write(staticItem.TileId);
        Writer.Write(staticItem.Hue);
        Writer.Write(newX);
        Writer.Write(newY);
    }
}

class HueStaticPacket : Packet {
    public HueStaticPacket(StaticItem staticItem, ushort newHue) : base(0x0B, 12) {
        Writer.Write(staticItem.X);
        Writer.Write(staticItem.Y);
        Writer.Write(staticItem.Z);
        Writer.Write(staticItem.TileId);
        Writer.Write(staticItem.Hue);
        Writer.Write(newHue);
    }
}