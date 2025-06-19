using System.Buffers;
using CentrED.Network;

namespace Shared.Tests;

public class ZlibTest
{
    [Fact]
    public void TestZlib()
    {
        var netstate = new DummyNetstate();
        netstate.RegisterPacketHandler(0x01, 0, Zlib.OnCompressedPacket);
        netstate.RegisterPacketHandler(0x02, 5, TestMethod);
        var data = new byte[] { 
            2, //PacketID
            3, 4, 5, 6 //Data
        };
        netstate.SendCompressed(data);
        //We "flush" to the same dummy netstate recv pipe
        netstate.GetSendPipe.Reader.AvailableToRead().CopyTo(netstate.GetRecvPipe.Writer.AvailableToWrite());
        
        netstate.InvokeProcessBuffer();
        Assert.Equal(0, netstate.GetRecvPipe.Reader.AvailableToRead().Length);
    }

    public static void TestMethod(SpanReader reader, NetState<DummyLogging> ns)
    {
        var expected = new byte[] { 3, 4, 5, 6 };
        var buffer = new byte[4];
        reader.Read(buffer);
        Assert.Equal(expected, buffer);
    }
}