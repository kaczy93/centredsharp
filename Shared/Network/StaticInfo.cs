using System.Runtime.InteropServices;

namespace CentrED.Network; 

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticInfo {
    public StaticInfo(BinaryReader buffer) {
        X = buffer.ReadUInt16();
        Y = buffer.ReadUInt16();
        Z = buffer.ReadSByte();
        TileId = buffer.ReadUInt16();
        Hue = buffer.ReadUInt16();
    }
    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort TileId { get; }
    public ushort Hue { get; }
        
    public bool Match(StaticTile s) => s.Z == Z && s.TileId == TileId && s.Hue == Hue;

    public void Serialize(BinaryWriter writer) {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(TileId);
        writer.Write(Hue);
    }
}