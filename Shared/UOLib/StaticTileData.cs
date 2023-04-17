using System.Text;

namespace Shared;

public class StaticTileData : TileData {

    public StaticTileData(BinaryReader? reader = null, TileDataVersion version = TileDataVersion.Legacy) {
        this.version = version;
        if (reader == null) return;
        
        ReadFlags(reader);
        Weight = reader.ReadByte();
        Quality = reader.ReadByte();
        Unknown1 = reader.ReadUInt16();
        Unknown2 = reader.ReadByte();
        Quantity = reader.ReadByte();
        AnimId = reader.ReadUInt16();
        Unknown3 = reader.ReadByte();
        Hue = reader.ReadByte();
        Unknown4 = reader.ReadUInt16();
        TileName = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
    }
    
    public byte Weight { get; set; }
    public byte Quality { get; set; }
    public ushort Unknown1 { get; set; }
    public byte Unknown2 { get; set; }
    public byte Quantity { get; set; }
    public ushort AnimId { get; set; }
    public byte Unknown3 { get; set; }
    public byte Hue { get; set; }
    public ushort Unknown4 { get; set; }
    public byte Height { get; set; }

    protected void PopulateClone(StaticTileData clone) {
        clone.Weight = Weight;
        clone.Quality = Quality;
        clone.Unknown1 = Unknown1;
        clone.Unknown2 = Unknown2;
        clone.Quantity = Quantity;
        clone.AnimId = AnimId;
        clone.Unknown3 = Unknown3;
        clone.Hue = Hue;
        clone.Unknown4 = Unknown4;
        clone.Height = Height;
    }

    public StaticTileData Clone() {
        StaticTileData result = new StaticTileData();
        PopulateClone(result);
        return result;
    }

    public override void Write(BinaryWriter writer) {
        WriteFlags(writer);
        writer.Write(Weight);
        writer.Write(Quality);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Quantity);
        writer.Write(AnimId);
        writer.Write(Unknown3);
        writer.Write(Hue);
        writer.Write(Unknown4);
        writer.Write(Height);
        writer.Write(TileName[..20]);
    }
}