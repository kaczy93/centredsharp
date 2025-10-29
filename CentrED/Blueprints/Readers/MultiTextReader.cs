using System.Diagnostics.CodeAnalysis;

namespace CentrED.Blueprints;

//Source: https://github.com/CorexUO/UOArchitect/blob/main/UO%20Architect/IO/MultiTextAdapter.cs
public static class MultiTextReader
{
    public static bool Read(string path, [MaybeNullWhen(false)] out List<BlueprintTile> tiles)
    {
        tiles = null;
        if (!path.EndsWith(".txt"))
            return false;
        
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);
        
        var versionLine = reader.ReadLine()!;
        if ("6 version" != versionLine?.Trim())
        {
            return false; //Are there any other valid versions known?
        }
        var templateIdLine = reader.ReadLine()!;
        var itemVersionLine = reader.ReadLine()!;
        var numComponentsLine = reader.ReadLine()!;
        if (!int.TryParse(numComponentsLine.Split(' ')[0], out var itemCount))
        {
            return false;
        }
        tiles = new List<BlueprintTile>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                Log($"{path}: Actual number of components is lower than defined {itemCount}");
                break;
            }
            
            var split = line.Split(' ');
            
            var id = Convert.ToUInt16(split[0]);
            var x = Convert.ToInt16(split[1]);
            var y = Convert.ToInt16(split[2]);
            var z = Convert.ToInt16(split[3]);
            var flags = Convert.ToInt32(split[4]); //What is this?
            
            tiles.Add(new BlueprintTile(id, x, y, z, 0, true));
        }
        if (!reader.EndOfStream)
        {
            Log($"{path}: Actual number of components is higher than defined {itemCount}");
        }
        return true;
    }

    private static void Log(string text)
    {
        Console.WriteLine("[MultiTextReader] " + text);
    }
}