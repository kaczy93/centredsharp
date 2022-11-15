//UOLib/UMap.pas

namespace Shared;

//UMap
public class Map {
    public static int CellSize = 3;
    public static int BlockSize = 4 + 64 * CellSize;

    public static int MapCellOffset(int block) {
        var group = block / 64;
        var tile = block % 64;

        return group * BlockSize + 4 + tile * CellSize;
    }
}