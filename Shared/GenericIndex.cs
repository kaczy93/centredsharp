//UOLib/UGenericIndex.pas

namespace Shared;

//TGenericIndex
public class GenericIndex : MulBlock { //Todo

    public int Lookup;
    public override int Size { get; }

    public override MulBlock Clone() {
        throw new NotImplementedException();
    }

    public override void Write(BinaryWriter writer) {
        throw new NotImplementedException();
    }
}