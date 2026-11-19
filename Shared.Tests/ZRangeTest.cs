using CentrED;

namespace Shared.Tests;

public class ZRangeTest
{
    private class DummyLandscape : BaseLandscape
    {
        public DummyLandscape() : base(8, 8)
        {
        }

        protected override Block LoadBlock(ushort x, ushort y) => throw new NotImplementedException();
        public override void LogInfo(string message) => throw new NotImplementedException();
        public override void LogWarn(string message) => throw new NotImplementedException();
        public override void LogError(string message) => throw new NotImplementedException();
        public override void LogDebug(string message) => throw new NotImplementedException();
    }

    [Fact]
    public void LandBlockRoundTripIsByteIdentical()
    {
        var input = new MemoryStream();
        var inputWriter = new BinaryWriter(input);
        inputWriter.Write(0x12345678);
        for (var i = 0; i < 64; i++)
        {
            inputWriter.Write((ushort)(0x100 + i));
            inputWriter.Write((sbyte)(i * 4 - 128));
        }
        var bytes = input.ToArray();
        Assert.Equal(LandBlock.SIZE, bytes.Length);

        var block = new LandBlock(new DummyLandscape(), 0, 0, new BinaryReader(new MemoryStream(bytes)));
        Assert.Equal(-128, block.Tiles[0].Z);
        Assert.Equal(124, block.Tiles[63].Z);

        var output = new MemoryStream();
        block.Write(new BinaryWriter(output));
        Assert.Equal(bytes, output.ToArray());
    }

    [Fact]
    public void StaticTileRoundTripIsByteIdentical()
    {
        byte[] bytes = [0x34, 0x12, 3, 5, unchecked((byte)-128), 0x21, 0x04];
        Assert.Equal(StaticTile.SIZE, bytes.Length);

        var tile = new StaticTile(1, 2, new BinaryReader(new MemoryStream(bytes)));
        Assert.Equal(-128, tile.Z);

        var output = new MemoryStream();
        tile.Write(new BinaryWriter(output));
        Assert.Equal(bytes, output.ToArray());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(127, 127)]
    [InlineData(-128, -128)]
    [InlineData(200, 127)]
    [InlineData(-300, -128)]
    public void WriteZClampsToSByte(short z, sbyte expected)
    {
        var output = new MemoryStream();
        new BinaryWriter(output).WriteZ(z);
        Assert.Equal([unchecked((byte)expected)], output.ToArray());
    }

    [Fact]
    public void ConstructorsClampZ()
    {
        Assert.Equal(127, new LandTile(0, 0, 0, 300).Z);
        Assert.Equal(-128, new StaticTile(0, 0, 0, -300, 0).Z);
    }

    [Fact]
    public void SettersRaiseClampedZ()
    {
        var landscape = new DummyLandscape();
        var landTile = new LandTile(new LandBlock(landscape), 0, 0, 0, 0);
        var staticBlock = new StaticBlock(landscape, 0, 0);
        var staticTile = new StaticTile(0, 0, 0, 0, 0);
        staticBlock.AddTile(staticTile);

        var raised = new List<short>();
        landscape.LandTileElevated += (_, z) => raised.Add(z);
        landscape.LandTileReplaced += (_, _, z) => raised.Add(z);
        landscape.StaticTileElevated += (_, z) => raised.Add(z);

        landTile.Z = 300;
        landTile.ReplaceLand(1, -300);
        staticTile.Z = 200;
        staticTile.UpdatePos(0, 0, -200);

        Assert.Equal([127, -128, 127, -128], raised);
    }
}
