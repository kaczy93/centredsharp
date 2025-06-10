using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AreaInfo
{
    public AreaInfo(ushort left, ushort top, ushort right, ushort bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
    
    // Include both boundaries when calculating the size so that
    // callers requesting blocks for an inclusive area allocate
    // enough space. The previous implementation subtracted the
    // coordinates directly, which caused off-by-one errors and
    // could evict neighbour blocks prematurely during generation.
    public ushort Width => (ushort)(Right - Left + 1);
    public ushort Height => (ushort)(Bottom - Top + 1);

    public AreaInfo(BinaryReader reader)
    {
        var left = reader.ReadUInt16();
        var top = reader.ReadUInt16();
        var right = reader.ReadUInt16();
        var bottom = reader.ReadUInt16();
        Left = Math.Min(left, right);
        Top = Math.Min(top, bottom);
        Right = Math.Max(left, right);
        Bottom = Math.Max(top, bottom);
    }

    public ushort Left { get; set; }
    public ushort Top { get; set; }
    public ushort Right { get; set; }
    public ushort Bottom { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Left);
        writer.Write(Top);
        writer.Write(Right);
        writer.Write(Bottom);
    }
}