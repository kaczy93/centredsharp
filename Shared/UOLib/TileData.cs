namespace Shared;

public abstract class TileData : MulEntry{
    protected TileData(TileDataVersion tileDataVersion) {
        Version = tileDataVersion;
    }
    
    protected TileDataVersion Version;
    public TiledataFlag Flags { get; set; }
    public string TileName { get; set; } = "";
    
    protected void ReadFlags(BinaryReader reader) {
        Flags = Version switch {
            TileDataVersion.HighSeas => (TiledataFlag)reader.ReadUInt64(),
            _ => (TiledataFlag)reader.ReadUInt32()
        };
    }

    protected void WriteFlags(BinaryWriter writer) {
        writer.Write((ulong)Flags);
    }
}