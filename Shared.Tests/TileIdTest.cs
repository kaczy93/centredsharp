using CentrED;
using CentrED.Client.Map;
using ClientPackets = CentrED.Client;
using ServerPackets = CentrED.Server;

namespace Shared.Tests;

public class TileIdTest
{
    private static BinaryWriter Writer() => new(new MemoryStream());

    [Theory]
    [InlineData(0x10000u)]
    [InlineData(uint.MaxValue)]
    public void IdsThatDontFitAreRefusedInsteadOfTruncated(uint id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Writer().WriteTileId(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StaticTile(id, 0, 0, 0, 0).Write(Writer()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LandTile(id, 0, 0, 0).Write(Writer()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClientPackets.DrawMapPacket(0, 0, 0, id));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServerPackets.DrawMapPacket(new LandTile(id, 0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LSODrawLand([id]).Write(Writer()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Writer().WriteLsoStaticId(id + (id < uint.MaxValue ? TileId.LSO_OFFSET : 0)));
    }

    [Theory]
    [InlineData(0xC001u, "0100")]
    [InlineData(0xFFFFu, "FF3F")]
    public void ShiftedIdsWrapOnTheWire(uint id, string expected)
    {
        var stream = new MemoryStream();
        new BinaryWriter(stream).WriteLsoStaticId(id + TileId.LSO_OFFSET);
        Assert.Equal(expected, Convert.ToHexString(stream.ToArray()));
    }

    [Fact]
    public void BaseTileIdIsTheTileId()
    {
        BaseTile land = new LandTile(0x12345, 0, 0, 0) { GhostId = 0x23456 };
        BaseTile staticTile = new StaticTile(0x12345, 0, 0, 0, 0);
        Assert.Equal(0x23456u, land.Id);
        Assert.Equal(0x12345u, ((LandTile)land).RealId);
        Assert.Equal(0x12345u, staticTile.Id);
    }
}
