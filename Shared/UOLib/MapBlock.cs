namespace Shared;

public class MapBlock : WorldBlock {
    public MapCell[] Cells = new MapCell[64];

    public MapBlock(BinaryReader? reader = null, ushort x = 0, ushort y = 0) {
        X = x;
        Y = y;
        if(reader != null) {
            Header = reader.ReadInt32();
            for (ushort iy = 0; iy < 8; iy++)
            for (ushort ix = 0; ix < 8; ix++)
                Cells[iy * 8 + ix] =
                    new MapCell(this, reader, (ushort)(x * 8 + ix), (ushort)(y * 8 + iy));
        }

        Changed = false;
    }

    public int Header { get; }

    public const int Size = 4 + 64 * MapCell.Size;

    public MapBlock Clone() {
        var result = new MapBlock {
            X = X,
            Y = Y
        };
        for (var i = 0; i < Cells.Length; i++) result.Cells[i] = Cells[i].Clone();
        return result;
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(Header);
        foreach (var mapCell in Cells) mapCell.Write(writer);
    }
}