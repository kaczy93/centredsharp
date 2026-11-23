using CentrED;

namespace Shared.Tests;

public class StaticIdTest
{
    private static BinaryWriter Writer() => new(new MemoryStream());

    [Theory]
    [InlineData(0x10000u)]
    [InlineData(uint.MaxValue)]
    public void IdsThatDontFitAreRefusedInsteadOfTruncated(uint id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Writer().WriteStaticId(id));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StaticTile(id, 0, 0, 0, 0).Write(Writer()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Writer().WriteLsoStaticId(id + (id < uint.MaxValue ? StaticId.LSO_OFFSET : 0)));
    }

    [Theory]
    [InlineData(0xC001u, "0100")]
    [InlineData(0xFFFFu, "FF3F")]
    public void ShiftedIdsWrapOnTheWire(uint id, string expected)
    {
        var stream = new MemoryStream();
        new BinaryWriter(stream).WriteLsoStaticId(id + StaticId.LSO_OFFSET);
        Assert.Equal(expected, Convert.ToHexString(stream.ToArray()));
    }

    [Fact]
    public void BaseTileExposesBothIdWidths()
    {
        BaseTile land = new LandTile(0x1234, 0, 0, 0) { GhostId = 0x3000 };
        BaseTile staticTile = new StaticTile(0x12345, 0, 0, 0, 0);
        Assert.Equal(0x3000u, land.Id);
        Assert.Equal(0x12345u, staticTile.Id);
    }
}
