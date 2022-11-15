//UOLib/UStatics.pas
namespace Shared; 

//TStaticItem
public class StaticItem : WorldItem{
    
    public StaticItem(WorldBlock owner) : base(owner) { }
    
    public override int Size { get; }
    
    public override MulBlock Clone() {
        throw new NotImplementedException();
    }

    public override void Write(BinaryWriter writer) {
        throw new NotImplementedException();
    }
}