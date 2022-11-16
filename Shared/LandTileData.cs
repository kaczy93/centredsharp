//UOLib/UTiledata.pas
using System.Text;

namespace Shared; 

//TLandTiledata
public class LandTileData : Tiledata {

    public LandTileData(Stream? stream, TileDataVersion version = TileDataVersion.Legacy) {
        this.version = version;
        if (stream != null) {
            ReadFlags(stream);
            using var reader = new BinaryReader(stream);
            TextureId = reader.ReadUInt16();
            TileName = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
        }
    }

    public ushort TextureId { get; set; }

    public void PopulateClone(LandTileData clone) {
        clone.TextureId = TextureId;
    }

    public override int GetSize => LandTileDataSize;
    
    public override MulBlock Clone() {
       LandTileData result = new LandTileData(null);
       PopulateClone(result); // This is stupid, fix me
       return result; 
    }

    public override void Write(BinaryWriter writer) {
        WriteFlags(writer);
        writer.Write(TextureId);
        writer.Write(TileName[..20]);
    }
}