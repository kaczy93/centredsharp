using System.Runtime.InteropServices;

namespace CentrED.Network; 

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticInfo {
    public StaticInfo(BinaryReader reader) {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        Z = reader.ReadSByte();
        TileId = reader.ReadUInt16();
        Hue = reader.ReadUInt16();
    }
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    public ushort Hue { get; }
        
    public bool Match(StaticTile s) => s.Z == Z && s.Id == TileId && s.Hue == Hue;

    public void Serialize(BinaryWriter writer) {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(TileId);
        writer.Write(Hue);
    }
}