//Server/ULandscape.pas

using Shared;

namespace Server; 

//TLandscape
public class Landscape {
    public MapBlock? GetMapBlock(ushort coordX, ushort coordY) {
        throw new NotImplementedException();
    }

    public StaticBlock? GetStaticBlock(ushort coordX, ushort coordY) {
        throw new NotImplementedException();
    }

    //TODO: LinkedList of what?
    //Also linkedList in original code is a custom class
    public LinkedList<object> BlockSubscriptions(ushort coordX, ushort coordY) {
        throw new NotImplementedException();
    }
}