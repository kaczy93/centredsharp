using System.Buffers;
using CentrED;
using CentrED.Client.Map;
using CentrED.Network;
using CentrED.Server.Map;
using ClientPackets = CentrED.Client;
using ServerPackets = CentrED.Server;

namespace Shared.Tests;

// Static ids are 16 bit on disk and on the wire, whatever their type is in memory.
// Expected bytes are literals on purpose: BinaryWriter.Write picks its width from the type of the argument.
public class StaticIdWireFormatTest
{
    private class DummyLandscape() : BaseLandscape(8, 8)
    {
        protected override Block LoadBlock(ushort x, ushort y) => throw new NotImplementedException();
        public override void LogInfo(string message) => throw new NotImplementedException();
        public override void LogWarn(string message) => throw new NotImplementedException();
        public override void LogError(string message) => throw new NotImplementedException();
        public override void LogDebug(string message) => throw new NotImplementedException();
    }

    private static StaticTile Tile() => new(0xBEEF, 0x1234, 0x5678, -5, 0x0A0B);

    private static void AssertBytes(string expected, ReadOnlySpan<byte> actual) =>
        Assert.Equal(expected, Convert.ToHexString(actual));

    private static string Hex(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        write(writer);
        writer.Flush();
        return Convert.ToHexString(stream.ToArray());
    }

    [Fact]
    public void StaticTileOnDisk() => Assert.Equal("EFBE0400FB0B0A", Hex(w => Tile().Write(w)));

    [Fact]
    public void StaticInfoOnWire() =>
        Assert.Equal("34127856FBEFBE0B0A", Hex(w => new StaticInfo(0x1234, 0x5678, -5, 0xBEEF, 0x0A0B).Write(w)));

    [Theory]
    [InlineData("0000")]
    [InlineData("FF3F")]
    [InlineData("00C0")]
    [InlineData("FFFF")]
    public void StaticBlockRoundTrip(string id)
    {
        var entry = id + "0203040500";
        var bytes = Convert.FromHexString(entry);
        var fromDisk = new StaticTile(0, 0, new BinaryReader(new MemoryStream(bytes)));
        Assert.Equal(entry, Hex(fromDisk.Write));

        var fromWire = new StaticBlock(new DummyLandscape(), 0, 0, new SpanReader(bytes));
        Assert.Equal(entry, Hex(fromWire.Write));
        Assert.Equal(7, fromWire.TotalSize);
    }

    [Fact]
    public void StaticInfoFromWire()
    {
        var reader = new SpanReader(Convert.FromHexString("34127856FBEFBE0B0A"));
        Assert.Equal(new StaticInfo(0x1234, 0x5678, -5, 0xBEEF, 0x0A0B), reader.ReadStaticInfo());
        Assert.Equal(9, reader.Position);
    }

    [Fact]
    public void ClientDrawMap() =>
        AssertBytes("0634127856FBEFBE", new ClientPackets.DrawMapPacket(0x1234, 0x5678, -5, 0xBEEF).Compile());

    [Fact]
    public void ClientInsertStatic() =>
        AssertBytes("0734127856FBEFBE0B0A", new ClientPackets.InsertStaticPacket(Tile()).Compile());

    [Fact]
    public void ClientDeleteStatic() =>
        AssertBytes("0834127856FBEFBE0B0A", new ClientPackets.DeleteStaticPacket(Tile()).Compile());

    [Fact]
    public void ClientElevateStatic() =>
        AssertBytes("0934127856FBEFBE0B0A07", new ClientPackets.ElevateStaticPacket(Tile(), 7).Compile());

    [Fact]
    public void ClientMoveStatic() =>
        AssertBytes("0A34127856FBEFBE0B0A21436587", new ClientPackets.MoveStaticPacket(Tile(), 0x4321, 0x8765).Compile());

    [Fact]
    public void ClientHueStatic() =>
        AssertBytes("0B34127856FBEFBE0B0A0D0C", new ClientPackets.HueStaticPacket(Tile(), 0x0C0D).Compile());

    [Fact]
    public void ServerDrawMap() =>
        AssertBytes("0634127856FBEFBE", new ServerPackets.DrawMapPacket(new LandTile(0xBEEF, 0x1234, 0x5678, -5)).Compile());

    [Fact]
    public void ServerInsertStatic() =>
        AssertBytes("0734127856FBEFBE0B0A", new ServerPackets.InsertStaticPacket(Tile()).Compile());

    [Fact]
    public void ServerDeleteStatic() =>
        AssertBytes("0834127856FBEFBE0B0A", new ServerPackets.DeleteStaticPacket(Tile()).Compile());

    [Fact]
    public void ServerElevateStatic() =>
        AssertBytes("0934127856FBEFBE0B0A07", new ServerPackets.ElevateStaticPacket(Tile(), 7).Compile());

    [Fact]
    public void ServerMoveStatic() =>
        AssertBytes("0A34127856FBEFBE0B0A21436587", new ServerPackets.MoveStaticPacket(Tile(), 0x4321, 0x8765).Compile());

    [Fact]
    public void ServerHueStatic() =>
        AssertBytes("0B34127856FBEFBE0B0A0D0C", new ServerPackets.HueStaticPacket(Tile(), 0x0C0D).Compile());

    [Fact]
    public void DrawLandOperation() =>
        Assert.Equal("02000300EFBE", Hex(new LSODrawLand([0x0003, 0xBEEF]).Write));

    // LSO static ids carry a +0x4000 offset and wrap in 16 bit: 0xC001 travels as 0x0001
    [Fact]
    public void DeleteStaticsOperation()
    {
        var hex = Hex(new LSODeleteStatics([0x4000, 0xFFFF, 0x0001], -5, 7).Write);
        Assert.Equal("03000040FFFF0100FB07", hex);

        var reader = new SpanReader(Convert.FromHexString(hex));
        var operation = new LsDeleteStatics(ref reader);
        Assert.Equal([0, 0xBFFF, 0xC001], operation.TileIds);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void AddStaticsOperation()
    {
        var hex = Hex(new LSOAddStatics([0x4000, 0xFFFF, 0x0001], 50, LSO.StaticsPlacement.Fix, -5).Write);
        Assert.Equal($"03000040FFFF010032{(byte)LSO.StaticsPlacement.Fix:X2}FB", hex);

        var reader = new SpanReader(Convert.FromHexString(hex));
        var operation = new LsInsertStatics(ref reader);
        Assert.Equal([0, 0xBFFF, 0xC001], operation.TileIds);
        Assert.Equal(0, reader.Remaining);
    }
}
