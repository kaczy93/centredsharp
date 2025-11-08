using CentrED.Utils;

namespace CentrED.Blueprints;

public class MultiNamesReader
{
    public const string FILE_NAME = "Multi.mul.txt";
    public static Dictionary<uint, string> Read(string dir)
    {
        Dictionary<uint, string> names = [];
        
        var file = Path.Combine(dir, FILE_NAME);
        if (!File.Exists(file))
            return names;
        
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);
        
        var lineIdx = 0;
        do
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break; //Done reading
            }
            lineIdx++;
            if (line.StartsWith("#"))
            {
                continue; //Comment
            }

            var split = line.Split(',', 2);
            if (split.Length != 2)
            {
                Console.WriteLine($"Invalid multi.mul.txt name line {lineIdx}: {line}");
                continue;
            }

            var id = UshortParser.Apply(split[0]);
            var name = split[1];
            
            names.Add(id, name);
        } while (true);
        
        return names;
    }
}