using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AreaInfo {
    public AreaInfo(BinaryReader reader) {
        Left = reader.ReadUInt16();
        Top = reader.ReadUInt16();
        Right = reader.ReadUInt16();
        Bottom = reader.ReadUInt16();
    }
    
    public ushort Left { get; set; }
    public ushort Top { get; set; }
    public ushort Right { get; set; }
    public ushort Bottom { get; set; }

    public void Serialize(BinaryWriter writer) {
        writer.Write(Left);
        writer.Write(Top);
        writer.Write(Right);
        writer.Write(Bottom);
    }
}