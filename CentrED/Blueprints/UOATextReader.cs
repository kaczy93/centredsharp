using ClassicUO.Assets;

namespace CentrED.Blueprints;

public static class UOATextReader
{
    public static List<MultiInfo> Read(string file)
    {
        var itemCount = 0;
        var lineNumber = 0;
        var tiles = new List<MultiInfo>();
        using (var reader = new StreamReader(file))
        {
            while (reader.ReadLine() is { } line)
            {
                lineNumber++;

                if (lineNumber < 4)
                    continue; // skip header
                if (lineNumber == 4)
                {
                    string[] split = line.Split(' ');
                    itemCount = Convert.ToInt32(split[0]);
                    
                    break;
                }
            }
            tiles.Capacity = itemCount;
            for (int i = 0; i < itemCount; i++)
            {
                var line = reader.ReadLine();
                if (line is null)
                {
                    Console.WriteLine($"[UOA] Mismatched number of components {itemCount} vs {i}");
                    break;
                }
                
                string[] split = line.Split(' ');

                var info = new MultiInfo();
                info.ID  = Convert.ToUInt16(split[0]);
                info.X = Convert.ToInt16(split[1]);
                info.Y = Convert.ToInt16(split[2]);
                info.Z = Convert.ToInt16(split[3]);
                info.IsVisible = true;
                var flags = Convert.ToInt32(split[4]); //What is this?
                var unk1 = 0;
                tiles.Add(info);
            }
        }
        return tiles;
    } 
}