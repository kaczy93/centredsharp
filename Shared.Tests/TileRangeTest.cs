using CentrED;

namespace Shared.Tests;

public class TileRangeTest
{
    [Fact]
    public void SingleChunk()
    {
        var iterator = new TileRange(0, 0, 7, 7);
        var result = string.Join("|", iterator.Take(64).Select(t => $"{t.x},{t.y}"));
        Assert.Equal("0,0|1,0|2,0|3,0|4,0|5,0|6,0|7,0|" + 
                     "0,1|1,1|2,1|3,1|4,1|5,1|6,1|7,1|" +
                     "0,2|1,2|2,2|3,2|4,2|5,2|6,2|7,2|" + 
                     "0,3|1,3|2,3|3,3|4,3|5,3|6,3|7,3|" +
                     "0,4|1,4|2,4|3,4|4,4|5,4|6,4|7,4|" +
                     "0,5|1,5|2,5|3,5|4,5|5,5|6,5|7,5|" +
                     "0,6|1,6|2,6|3,6|4,6|5,6|6,6|7,6|" +
                     "0,7|1,7|2,7|3,7|4,7|5,7|6,7|7,7", 
                     result);
    }
    
    [Fact]
    public void Cross4Chunks()
    {
        var iterator = new TileRange(6, 6, 9, 9);
        var result = string.Join("|", iterator.Take(16).Select(t => $"{t.x},{t.y}"));
        Assert.Equal("6,6|7,6|"+// Block 0,0
                     "6,7|7,7|"+
                     "6,8|7,8|"+// Block 0,1
                     "6,9|7,9|"+
                     "8,6|9,6|"+// Block 1,0
                     "8,7|9,7|"+
                     "8,8|9,8|"+// Block 1,1
                     "8,9|9,9",
                     result);
    }

    [Fact]
    public void SingleChunkXReversed()
    {
        var iterator = new TileRange(7, 0, 0, 7);
        var result = string.Join("|", iterator.Take(64).Select(t => $"{t.x},{t.y}"));
        Assert.Equal("7,0|6,0|5,0|4,0|3,0|2,0|1,0|0,0|" +
                     "7,1|6,1|5,1|4,1|3,1|2,1|1,1|0,1|" +
                     "7,2|6,2|5,2|4,2|3,2|2,2|1,2|0,2|" +
                     "7,3|6,3|5,3|4,3|3,3|2,3|1,3|0,3|" +
                     "7,4|6,4|5,4|4,4|3,4|2,4|1,4|0,4|" +
                     "7,5|6,5|5,5|4,5|3,5|2,5|1,5|0,5|" +
                     "7,6|6,6|5,6|4,6|3,6|2,6|1,6|0,6|" +
                     "7,7|6,7|5,7|4,7|3,7|2,7|1,7|0,7", 
                     result);
    }

    [Fact]
    public void SingleChunkYReversed()
    {
        var iterator = new TileRange(0, 7, 7, 0);
        var result = string.Join("|", iterator.Take(64).Select(t => $"{t.x},{t.y}"));

        
        Assert.Equal("0,7|1,7|2,7|3,7|4,7|5,7|6,7|7,7|" + 
                     "0,6|1,6|2,6|3,6|4,6|5,6|6,6|7,6|" +
                     "0,5|1,5|2,5|3,5|4,5|5,5|6,5|7,5|" + 
                     "0,4|1,4|2,4|3,4|4,4|5,4|6,4|7,4|" +
                     "0,3|1,3|2,3|3,3|4,3|5,3|6,3|7,3|" + 
                     "0,2|1,2|2,2|3,2|4,2|5,2|6,2|7,2|" +
                     "0,1|1,1|2,1|3,1|4,1|5,1|6,1|7,1|" + 
                     "0,0|1,0|2,0|3,0|4,0|5,0|6,0|7,0",
                     result);
    }
    
    [Fact]
    public void SingleChunkXYReversed()
    {
        var iterator = new TileRange(7, 7, 0, 0);
        var result = string.Join("|", iterator.Take(64).Select(t => $"{t.x},{t.y}"));
        Assert.Equal("7,7|6,7|5,7|4,7|3,7|2,7|1,7|0,7|" +
                     "7,6|6,6|5,6|4,6|3,6|2,6|1,6|0,6|" +
                     "7,5|6,5|5,5|4,5|3,5|2,5|1,5|0,5|" +
                     "7,4|6,4|5,4|4,4|3,4|2,4|1,4|0,4|" +
                     "7,3|6,3|5,3|4,3|3,3|2,3|1,3|0,3|" +
                     "7,2|6,2|5,2|4,2|3,2|2,2|1,2|0,2|" +
                     "7,1|6,1|5,1|4,1|3,1|2,1|1,1|0,1|" +
                     "7,0|6,0|5,0|4,0|3,0|2,0|1,0|0,0", 
                     result);
    }

    [Fact]
    public void UshortLimits()
    {
        var max = ushort.MaxValue;
        var maxMinusOne = (ushort)(max - 1);
        var iterator = new TileRange(maxMinusOne, maxMinusOne, max, max);
        var result = string.Join("|", iterator.Take(4).Select(t => $"{t.x},{t.y}"));
        Assert.Equal($"{maxMinusOne},{maxMinusOne}|{max},{maxMinusOne}|{maxMinusOne},{max}|{max},{max}",
                     result);
    }
    
    [Fact]
    public void UshortLimitsReversed()
    {
        var iterator = new TileRange(1, 1, 0, 0);
        var result = string.Join("|", iterator.Take(4).Select(t => $"{t.x},{t.y}"));
        Assert.Equal($"1,1|0,1|1,0|0,0",
                     result);
    }

    [Fact]
    public void XLine()
    {
        var iterator = new TileRange(0, 0, 8, 0);
        var result = string.Join("|", iterator.Take(9).Select(t => $"{t.x},{t.y}"));
        Assert.Equal($"0,0|1,0|2,0|3,0|4,0|5,0|6,0|7,0|8,0",
                     result);
    }
    
    [Fact]
    public void YLine()
    {
        var iterator = new TileRange(0, 0, 0, 8);
        var result = string.Join("|", iterator.Take(9).Select(t => $"{t.x},{t.y}"));
        Assert.Equal($"0,0|0,1|0,2|0,3|0,4|0,5|0,6|0,7|0,8",
                     result);
    }
    
    [Fact]
    public void SingleTile()
    {
        var iterator = new TileRange(5,5,5,5);
        var result = string.Join("|", iterator.Select(t => $"{t.x},{t.y}"));
        Assert.Equal($"5,5",
                     result);
    }
}