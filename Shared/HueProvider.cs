namespace CentrED;

public static class HueProvider
{
    public static bool GetHueCount(string huePath, out int hueCount)
    {
        if (File.Exists(huePath))
        {
            using var file = File.Open(huePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //Header + 8 entries * (colortable + tablestart + tableend + name)
            int groupSize = 4 + 8 * (32 * 2 + 2 + 2 + 20);
            int entrycount = (int)file.Length / groupSize;
            hueCount = entrycount * 8;
            return true;
        }
        hueCount = 3000;
        return false;
    }
}