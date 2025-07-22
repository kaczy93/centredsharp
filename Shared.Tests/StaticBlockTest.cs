using CentrED;

namespace Shared.Tests;

public class StaticBlockTest
{
    private class DummyLandscape : BaseLandscape
    {
        public DummyLandscape(ushort width, ushort height) : base(width, height)
        {
        }

        protected override Block LoadBlock(ushort x, ushort y)
        {
            throw new NotImplementedException();
        }

        public override void LogInfo(string message)
        {
            throw new NotImplementedException();
        }

        public override void LogWarn(string message)
        {
            throw new NotImplementedException();
        }

        public override void LogError(string message)
        {
            throw new NotImplementedException();
        }

        public override void LogDebug(string message)
        {
            throw new NotImplementedException();
        }
    }
    
    [Fact]
    public void DeduplicateTest()
    {
        var block = new StaticBlock(new DummyLandscape(128, 128), 0, 0);
        block.AddTile(new StaticTile(0,1,2,3,4));
        block.AddTile(new StaticTile(0,1,2,3,4));
        block.AddTile(new StaticTile(0,1,2,3,4));
        Assert.Equal(3, block.TotalTilesCount);
        block.Deduplicate();
        Assert.Equal(1, block.TotalTilesCount);
    }
}