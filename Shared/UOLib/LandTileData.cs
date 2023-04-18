using System.Text;

namespace Shared;

public class LandTileData : TileData {
    public static int Size(TileDataVersion version) => version switch {
        TileDataVersion.HighSeas => 30,
        _ => 26
    };

    public LandTileData(TileDataVersion version, BinaryReader? reader = null) : base(version) {
        if (reader == null) return;

        ReadFlags(reader);
        TextureId = reader.ReadUInt16();
        TileName = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
    }

    public ushort TextureId { get; set; }

    public override void Write(BinaryWriter writer) {
        WriteFlags(writer);
        writer.Write(TextureId);
        writer.Write(TileName[..20]);
    }
}