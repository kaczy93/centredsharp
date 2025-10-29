using System.Diagnostics.CodeAnalysis;

namespace CentrED.Blueprints;

//Source: https://github.com/CorexUO/UOArchitect/blob/main/UO%20Architect/IO/UOARBatchDataAdapter.cs
public class UOABinaryReader
{
    public static bool Read(string path, [MaybeNullWhen(false)] out Dictionary<string, List<BlueprintTile>> result)
    {
        result = null;
        if (!path.EndsWith(".uoa"))
            return false;
        
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(fs))
        {
            var version = reader.ReadInt16();
            var designCount = 1;
            if (version > 2 ) // Version check
            {
                Console.WriteLine($"[UOAB] version {version} is not supported!");
                return false;
            }
            if (version == 2)
                designCount = reader.ReadInt16();

            result = new Dictionary<string, List<BlueprintTile>>();
            
            for (int i = 0; i < designCount; i++)
            {
                var design = new List<BlueprintTile>();
                var name = ReadUOAString(reader);
                var category = ReadUOAString(reader);
                var subcategory = ReadUOAString(reader);

                var height = reader.ReadInt32();
                var width = reader.ReadInt32();
                var userHeight = reader.ReadInt32();
                var userWidth = reader.ReadInt32();

                int count = reader.ReadInt32();
                for (var j = 0; j < count; j++)
                {
                    var id = (ushort)reader.ReadInt16();
                    var x = reader.ReadInt16();
                    var y = reader.ReadInt16();
                    var z = reader.ReadInt16();
                    var isVisible = true;
                    reader.ReadInt16(); // level
                    var hue = (ushort)reader.ReadInt16(); 

                    design.Add(new BlueprintTile(id, x, y, z, hue, isVisible));
                }
                if (result.ContainsKey(name))
                {
                    name = $"name{i}";
                }
                result.Add(name, design);
            }
        }
        return true;
    }
    
    private static string ReadUOAString(BinaryReader bin)
    {
        byte flag = bin.ReadByte();

        return flag == 0 ? null : bin.ReadString();
    }
}