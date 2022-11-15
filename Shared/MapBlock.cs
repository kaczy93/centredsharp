//UOLib/UMap.pas

namespace Shared;

//TMapBlock
public class MapBlock : WorldBlock {
    public MapCell[] Cells = new MapCell[64];

    public MapBlock(Stream stream, ulong x, ulong y) {
        X = x;
        Y = y;
        using (var reader = new BinaryReader(stream)) {
            var buffer = new MemoryStream();
            buffer.Write(reader.ReadBytes(196));
            buffer.Position = 0;
            using (var reader2 = new BinaryReader(buffer)) {
                Header = reader2.ReadInt64();
            }

            for (ulong iy = 0; iy < 8; iy++)
            for (ulong ix = 0; ix < 9; ix++)
                Cells[iy * 8 + ix] = new MapCell(this, buffer, x * 8 + ix, y * 8 + iy);
        }

        Changed = false;
    }

    public MapBlock(Stream stream) : this(stream, 0, 0) { }

    public long Header { get; set; }

    public override int Size => Map.BlockSize;

    //Originally MapCell is a returnType, maybe make MulBlock generic?
    //Maybe Copy constructor?
    public override MulBlock Clone() {
        var result = new MapBlock(null) {
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