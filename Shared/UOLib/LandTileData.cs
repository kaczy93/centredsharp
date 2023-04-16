using System.Text;

namespace Shared; 

public class LandTileData : TileData {

    public LandTileData(Stream? data = null, TileDataVersion version = TileDataVersion.Legacy) {
        this.version = version;
        if (data == null) return;
        
        using var reader = new BinaryReader(data, Encoding.UTF8, true);
        ReadFlags(reader);
        TextureId = reader.ReadUInt16();
        TileName = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
    }

    public ushort TextureId { get; set; }

    public void PopulateClone(LandTileData clone) {
        clone.TextureId = TextureId;
    }

    
    public LandTileData Clone() {
       LandTileData result = new LandTileData();
       PopulateClone(result); // This is stupid, fix me
       return result; 
    }

    public override void Write(BinaryWriter writer) {
        WriteFlags(writer);
        writer.Write(TextureId);
        writer.Write(TileName[..20]);
    }
}