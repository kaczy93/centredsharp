using ClassicUO.IO;

namespace CentrED.Utils;

public static class UOFileIndexExtensions
{
    //GetValidRefEntry() returns UOFileIndex.Invalid (Length 0) for anything unusable
    public static bool IsValid(this in UOFileIndex entry) => entry.Length > 0;
}
