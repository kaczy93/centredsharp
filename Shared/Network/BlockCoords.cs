namespace CentrED.Network; 

public record BlockCoords(ushort X, ushort Y) {
    public BlockCoords(BinaryReader reader) : this(0, 0) {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(X);
        writer.Write(Y);
    }
};
