//UOLib/UMap.pas

namespace Shared;

//TMapBlock
public class MapBlock : WorldBlock {
    public MapCell[] Cells = new MapCell[64];

    public MapBlock(Stream? data = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        if(data != null) {
            using var reader = new BinaryReader(data);
            var buffer = new MemoryStream();
            buffer.Write(reader.ReadBytes(196));
            buffer.Position = 0;
            using (var reader2 = new BinaryReader(buffer)) {
                Header = reader2.ReadInt32();
                for (ushort iy = 0; iy < 8; iy++)
                for (ushort ix = 0; ix < 8; ix++)
                    Cells[iy * 8 + ix] =
                        new MapCell(this, buffer, (ushort)(x * 8 + ix),
                            (ushort)(y * 8 + iy)); //This casting to ushort is fishy :/
            }
        }

        Changed = false;
    }

    public int Header { get; set; }

    public override int GetSize => Map.BlockSize;

    //Originally MapCell is a returnType, maybe make MulBlock generic?
    //Maybe Copy constructor?
    public override MulBlock Clone() {
        var result = new MapBlock {
            X = X,
            Y = Y
        };
        for (var i = 0; i < Cells.Length; i++) result.Cells[i] = (MapCell)Cells[i].Clone();
        return result;
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Header);
        foreach (var mapCell in Cells) mapCell.Write(writer);
    }
}