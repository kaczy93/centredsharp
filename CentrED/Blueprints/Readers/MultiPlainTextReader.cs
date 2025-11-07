using System.Diagnostics.CodeAnalysis;
using CentrED.Utils;

namespace CentrED.Blueprints;

//No idea what is the source of this
public static class MultiPlainTextReader
{
    public static bool Read(string path, [MaybeNullWhen(false)] out List<BlueprintTile> tiles)
    {
        tiles = null;
        if (!path.EndsWith(".txt"))
            return false;
        
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);
        
        tiles = new List<BlueprintTile>();
        do
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break; //Done reading
            }

            var split = line.Split(' ');
            if (split.Length != 5)
            {
                return false;
            }

            var id = UshortParser.Apply(split[0]);
            var x = Convert.ToInt16(split[1]);
            var y = Convert.ToInt16(split[2]);
            var z = Convert.ToInt16(split[3]);
            var flags = Convert.ToInt32(split[4]);

            tiles.Add(new BlueprintTile(id, x, y, z, 0, true));
        } while (true);
        
        return true;
    }
}