//UOLib/UTiledata

namespace Shared;

//TTiledata
public class Tiledata : MulBlock { //Todo
    public HashSet<TiledataFlag> Flags;

    public override int Size { get; }

    public override MulBlock Clone() {
        throw new NotImplementedException();
    }

    public override void Write(BinaryWriter writer) {
        throw new NotImplementedException();
    }
}