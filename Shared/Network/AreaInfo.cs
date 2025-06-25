using System.Buffers;
using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AreaInfo
{
    public AreaInfo(ushort x1, ushort y1, ushort x2, ushort y2)
    {
        Left = Math.Min(x1, x2);
        Top = Math.Min(y1, y2);
        Right = Math.Max(x1, x2);
        Bottom = Math.Max(y1, y2);
    }

    public const int SIZE = 8;

    public ushort Width => (ushort)(Right - Left + 1);
    public ushort Height => (ushort)(Bottom - Top + 1);
    public ushort Left { get; }
    public ushort Top { get; }
    public ushort Right { get; }
    public ushort Bottom { get; }

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