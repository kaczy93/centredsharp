namespace Shared;

public class LandTile : Tile<LandBlock> {
    public const int Size = 3;
    public static LandTile Empty => new() { _tileId = 0, _z = 0 };

    public LandTile(LandBlock? owner = null, BinaryReader? reader = null, ushort x = 0, ushort y = 0) : base(owner) {
        _x = x;
        _y = y;
        if (reader != null) {
            _tileId = reader.ReadUInt16();
            _z = reader.ReadSByte();
        }
    }

    public override void Write(BinaryWriter writer) {
        writer.Write(_tileId);
        writer.Write(_z);
    }
}