namespace Shared;

public class VirtualTile : WorldItem {
    public VirtualTile(WorldBlock owner) : base(owner) { }

    public override int Size { get; }

    public override MulBlock Clone() {
        throw new NotImplementedException();
    }

    public override void Write(BinaryWriter writer) {
        throw new NotImplementedException();
    }
}