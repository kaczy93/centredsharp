using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using CentrED.Utils;

namespace CentrED.Blueprints;

public class TilesEntryXmlReader
{
    public static bool Read(string path, [MaybeNullWhen(false)] out Dictionary<string, List<BlueprintTile>> result)
    {
        result = null;
        if (!path.EndsWith(".xml"))
            return false;

        var xml = XElement.Load(path);
        if (xml.Name != "TilesEntry")
            return false;
        
        var entries = xml.Elements("Entry");
        
        result = new Dictionary<string, List<BlueprintTile>>();
        var i = 0;
        foreach (var entry in entries)
        {
            var name = entry.Attribute("Name")?.Value ?? $"Unknown{i}";
            if (TryReadTiles(entry.Elements("Item"), out var tiles))
            {
                if (!result.TryAdd(name, tiles))
                {
                    result.Add($"name_{i}", tiles);
                }
            }
            else
            {
                Console.WriteLine($"Unable to parse tiles entry '{name}', id: {i}");
                continue;
            }
            i++;
        }

        return true;
    }

    private static bool TryReadTiles(IEnumerable<XElement> items, out List<BlueprintTile> result)
    {
        result = new List<BlueprintTile>();
        foreach (var item in items)
        {
            short x, y, z;
            ushort id, hue;
            try
            {
                x = short.Parse(item.Attribute("X")?.Value ?? "0");
                y = short.Parse(item.Attribute("Y")?.Value ?? "0");
                z = short.Parse(item.Attribute("Z")?.Value ?? "0");
                id = UshortParser.Apply(item.Attribute("ID")?.Value ?? "0x0");
                hue = UshortParser.Apply(item.Attribute("Hue")?.Value ?? "0x0");
            }
            catch
            {
                return false;
            }
            result.Add(new BlueprintTile(id, x, y, z, hue, true));
        }
        return true;
    }
}