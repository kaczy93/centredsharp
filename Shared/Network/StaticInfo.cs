using System.Runtime.InteropServices;

namespace CentrED.Network;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StaticInfo
{
    public StaticInfo(BinaryReader reader)
    {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        Z = reader.ReadSByte();
        Id = reader.ReadUInt16();
        Hue = reader.ReadUInt16();
    }

    public ushort X { get; }
    public ushort Y { get; }
    public sbyte Z { get; }
    public ushort Id { get; }
    public ushort Hue { get; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(Id);
        writer.Write(Hue);
    }

    public override string ToString()
    {
        return $"{Id}:{X},{Y},{Z} {Hue}";
    }
}