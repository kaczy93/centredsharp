using System.Buffers;
using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AreaInfo(ushort Left, ushort Top, ushort Right, ushort Bottom)
{
    public const int SIZE = 8;

    public ushort Width => (ushort)(Right - Left);
    public ushort Height => (ushort)(Bottom - Top);
    
    public void Write(BinaryWriter writer)
    {
        writer.Write(Left);
        writer.Write(Top);
        writer.Write(Right);
        writer.Write(Bottom);
    }
}

public static class SpanReaderAreaInfo
{
    public static AreaInfo ReadAreaInfo(this ref SpanReader reader)
    {
        var left = reader.ReadUInt16();
        var top = reader.ReadUInt16();
        var right = reader.ReadUInt16();
        var bottom = reader.ReadUInt16();
        return new AreaInfo(Math.Min(left, right), Math.Min(top, bottom), Math.Max(left, right), Math.Max(top, bottom));
    }
}