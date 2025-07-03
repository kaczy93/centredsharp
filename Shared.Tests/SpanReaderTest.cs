using System.Buffers;
using CentrED.Network;

namespace Shared.Tests;

public class SpanReaderTest
{
    [Fact]
    public void TestReadStaticInfo()
    {
        var data = new byte[]{1,0,2,0,3,4,0,5,0};
        var reader = new SpanReader(data);
        var staticInfo = reader.ReadStaticInfo();
        Assert.Equal(StaticInfo.SIZE, reader.Position );
        Assert.Equal(new StaticInfo(1,2,3,4,5), staticInfo);
    }
    
    [Fact]
    public void TestReadRect()
    {
        var data = new byte[]{1,0,2,0,3,0,4,0};
        var reader = new SpanReader(data);
        var rect = reader.ReadRectU16();
        Assert.Equal(RectU16.SIZE, reader.Position );
        Assert.Equal(new RectU16(1,2,3,4), rect);
    }

    [Fact]
    public void TestReadAreaInfo()
    {
        var data = new byte[] { 1, 0, 2, 0, 3, 0, 4, 0 };
        var reader = new SpanReader(data);
        var areaInfo = reader.ReadRectU16();
        Assert.Equal(RectU16.SIZE, reader.Position );
        Assert.Equal(new RectU16(1,2,3,4), areaInfo);;    
    }

    [Fact]
    public void TestReadBlockCoords()
    {
        var data = new byte[] { 1, 0, 2, 0 };
        var reader = new SpanReader(data);
        var coords = reader.ReadPointU16();
        Assert.Equal(PointU16.SIZE, reader.Position );
        Assert.Equal(new PointU16(1,2), coords);
    }
}