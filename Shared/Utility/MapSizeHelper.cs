namespace CentrED.Utility;

public static class MapSizeHelper
{
    
    public static void StaidxSizeHint(string filePath, out ushort width, out ushort height, out string desc)
    {
        using var file = File.OpenRead(filePath);
        switch (file.Length)
        {
            case 196_608:
                width = 128;
                height = 128;
                desc = "map0 Pre-Alpha";
                break;
            case 4_718_592:
                width = 768;
                height = 512;
                desc = "map0,map1 Pre-ML";
                break;
            case 5_505_024:
                width = 896;
                height = 512;
                desc = "map0,map1 Post-ML";
                break;
            case 691_200:
                width = 288;
                height = 200;
                desc = "map2";
                break;
            case 983_040:
                if (Path.GetFileName(filePath).Contains("3"))
                {
                    width = 320;
                    height = 256;
                    desc = "map3";
                }
                else
                {
                    width = 160;
                    height = 512;
                    desc = "map5";
                }
                break;
            case 393_132:
                width = 160;
                height = 512;
                desc = "map4";
                break;
            default:
                width = 0;
                height = 0;
                desc = "Unknown map";
                break;
        }
    }
}