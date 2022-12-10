namespace Shared;

public class Map {
    public const int CellSize = 3;
    public const int BlockSize = 4 + 64 * CellSize;

    public static int MapCellOffset(int block) {
        var group = block / 64;
        var tile = block % 64;

        return group * BlockSize + 4 + tile * CellSize;
    }
}