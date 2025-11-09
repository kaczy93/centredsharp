using System.Diagnostics.CodeAnalysis;
using CentrED.Utils;

namespace CentrED.Blueprints;

public class CsvReader
{
    public static bool Read(string path, [MaybeNullWhen(false)] out List<BlueprintTile> tiles)
    {
        tiles = null;
        if (!path.EndsWith(".csv"))
            return false;
        
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);
        
        var header = reader.ReadLine();//Header
        
        tiles = new List<BlueprintTile>();
        do
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break; //Done reading
            }

            var split = line.Split(',');
            if (split.Length != 6)
            {
                return false;
            }

            var id = UshortParser.Apply(split[0]);
            var x = Convert.ToInt16(split[1]);
            var y = Convert.ToInt16(split[2]);
            var z = Convert.ToInt16(split[3]);
            var hue = UshortParser.Apply(split[4]);
            var flags = Convert.ToInt32(split[5]);

            tiles.Add(new BlueprintTile(id, x, y, z, hue, true));
        } while (true);
        
        return true;
    }
}